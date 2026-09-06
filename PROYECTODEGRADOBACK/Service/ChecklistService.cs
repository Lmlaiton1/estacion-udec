using Microsoft.EntityFrameworkCore;
using RR_Nueva_Naturaleza.DAL;
using RR_Nueva_Naturaleza.Models;
using RR_Nueva_Naturaleza.Models.Dto;
using System.Text;
using System.Text.RegularExpressions;
using System.Transactions;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace RR_Nueva_Naturaleza.Service
{
    public class ChecklistService : IChecklistService
    {
        private readonly RR_Nueva_NaturalezaContext _context;

        public ChecklistService(RR_Nueva_NaturalezaContext context)
        {
            _context = context;
        }

        public async Task<ServiceResponse> AddChecklist(ChecklistHeaderDto checklistDto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Crear encabezado
                var checklist = new ChecklistHeader()
                {
                    Id = Guid.NewGuid(),
                    UsuarioId = checklistDto.UsuarioId,
                    Fecha = checklistDto.Fecha,
                    Observacion = checklistDto.Observacion
                };

                await _context.ChecklistHeaders.AddAsync(checklist);

                // 2. Crear detalles
                foreach (var detalle in checklistDto.Detalles ?? Enumerable.Empty<ChecklistDetailDto>())
                {
                    var detalleEntidad = new ChecklistDetail()
                    {
                        Id = Guid.NewGuid(),
                        ChecklistHeaderId = checklist.Id,
                        DispositivoId = detalle.DispositivoId,
                        Estado = detalle.Estado ? "Encendido" : "Apagado",
                        MedicionSensor = detalle.MedicionSensor,
                        MedicionManual = detalle.MedicionManual
                    };

                    await _context.ChecklistDetails.AddAsync(detalleEntidad);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new ServiceResponse()
                {
                    Result = ServiceResponseType.Succeded,
                    InformationMessage = "Checklist agregado correctamente",
                    Data = checklist.Id
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new ServiceResponse()
                {
                    Result = ServiceResponseType.Failed,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<ServiceResponse> GetChecklist(Guid checklistId)
        {
            try
            {
                var checklist = await _context.ChecklistHeaders
                    .Include(c => c.Detalles)
                    .FirstOrDefaultAsync(c => c.Id == checklistId);

                if (checklist == null)
                {
                    return new ServiceResponse()
                    {
                        Result = ServiceResponseType.Failed,
                        ErrorMessage = "Checklist no encontrado"
                    };
                }

                return new ServiceResponse()
                {
                    Result = ServiceResponseType.Succeded,
                    Data = checklist
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse()
                {
                    Result = ServiceResponseType.Failed,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<IEnumerable<ChecklistHeader>> GetAllChecklists()
        {
            return await _context.ChecklistHeaders
                .Include(c => c.Detalles)
                .OrderByDescending(c => c.Fecha)
                .ToListAsync();
        }

        public async Task<IEnumerable<ChecklistHeader>> GetChecklistsByDate(DateTime startDate, DateTime endDate)
        {
            return await _context.ChecklistHeaders
                .Include(c => c.Detalles)
                .Where(c => c.Fecha >= startDate && c.Fecha <= endDate.AddDays(1))
                .OrderBy(c => c.Fecha)
                .ToListAsync();
        }

        public async Task<IEnumerable<ChecklistHeader>> GetChecklistsByUser(Guid userId)
        {
            return await _context.ChecklistHeaders
                .Include(c => c.Detalles)
                .Where(c => c.UsuarioId == userId)
                .OrderBy(c => c.Fecha)
                .ToListAsync();
        }

        public async Task<IEnumerable<object>> GetSensorMeasurements(Guid deviceId, DateTime startDate, DateTime endDate)
        {
            var mediciones = await _context.ChecklistDetails
                .Include(d => d.ChecklistHeader)
                .Include(d => d.Dispositivo)
                .Where(d => d.DispositivoId == deviceId &&
                            d.ChecklistHeader.Fecha >= startDate &&
                            d.ChecklistHeader.Fecha <= endDate.AddDays(1))
                .OrderBy(d => d.ChecklistHeader.Fecha)
                .Select(d => new
                {
                    Fecha = d.ChecklistHeader.Fecha,
                    MedicionSensor = d.MedicionSensor,
                    MedicionManual = d.MedicionManual,
                    SensorNombre = d.Dispositivo.Device_Name
                })
                .ToListAsync();

            return mediciones;
        }

        public async Task GenerateChecklistCsv(Stream output, DateTime startDate, DateTime endDate, Guid? userId)
        {

            var checklistsQuery = _context.ChecklistHeaders
                .Include(c => c.Usuario)
                .Include(c => c.Detalles)
                    .ThenInclude(d => d.Dispositivo)
                .AsQueryable();

            if (userId.HasValue)
                checklistsQuery = checklistsQuery.Where(c => c.UsuarioId == userId.Value);

            checklistsQuery = checklistsQuery.Where(c => c.Fecha >= startDate && c.Fecha <= endDate.AddDays(1))
                              .OrderByDescending(c => c.Fecha);

            var checklists = await checklistsQuery.ToListAsync();

            using (var writer = new StreamWriter(output, new UTF8Encoding(true)))
            {
                await writer.WriteLineAsync("ChecklistId,Fecha,Usuario,Observacion,Dispositivo,Estado,MedicionSensor,MedicionManual,Unidad");

                foreach (var checklist in checklists)
                {
                    var usuarioNombre = checklist.Usuario != null
                        ? $"{checklist.Usuario.Name} {checklist.Usuario.Last_Name}".Trim()
                        : "N/A";

                    foreach (var detalle in checklist.Detalles)
                    {
                        var dispositivoNombre = detalle.Dispositivo?.Device_Name ?? "N/A";
                        var typeMeasurement = await _context.Measurement_Types.FirstOrDefaultAsync(j => j.Name == dispositivoNombre);

                        Unit_Measurement? unit = null;

                        if (typeMeasurement != null)
                        {
                            unit = await _context.Unit_Measurements
                                .FirstOrDefaultAsync(c => c.Id == typeMeasurement.Unit_MeasurementId);
                        }
                        // Escapar la observación para CSV
                        var observacion = checklist.Observacion ?? "";
                        observacion = observacion.Replace("\"", "\"\"");
                        observacion = $"\"{observacion}\"";

                        var line = string.Join(",",
                            checklist.Id,
                            checklist.Fecha.ToString("yyyy-MM-dd HH:mm"),
                            $"\"{usuarioNombre}\"",
                            observacion,
                            $"\"{dispositivoNombre}\"",
                            detalle.Estado,
                            detalle.MedicionSensor,
                            detalle.MedicionManual,
                            unit?.Name ?? "N/A"
                        );

                        await writer.WriteLineAsync(line);
                    }
                }

                await writer.FlushAsync();
            }
        }

        public async Task<List<ChecklistMeasurementDto>> ObtenerDatosPdf(ChecklistPdfRequestDto request)
        {
            return await _context.ChecklistDetails
                .Where(cd => cd.DispositivoId == request.SensorId &&
                             cd.ChecklistHeader.Fecha >= request.StartDate &&
                             cd.ChecklistHeader.Fecha <= request.EndDate)
                .Select(cd => new ChecklistMeasurementDto
                {
                    Fecha = cd.ChecklistHeader.Fecha,
                    MedicionSensor = (decimal?)cd.MedicionSensor,
                    MedicionManual = (decimal?)cd.MedicionManual,
                    SensorNombre = cd.Dispositivo.Device_Name
                })
                .OrderBy(x => x.Fecha)
                .ToListAsync();
        }

        public async Task GeneratePdf(Stream output, Guid sensorId, DateTime startDate, DateTime endDate, string chartImageBase64)
        {
            var data = await ObtenerDatosPdf(new ChecklistPdfRequestDto{ SensorId = sensorId, StartDate = startDate, EndDate = endDate});

            var firstItem = data.FirstOrDefault();
            var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/LogoWhite.jpg");

            // Convertir base64 del gráfico a bytes
            byte[] chartImageBytes = null;
            if (!string.IsNullOrEmpty(chartImageBase64))
            {
                var base64Data = Regex.Replace(chartImageBase64, @"^data:image\/[a-zA-Z]+;base64,", string.Empty);
                chartImageBytes = Convert.FromBase64String(base64Data);
            }

            QuestPDF.Settings.License = LicenseType.Community;

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(50);
                    page.Size(PageSizes.A4);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(14));

                    page.Header().Column(column =>
                    {
                        column.Item().Row(row =>
                        {
                            row.RelativeItem();
                            row.RelativeItem().AlignRight().Text(text =>
                            {
                                text.Span("Generado el: ").FontSize(9);
                                text.Span($"{DateTime.Now:dd-MM-yyyy HH:mm}").FontSize(9);
                            });
                        });

                        column.Item().Row(row =>
                        {
                            row.ConstantItem(80).Image(imagePath).FitWidth();
                            row.RelativeItem().AlignMiddle().PaddingLeft(50)
                                .Text("NUEVA NATURALEZA")
                                .SemiBold().FontSize(24).FontColor(Colors.Green.Darken2);
                        });
                    });

                    page.Content().Column(column =>
                    {
                        column.Spacing(10);

                        // Título del reporte
                        column.Item().AlignCenter()
                            .Text(firstItem?.SensorNombre ?? "Reporte de Sensor")
                            .SemiBold().FontSize(20).FontColor(Colors.Grey.Darken4);

                        column.Item().Height(20);

                        // Gráfico
                        if (chartImageBytes != null)
                        {
                            column.Item().AlignCenter().Image(chartImageBytes).FitWidth();
                        }

                        column.Item().Height(30);

                        // Tabla
                        column.Item().Table(table =>
                        {
                            var headerColor = "#2FA449";  // Verde institucional
                            var zebra1 = "#FFFFFF";
                            var zebra2 = "#F4F6F5";

                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(2);
                                c.RelativeColumn(2);
                                c.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(headerColor).Padding(6).Text("Sensor").FontSize(10).SemiBold().AlignCenter();
                                header.Cell().Background(headerColor).Padding(6).Text("Medición sensor").FontSize(10).SemiBold().AlignCenter();
                                header.Cell().Background(headerColor).Padding(6).Text("Medición manual").FontSize(10).SemiBold().AlignCenter();
                            });

                            bool odd = false;
                            foreach (var item in data)
                            {
                                odd = !odd;
                                var rowColor = odd ? zebra1 : zebra2;

                                table.Cell().Background(rowColor).Padding(5).Text(item.SensorNombre).FontSize(9).AlignCenter();
                                table.Cell().Background(rowColor).Padding(5).Text(item.MedicionSensor?.ToString() ?? "-").FontSize(9).AlignCenter();
                                table.Cell().Background(rowColor).Padding(5).Text(item.MedicionManual?.ToString() ?? "-").FontSize(9).AlignCenter();             
                            }


                        });
                    });

                    // ---------------------------
                    // FOOTER
                    // ---------------------------
                    page.Footer().DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Grey.Darken2))
                        .Column(column =>
                        {
                            column.Spacing(2);

                            column.Item().Row(row =>
                            {
                                row.RelativeItem(3).Text(text =>
                                {
                                    text.Line("Equipo de desarrolladores del grupo de investigación GISTFA");
                                    text.Line("Jorge Andrés Rodríguez Castaño y Carlos Andrés Ramos García");
                                });

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
