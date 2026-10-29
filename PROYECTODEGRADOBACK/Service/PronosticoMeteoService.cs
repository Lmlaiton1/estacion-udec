using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RR_Nueva_Naturaleza.DAL;
using RR_Nueva_Naturaleza.Models;
using RR_Nueva_Naturaleza.Models.Dto;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace RR_Nueva_Naturaleza.Service
{
    // Pronóstico IA del día siguiente. El modelo (LSTM Keras) se reentrena en Python con las lecturas
    // recientes; este servicio exporta los datos, ejecuta ia/pronostico_ia/pronostico_diario.py y guarda el resultado.
    // La retroalimentación por franja calibra los dos umbrales que deciden la condición del cielo.
    public class PronosticoMeteoService : IPronosticoMeteoService
    {
        // Un solo entrenamiento a la vez: el script reescribe la carpeta operacion/ en cada ejecución
        private static readonly SemaphoreSlim _enCurso = new(1, 1);

        public const int MinimoParaCalibrar = 10;
        private const decimal UmbralLluviaDefecto = 0.1m;
        private const decimal FactorSolDefecto = 0.65m;
        private static readonly decimal[] UmbralesLluvia = { 0.05m, 0.1m, 0.15m, 0.2m, 0.3m, 0.4m, 0.5m, 0.75m, 1m };
        private static readonly decimal[] FactoresSol = { 0.4m, 0.45m, 0.5m, 0.55m, 0.6m, 0.65m, 0.7m, 0.75m, 0.8m, 0.85m, 0.9m };

        // Variables que usa el modelo (nombres de Lecturas_Meteo)
        private static readonly string[] VariablesModelo = { "temperatura", "humedad", "radiacion", "lluvia" };
        private static readonly string[] CondicionesReales = { "Soleado", "Nublado", "Lluvioso" };

        // Etiquetas que emite predecir.py
        private const string SenalSol = "Señal de sol";
        private const string PosibleNubosidad = "Posible nubosidad";
        private const string PosibleLluvia = "Posible lluvia";
        private const string NoDeterminado = "Cielo no determinado";
        private const string Variable = "Condición variable";

        private readonly RR_Nueva_NaturalezaContext _context;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<PronosticoMeteoService> _logger;

        public PronosticoMeteoService(RR_Nueva_NaturalezaContext context, IMapper mapper, IConfiguration configuration,
            IWebHostEnvironment env, ILogger<PronosticoMeteoService> logger)
        {
            _context = context;
            _mapper = mapper;
            _configuration = configuration;
            _env = env;
            _logger = logger;
        }

        public async Task<ServiceResponse> GenerarPronostico(Guid usuarioId)
        {
            ServiceResponse response = new ServiceResponse();
            if (!await _enCurso.WaitAsync(0))
                return Fallo(response, "Ya hay un pronóstico en curso. Espere a que termine.");

            var trabajo = Path.Combine(Path.GetTempPath(), "pronostico_meteo", Guid.NewGuid().ToString("N"));
            try
            {
                var seccion = _configuration.GetSection("Pronostico");
                var carpeta = RutaAbsoluta(seccion["CarpetaScript"] ?? "../ia/pronostico_ia");
                var python = seccion["PythonPath"] ?? "../ia/pronostico_ia/.venv/Scripts/python.exe";
                // Un nombre suelto (python3) se busca en el PATH; una ruta se resuelve desde el backend
                if (python.Contains('/') || python.Contains('\\'))
                {
                    python = RutaAbsoluta(python);
                    if (!File.Exists(python))
                        return Fallo(response, $"No se encontró Python en {python}. Revise Pronostico:PythonPath.");
                }
                if (!File.Exists(Path.Combine(carpeta, "pronostico_diario.py")))
                    return Fallo(response, $"No se encontró pronostico_diario.py en {carpeta}. Revise Pronostico:CarpetaScript.");

                Directory.CreateDirectory(trabajo);
                var lecturas = Path.Combine(trabajo, "lecturas.csv");
                var salida = Path.Combine(trabajo, "pronostico.json");
                var corte = DateTime.Now;
                var dias = seccion.GetValue("DiasExportar", 30);
                await ExportarLecturas(lecturas, corte.AddDays(-dias), corte);

                var (umbralLluvia, factorSol, _) = await CalcularUmbrales();
                var reloj = Stopwatch.StartNew();
                var (codigo, error) = await EjecutarPython(python, carpeta, new[]
                {
                    "pronostico_diario.py",
                    "--lecturas", lecturas,
                    "--corte", corte.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                    "--umbral-lluvia", umbralLluvia.ToString(CultureInfo.InvariantCulture),
                    "--factor-sol", factorSol.ToString(CultureInfo.InvariantCulture),
                    "--salida", salida
                }, TimeSpan.FromMinutes(seccion.GetValue("TiempoMaximoMinutos", 10)));
                reloj.Stop();

                if (codigo != 0 || !File.Exists(salida))
                    return Fallo(response, "No se pudo generar el pronóstico: " + error);

                var pronostico = await GuardarResultado(salida, usuarioId, (decimal)reloj.Elapsed.TotalSeconds);
                response.Result = ServiceResponseType.Succeded;
                response.InformationMessage = pronostico.Advertencia ?? $"Pronóstico del {pronostico.DiaPronosticado:dd/MM/yyyy} generado";
                response.Data = await ConstruirDto(pronostico.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando el pronóstico");
                response.Result = ServiceResponseType.Failed;
                response.ErrorMessage = "Error generando el pronóstico: " + ex.Message;
            }
            finally
            {
                _enCurso.Release();
                try { if (Directory.Exists(trabajo)) Directory.Delete(trabajo, true); } catch { /* temporal */ }
            }
            return response;
        }

        public async Task<PronosticoMeteoDto?> GetUltimo()
        {
            var id = await _context.Pronosticos_Meteo
                .OrderByDescending(p => p.FechaGeneracion)
                .Select(p => (Guid?)p.Id)
                .FirstOrDefaultAsync();
            return id == null ? null : await ConstruirDto(id.Value);
        }

        public async Task<List<PronosticoFranjaMeteoDto>> GetFranjasRecientes(int dias)
        {
            var ahora = DateTime.Now;
            var franjas = await FranjasVigentes(ahora.Date.AddDays(-Math.Max(dias, 1)));
            return franjas
                .Where(f => FinFranja(f) <= ahora)
                .OrderByDescending(f => f.Pronostico!.DiaPronosticado).ThenByDescending(f => f.Orden)
                .Select(ADto)
                .ToList();
        }

        public async Task<ServiceResponse> Calificar(RetroalimentacionMeteoCreateDto retro, Guid usuarioId)
        {
            ServiceResponse response = new ServiceResponse();
            try
            {
                var real = CondicionesReales.FirstOrDefault(c => c.Equals(retro.CondicionReal?.Trim(), StringComparison.OrdinalIgnoreCase));
                if (real == null)
                    return Fallo(response, "La condición real debe ser Soleado, Nublado o Lluvioso");

                var franja = await _context.Pronosticos_Franja_Meteo
                    .Include(f => f.Pronostico)
                    .Include(f => f.Retroalimentacion)
                    .FirstOrDefaultAsync(f => f.Id == retro.FranjaId);
                if (franja == null)
                    return Fallo(response, "La franja no existe");
                if (FinFranja(franja) > DateTime.Now)
                    return Fallo(response, "Solo se puede calificar una franja que ya terminó");

                // Si ya estaba calificada se corrige (queda la última observación)
                var registro = franja.Retroalimentacion ?? new RetroalimentacionMeteo { Id = Guid.NewGuid(), FranjaId = franja.Id };
                registro.CondicionReal = real;
                registro.Acerto = Coincide(franja.CondicionPrevista, real);
                registro.UsuarioId = usuarioId;
                registro.Fecha = DateTime.Now;
                if (franja.Retroalimentacion == null)
                    _context.Retroalimentacion_Meteo.Add(registro);
                await _context.SaveChangesAsync();

                response.Result = ServiceResponseType.Succeded;
                response.InformationMessage = registro.Acerto ? "Calificado: el pronóstico acertó" : "Calificado: el pronóstico no acertó";
                response.Data = ADto(franja);
            }
            catch (Exception ex)
            {
                response.Result = ServiceResponseType.Failed;
                response.ErrorMessage = "Error guardando la calificación: " + ex.Message;
            }
            return response;
        }

        public async Task<EstadisticaPronosticoDto> GetEstadisticas()
        {
            var total = await _context.Retroalimentacion_Meteo.CountAsync();
            var aciertos = await _context.Retroalimentacion_Meteo.CountAsync(r => r.Acerto);
            var (umbralLluvia, factorSol, calibrado) = await CalcularUmbrales();
            return new EstadisticaPronosticoDto
            {
                Calificadas = total,
                Aciertos = aciertos,
                PorcentajeAcierto = total == 0 ? 0 : Math.Round(100m * aciertos / total, 1),
                UmbralLluvia = umbralLluvia,
                FactorSol = factorSol,
                Calibrado = calibrado,
                MinimoParaCalibrar = MinimoParaCalibrar
            };
        }

        // Busca los umbrales (lluvia mm/h y fracción de la luz de referencia) que más franjas calificadas
        // aciertan, recalculando la condición con los valores horarios guardados de cada pronóstico.
        private async Task<(decimal umbralLluvia, decimal factorSol, bool calibrado)> CalcularUmbrales()
        {
            var calificadas = await _context.Retroalimentacion_Meteo
                .AsNoTracking()
                .Include(r => r.Franja!).ThenInclude(f => f.Pronostico)
                .ToListAsync();
            if (calificadas.Count < MinimoParaCalibrar)
                return (UmbralLluviaDefecto, FactorSolDefecto, false);

            var ids = calificadas.Select(r => r.Franja!.PronosticoId).Distinct().ToList();
            var horas = await _context.Predicciones_Meteo
                .AsNoTracking()
                .Where(p => p.PronosticoId != null && ids.Contains(p.PronosticoId.Value)
                            && (p.Variable == "lluvia" || p.Variable == "radiacion" || p.Variable == "luz_referencia"))
                .Select(p => new { p.PronosticoId, p.FechaPrediccion, p.Variable, p.ValorPredicho })
                .ToListAsync();
            var porHora = horas
                .GroupBy(h => (h.PronosticoId!.Value, h.FechaPrediccion))
                .ToDictionary(g => g.Key, g => g.ToDictionary(x => x.Variable, x => x.ValorPredicho));

            // Valores horarios de cada franja calificada
            var casos = calificadas.Select(r =>
            {
                var inicio = InicioFranja(r.Franja!);
                var valores = Enumerable.Range(0, 6)
                    .Select(i => inicio.AddHours(i))
                    .Where(t => porHora.ContainsKey((r.Franja!.PronosticoId, t)))
                    .Select(t => (hora: t.Hour, v: porHora[(r.Franja!.PronosticoId, t)]))
                    .ToList();
                return (real: r.CondicionReal, valores);
            }).Where(c => c.valores.Count > 0).ToList();
            if (casos.Count < MinimoParaCalibrar)
                return (UmbralLluviaDefecto, FactorSolDefecto, false);

            var mejor = (u: UmbralLluviaDefecto, f: FactorSolDefecto, aciertos: -1, distancia: decimal.MaxValue);
            foreach (var u in UmbralesLluvia)
            {
                foreach (var f in FactoresSol)
                {
                    var aciertos = casos.Count(c => Coincide(CondicionFranja(c.valores, u, f), c.real));
                    // En empate se prefiere lo más cercano a los valores validados originalmente
                    var distancia = Math.Abs(u - UmbralLluviaDefecto) + Math.Abs(f - FactorSolDefecto);
                    if (aciertos > mejor.aciertos || (aciertos == mejor.aciertos && distancia < mejor.distancia))
                        mejor = (u, f, aciertos, distancia);
                }
            }
            return (mejor.u, mejor.f, true);
        }

        // Misma regla de predecir.py: lluvia ≥ umbral; de día, luz ≥ max(100, factor × percentil 90 horario);
        // la franja toma la condición que se repite en al menos 3 de sus 6 horas.
        private static string CondicionFranja(List<(int hora, Dictionary<string, decimal> v)> valores, decimal umbralLluvia, decimal factorSol)
        {
            var estados = valores.Select(x =>
            {
                if (x.v.TryGetValue("lluvia", out var lluvia) && lluvia >= umbralLluvia) return PosibleLluvia;
                if (x.hora >= 6 && x.hora < 18)
                {
                    var hayReferencia = x.v.TryGetValue("luz_referencia", out var referencia);
                    x.v.TryGetValue("radiacion", out var luz);
                    return hayReferencia && luz >= Math.Max(100m, factorSol * referencia) ? SenalSol : PosibleNubosidad;
                }
                return NoDeterminado;
            });
            var mayoritario = estados.GroupBy(e => e).OrderByDescending(g => g.Count()).First();
            return mayoritario.Count() >= 3 ? mayoritario.Key : Variable;
        }

        private static bool Coincide(string prevista, string real) => prevista switch
        {
            SenalSol => real == "Soleado",
            PosibleNubosidad => real == "Nublado",
            PosibleLluvia => real == "Lluvioso",
            // De noche el modelo solo distingue lluvia / sin lluvia
            NoDeterminado => real != "Lluvioso",
            _ => false
        };

        private async Task ExportarLecturas(string ruta, DateTime desde, DateTime hasta)
        {
            var filas = _context.Lecturas_Meteo
                .AsNoTracking()
                .Where(l => l.FechaHora >= desde && l.FechaHora <= hasta && VariablesModelo.Contains(l.Variable))
                .OrderBy(l => l.EstacionNumero).ThenBy(l => l.FechaHora)
                .Select(l => new { l.EstacionNumero, l.Variable, l.FechaHora, l.Valor })
                .AsAsyncEnumerable();

            await using var escritor = new StreamWriter(ruta, false, new UTF8Encoding(false));
            await escritor.WriteLineAsync("estacion,variable,fecha_hora,valor");
            await foreach (var l in filas)
            {
                await escritor.WriteLineAsync(string.Join(',', l.EstacionNumero, l.Variable,
                    l.FechaHora.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                    l.Valor.ToString(CultureInfo.InvariantCulture)));
            }
        }

        private async Task<(int codigo, string error)> EjecutarPython(string python, string carpeta, string[] argumentos, TimeSpan limite)
        {
            var inicio = new ProcessStartInfo(python)
            {
                WorkingDirectory = carpeta,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
            foreach (var a in argumentos) inicio.ArgumentList.Add(a);
            inicio.Environment["PYTHONIOENCODING"] = "utf-8";
            inicio.Environment["TF_CPP_MIN_LOG_LEVEL"] = "2";

            using var proceso = new Process { StartInfo = inicio };
            var errores = new StringBuilder();
            proceso.ErrorDataReceived += (_, e) => { if (e.Data != null) lock (errores) errores.AppendLine(e.Data); };
            proceso.OutputDataReceived += (_, _) => { };
            proceso.Start();
            proceso.BeginErrorReadLine();
            proceso.BeginOutputReadLine();

            using var cancelar = new CancellationTokenSource(limite);
            try
            {
                await proceso.WaitForExitAsync(cancelar.Token);
            }
            catch (OperationCanceledException)
            {
                proceso.Kill(entireProcessTree: true);
                return (-1, $"el entrenamiento superó {limite.TotalMinutes:0} minutos");
            }

            var texto = errores.ToString();
            if (proceso.ExitCode != 0)
                _logger.LogWarning("pronostico_diario.py terminó con código {Codigo}: {Error}", proceso.ExitCode, texto);
            // El script escribe "ERROR: ..." para los casos esperados (p. ej. faltan horas de la estación 3)
            var lineas = texto.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var mensaje = lineas.LastOrDefault(l => l.StartsWith("ERROR:"))?.Substring(6).Trim()
                          ?? lineas.LastOrDefault() ?? "error desconocido";
            return (proceso.ExitCode, mensaje);
        }

        private async Task<PronosticoMeteo> GuardarResultado(string rutaJson, Guid usuarioId, decimal segundos)
        {
            using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(rutaJson, Encoding.UTF8));
            var raiz = doc.RootElement;
            var ahora = DateTime.Now;
            var ventanas = raiz.GetProperty("entrenamiento").GetProperty("modelos").EnumerateObject()
                .Sum(m => m.Value.GetProperty("ventanas").GetInt32());

            var pronostico = new PronosticoMeteo
            {
                Id = Guid.NewGuid(),
                FechaGeneracion = ahora,
                GeneradoPorId = usuarioId,
                DiaPronosticado = DateTime.Parse(raiz.GetProperty("dia_pronosticado").GetString()!, CultureInfo.InvariantCulture),
                OrigenDatos = DateTime.Parse(raiz.GetProperty("origen").GetString()!, CultureInfo.InvariantCulture),
                ModeloVersion = raiz.GetProperty("version_modelo").GetString() ?? string.Empty,
                UmbralLluvia = raiz.GetProperty("umbral_lluvia").GetDecimal(),
                FactorSol = raiz.GetProperty("factor_sol").GetDecimal(),
                Advertencia = Texto(raiz, "advertencia"),
                VentanasEntrenamiento = ventanas,
                SegundosEjecucion = Math.Round(segundos, 2)
            };

            var orden = 0;
            foreach (var f in raiz.GetProperty("franjas").EnumerateArray())
            {
                pronostico.Franjas.Add(new PronosticoFranjaMeteo
                {
                    Id = Guid.NewGuid(),
                    Franja = Texto(f, "franja") ?? string.Empty,
                    Orden = orden++,
                    CondicionPrevista = Texto(f, "condicion_orientativa") ?? string.Empty,
                    TemperaturaMedia = Numero(f, "temperatura_media_c") ?? 0,
                    TemperaturaMin = Numero(f, "temperatura_min_curva_c") ?? 0,
                    TemperaturaMax = Numero(f, "temperatura_max_curva_c") ?? 0,
                    HumedadMedia = Numero(f, "humedad_media_pct") ?? 0,
                    LuzMediaLux = Math.Round(Numero(f, "luz_media_lux") ?? 0, 2),
                    LluviaMm = Numero(f, "lluvia_era5_estimada_mm") ?? 0
                });
            }
            _context.Pronosticos_Meteo.Add(pronostico);

            // Variable de Lecturas_Meteo -> columnas del JSON horario (valor, límite inferior, límite superior)
            var columnas = new (string variable, string valor, string? limite)[]
            {
                ("temperatura", "tem", "tem"), ("humedad", "hum", "hum"), ("radiacion", "rad", "rad"),
                ("lluvia", "lluv", null), ("luz_referencia", "luz_referencia", null)
            };
            var horizonte = 0;
            foreach (var h in raiz.GetProperty("horario").EnumerateArray())
            {
                horizonte++;
                var fecha = DateTime.Parse(h.GetProperty("fecha_hora").GetString()!, CultureInfo.InvariantCulture);
                foreach (var (variable, valor, limite) in columnas)
                {
                    var v = Numero(h, valor);
                    if (v == null) continue;
                    _context.Predicciones_Meteo.Add(new PrediccionMeteo
                    {
                        Id = Guid.NewGuid(),
                        PronosticoId = pronostico.Id,
                        EstacionNumero = 3,     // estación de referencia del modelo
                        Variable = variable,
                        FechaPrediccion = fecha,
                        HorizonteHoras = horizonte,
                        ValorPredicho = Math.Round(v.Value, 4),
                        LimiteInferior = limite == null ? null : Redondear(Numero(h, limite + "_limite_error_inferior")),
                        LimiteSuperior = limite == null ? null : Redondear(Numero(h, limite + "_limite_error_superior")),
                        NivelConfianza = 0,     // el modelo no entrega confianza calibrada
                        ModeloVersion = pronostico.ModeloVersion,
                        FechaGeneracion = ahora
                    });
                }
            }
            await _context.SaveChangesAsync();
            return pronostico;
        }

        private async Task<PronosticoMeteoDto> ConstruirDto(Guid id)
        {
            var pronostico = await _context.Pronosticos_Meteo
                .AsNoTracking()
                .Include(p => p.Franjas).ThenInclude(f => f.Retroalimentacion)
                .FirstAsync(p => p.Id == id);
            var dto = _mapper.Map<PronosticoMeteo, PronosticoMeteoDto>(pronostico);
            dto.Franjas = pronostico.Franjas.OrderBy(f => f.Orden).Select(f => { f.Pronostico = pronostico; return ADto(f); }).ToList();

            var filas = await _context.Predicciones_Meteo
                .AsNoTracking()
                .Where(p => p.PronosticoId == id)
                .ToListAsync();
            dto.Horario = filas
                .GroupBy(p => p.FechaPrediccion)
                .OrderBy(g => g.Key)
                .Select(g =>
                {
                    PrediccionMeteo? V(string variable) => g.FirstOrDefault(p => p.Variable == variable);
                    var tem = V("temperatura"); var hum = V("humedad"); var rad = V("radiacion"); var lluv = V("lluvia");
                    return new PronosticoHoraDto
                    {
                        FechaHora = g.Key,
                        Temperatura = tem?.ValorPredicho, TemperaturaInferior = tem?.LimiteInferior, TemperaturaSuperior = tem?.LimiteSuperior,
                        Humedad = hum?.ValorPredicho, HumedadInferior = hum?.LimiteInferior, HumedadSuperior = hum?.LimiteSuperior,
                        Luz = rad?.ValorPredicho, LuzInferior = rad?.LimiteInferior, LuzSuperior = rad?.LimiteSuperior,
                        Lluvia = lluv?.ValorPredicho
                    };
                })
                .ToList();
            return dto;
        }

        // Franjas del pronóstico más reciente de cada día (si se pulsó varias veces, cuenta la última emisión)
        private async Task<List<PronosticoFranjaMeteo>> FranjasVigentes(DateTime desdeDia)
        {
            var pronosticos = await _context.Pronosticos_Meteo
                .AsNoTracking()
                .Where(p => p.DiaPronosticado >= desdeDia)
                .Include(p => p.Franjas).ThenInclude(f => f.Retroalimentacion)
                .ToListAsync();
            return pronosticos
                .GroupBy(p => p.DiaPronosticado)
                .Select(g => g.OrderByDescending(p => p.FechaGeneracion).First())
                .SelectMany(p => p.Franjas.Select(f => { f.Pronostico = p; return f; }))
                .ToList();
        }

        private PronosticoFranjaMeteoDto ADto(PronosticoFranjaMeteo franja)
        {
            var dto = _mapper.Map<PronosticoFranjaMeteo, PronosticoFranjaMeteoDto>(franja);
            dto.DiaPronosticado = franja.Pronostico?.DiaPronosticado ?? default;
            dto.CondicionReal = franja.Retroalimentacion?.CondicionReal;
            dto.Acerto = franja.Retroalimentacion?.Acerto;
            return dto;
        }

        // Madrugada 0-5, Mañana 6-11, Tarde 12-17, Noche 18-23
        private static DateTime InicioFranja(PronosticoFranjaMeteo f) => f.Pronostico!.DiaPronosticado.Date.AddHours(6 * f.Orden);
        private static DateTime FinFranja(PronosticoFranjaMeteo f) => InicioFranja(f).AddHours(6);

        private string RutaAbsoluta(string ruta) =>
            Path.GetFullPath(Path.IsPathRooted(ruta) ? ruta : Path.Combine(_env.ContentRootPath, ruta));

        private static string? Texto(JsonElement e, string nombre) =>
            e.TryGetProperty(nombre, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

        private static decimal? Numero(JsonElement e, string nombre) =>
            e.TryGetProperty(nombre, out var v) && v.ValueKind == JsonValueKind.Number ? (decimal)v.GetDouble() : null;

        private static decimal? Redondear(decimal? v) => v == null ? null : Math.Round(v.Value, 4);

        private static ServiceResponse Fallo(ServiceResponse response, string mensaje)
        {
            response.Result = ServiceResponseType.Failed;
            response.ErrorMessage = mensaje;
            return response;
        }
    }
}
