using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RR_Nueva_Naturaleza.DAL;
using RR_Nueva_Naturaleza.Models;
using RR_Nueva_Naturaleza.Models.Dto;

namespace RR_Nueva_Naturaleza.Service
{
    public class LecturaMeteoService : ILecturaMeteoService
    {
        private readonly RR_Nueva_NaturalezaContext _context;

        public LecturaMeteoService(RR_Nueva_NaturalezaContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<LecturaMeteo>> GetLecturas()
        {
            return await _context.Lecturas_Meteo.ToListAsync();
        }

        public async Task<LecturaMeteo?> GetLectura(Guid id)
        {
            return await _context.Lecturas_Meteo.FindAsync(id);
        }

        public async Task<IEnumerable<LecturaMeteo>> GetLecturasByEstacion(int numero, DateTime? fechaInicio, DateTime? fechaFin)
        {
            var (desde, hasta) = RangoConDefecto(fechaInicio, fechaFin);

            return await _context.Lecturas_Meteo
                .Where(l => l.EstacionNumero == numero && l.FechaHora >= desde && l.FechaHora <= hasta)
                .OrderByDescending(l => l.FechaHora)
                .ToListAsync();
        }

        public async Task<IEnumerable<LecturaMeteo>> GetLecturasByEstacionAndVariable(int numero, string variable, DateTime? fechaInicio, DateTime? fechaFin)
        {
            var (desde, hasta) = RangoConDefecto(fechaInicio, fechaFin);

            return await _context.Lecturas_Meteo
                .Where(l => l.EstacionNumero == numero && l.Variable == variable && l.FechaHora >= desde && l.FechaHora <= hasta)
                .OrderByDescending(l => l.FechaHora)
                .ToListAsync();
        }

        // Sin fechaInicio/fechaFin, el rango por defecto son las últimas 24 horas.
        private static (DateTime desde, DateTime hasta) RangoConDefecto(DateTime? fechaInicio, DateTime? fechaFin)
        {
            var hasta = fechaFin ?? DateTime.Now;
            var desde = fechaInicio ?? hasta.AddHours(-24);
            return (desde, hasta);
        }

        // Última lectura de cada variable, para las 3 estaciones, en una sola consulta liviana
        // (usa el índice compuesto EstacionNumero/Variable/FechaHora en vez de traer el historial completo).
        public async Task<IEnumerable<LecturaMeteo>> GetUltimasLecturas()
        {
            return await _context.Lecturas_Meteo
                .GroupBy(l => new { l.EstacionNumero, l.Variable })
                .Select(g => g.OrderByDescending(l => l.FechaHora).First())
                .ToListAsync();
        }

        public async Task<IEnumerable<LecturaMeteo>> GetLecturasParaReporte(LecturaMeteoPdfRequestDto filtro)
        {
            var desde = filtro.FechaInicio.Date;
            var hasta = filtro.FechaFin.Date.AddDays(1).AddTicks(-1);

            var query = _context.Lecturas_Meteo
                .Where(l => l.FechaHora >= desde && l.FechaHora <= hasta);

            if (filtro.EstacionNumero.HasValue)
            {
                query = query.Where(l => l.EstacionNumero == filtro.EstacionNumero.Value);
            }

            if (!string.IsNullOrEmpty(filtro.Variable))
            {
                query = query.Where(l => l.Variable == filtro.Variable);
            }

            var lecturas = await query.OrderBy(l => l.FechaHora).ToListAsync();

            // El rango horario se aplica en memoria: compara solo la hora del día, no la fecha completa.
            if (filtro.HoraInicio.HasValue && filtro.HoraFin.HasValue)
            {
                lecturas = lecturas
                    .Where(l => DentroDelHorario(l.FechaHora, filtro.HoraInicio.Value, filtro.HoraFin.Value))
                    .ToList();
            }

            return lecturas;
        }

        // Compara minuto a minuto, así "hasta" incluye todo ese minuto (23:59 cubre hasta 23:59:59).
        // Si inicio > fin (ej. 18:00 a 06:00) el horario cruza la medianoche.
        private static bool DentroDelHorario(DateTime fechaHora, TimeSpan horaInicio, TimeSpan horaFin)
        {
            var minuto = fechaHora.Hour * 60 + fechaHora.Minute;
            var inicio = (int)horaInicio.TotalMinutes;
            var fin = (int)horaFin.TotalMinutes;

            return inicio <= fin
                ? minuto >= inicio && minuto <= fin
                : minuto >= inicio || minuto <= fin;
        }

        public async Task<EstadisticaMeteoDto> GetEstadisticas(int estacionNumero, string variable, int cantidad)
        {
            // Se toman las N lecturas más recientes y se reordenan cronológicamente
            var lecturas = (await _context.Lecturas_Meteo
                .Where(l => l.EstacionNumero == estacionNumero && l.Variable == variable)
                .OrderByDescending(l => l.FechaHora)
                .Take(cantidad)
                .ToListAsync())
                .OrderBy(l => l.FechaHora)
                .ToList();

            var resultado = new EstadisticaMeteoDto
            {
                EstacionNumero = estacionNumero,
                Variable = variable,
                CantidadSolicitada = cantidad,
                CantidadUsada = lecturas.Count
            };

            if (lecturas.Count == 0)
            {
                return resultado;
            }

            var valores = lecturas.Select(l => l.Valor).ToList();
            var ordenados = valores.OrderBy(v => v).ToList();
            var n = ordenados.Count;

            resultado.Unidad = lecturas[^1].Unidad;
            resultado.FechaDesde = lecturas[0].FechaHora;
            resultado.FechaHasta = lecturas[^1].FechaHora;
            resultado.Valores = valores;

            var media = valores.Average();
            resultado.Media = Math.Round(media, 2);

            resultado.Mediana = n % 2 == 1
                ? ordenados[n / 2]
                : Math.Round((ordenados[n / 2 - 1] + ordenados[n / 2]) / 2, 2);

            // Moda: los valores son continuos, así que se agrupan redondeados a 1 decimal.
            // Si ningún valor se repite no hay moda; si hay empate se devuelven todas.
            var frecuencias = valores
                .GroupBy(v => Math.Round(v, 1))
                .Select(g => new { Valor = g.Key, Frecuencia = g.Count() })
                .ToList();
            var frecuenciaMax = frecuencias.Max(f => f.Frecuencia);
            if (frecuenciaMax > 1)
            {
                resultado.FrecuenciaModa = frecuenciaMax;
                resultado.Modas = frecuencias
                    .Where(f => f.Frecuencia == frecuenciaMax)
                    .Select(f => f.Valor)
                    .OrderBy(v => v)
                    .ToList();
            }

            resultado.Minimo = ordenados[0];
            resultado.Maximo = ordenados[^1];
            resultado.Rango = ordenados[^1] - ordenados[0];

            // Desviación estándar muestral (n - 1)
            if (n > 1)
            {
                var sumaCuadrados = valores.Sum(v => (double)((v - media) * (v - media)));
                resultado.DesviacionEstandar = Math.Round((decimal)Math.Sqrt(sumaCuadrados / (n - 1)), 2);
            }
            else
            {
                resultado.DesviacionEstandar = 0;
            }

            return resultado;
        }

        public async Task GenerarPdf(Stream output, LecturaMeteoPdfRequestDto filtro)
        {
            var lecturas = (await GetLecturasParaReporte(filtro)).ToList();
            var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/UdeC.jpg");

            var estacionTexto = filtro.EstacionNumero.HasValue ? $"Estación {filtro.EstacionNumero}" : "Todas las estaciones";
            var variableTexto = string.IsNullOrEmpty(filtro.Variable) ? "Todas las variables" : filtro.Variable;
            var horaTexto = (filtro.HoraInicio.HasValue && filtro.HoraFin.HasValue)
                ? $" · Horario: {filtro.HoraInicio:hh\\:mm} a {filtro.HoraFin:hh\\:mm}"
                  + (filtro.HoraInicio > filtro.HoraFin ? " (cruza la medianoche)" : string.Empty)
                : string.Empty;

            QuestPDF.Settings.License = LicenseType.Community;
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(50);
                    page.Size(PageSizes.A4);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(16));

                    page.Header().Column(column =>
                    {
                        column.Item().Row(row =>
                        {
                            row.RelativeItem();
                            row.RelativeItem().AlignRight()
                                .Text(text =>
                                {
                                    text.Span("Generado el: ").FontSize(9);
                                    text.Span($"{DateTime.Now:dd-MM-yyyy HH:mm}").FontSize(9);
                                });
                        });

                        column.Item().Row(row =>
                        {
                            row.ConstantItem(80).Image(imagePath).FitWidth();
                            row.RelativeItem().AlignMiddle().PaddingLeft(50)
                                .Text("ESTACIÓN METEOROLÓGICA")
                                .SemiBold().FontSize(22).FontColor(Colors.Blue.Darken3);
                        });
                    });

                    page.Content().PaddingTop(10).Column(column =>
                    {
                        column.Item().Height(30);
                        column.Spacing(5);

                        column.Item().AlignCenter()
                            .Text("Reporte de Lecturas Meteorológicas")
                            .SemiBold().FontSize(20).FontColor(Colors.Grey.Darken4);

                        column.Item().AlignCenter()
                            .Text($"{estacionTexto} · {variableTexto} · Del {filtro.FechaInicio:dd-MM-yyyy} al {filtro.FechaFin:dd-MM-yyyy}{horaTexto}")
                            .FontSize(11).FontColor(Colors.Grey.Darken2);

                        column.Item().Height(20);

                        column.Item().Table(table =>
                        {
                            var headerColor = "#1565C0";
                            var zebra1 = "#FFFFFF";
                            var zebra2 = "#F4F6F5";

                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn(1.5f);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(headerColor).Padding(6).Text("Estación").FontColor("#FFFFFF").FontSize(10).SemiBold().AlignCenter();
                                header.Cell().Background(headerColor).Padding(6).Text("Variable").FontColor("#FFFFFF").FontSize(10).SemiBold().AlignCenter();
                                header.Cell().Background(headerColor).Padding(6).Text("Valor").FontColor("#FFFFFF").FontSize(10).SemiBold().AlignCenter();
                                header.Cell().Background(headerColor).Padding(6).Text("Unidad").FontColor("#FFFFFF").FontSize(10).SemiBold().AlignCenter();
                                header.Cell().Background(headerColor).Padding(6).Text("Fecha y hora").FontColor("#FFFFFF").FontSize(10).SemiBold().AlignCenter();
                            });

                            bool odd = false;
                            foreach (var item in lecturas)
                            {
                                odd = !odd;
                                var rowColor = odd ? zebra1 : zebra2;

                                table.Cell().Background(rowColor).Padding(5).Text($"Estación {item.EstacionNumero}").FontSize(9).AlignCenter();
                                table.Cell().Background(rowColor).Padding(5).Text(item.Variable).FontSize(9).AlignCenter();
                                table.Cell().Background(rowColor).Padding(5).Text(item.ValorCardinal ?? item.Valor.ToString()).FontSize(9).AlignCenter();
                                table.Cell().Background(rowColor).Padding(5).Text(item.Unidad).FontSize(9).AlignCenter();
                                table.Cell().Background(rowColor).Padding(5).Text(item.FechaHora.ToString("yyyy-MM-dd HH:mm:ss")).FontSize(9).AlignCenter();
                            }
                        });

                        if (lecturas.Count == 0)
                        {
                            column.Item().Height(20);
                            column.Item().AlignCenter().Text("No hay lecturas que coincidan con los filtros seleccionados.")
                                .FontSize(12).Italic().FontColor(Colors.Grey.Darken1);
                        }
                    });

                    page.Footer().DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Grey.Darken2))
                        .Column(column =>
                        {
                            column.Spacing(2);
                            column.Item().Row(row =>
                            {
                                row.RelativeItem(3).Text("Universidad de Cundinamarca - Extensión Facatativá · Red de estaciones meteorológicas LESTOMA");
                                row.RelativeItem(1).AlignRight().Text(text =>
                                {
                                    text.Span("Página ");
                                    text.CurrentPageNumber();
                                    text.Span(" de ");
                                    text.TotalPages();
                                });
                            });
                        });
                });
            }).GeneratePdf(output);
        }
    }
}
