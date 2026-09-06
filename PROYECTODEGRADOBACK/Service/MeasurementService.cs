using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RR_Nueva_Naturaleza.DAL;
using RR_Nueva_Naturaleza.Models;
using System.Text;
using System.Text.RegularExpressions;

namespace RR_Nueva_Naturaleza.Service
{
    public class MeasurementService : IMeasurementService
    {
        private readonly RR_Nueva_NaturalezaContext _context;

        public MeasurementService(RR_Nueva_NaturalezaContext context) //Constructor con inyeccion de dependencias
        {
            _context = context;
        }

        public async Task<ServiceResponse> AddMeasurement(decimal value, DateTime date, Guid deviceId, Guid measuremen_TipeId)
        {
            try
            {
                await _context.Measurements.AddAsync(new Measurement()
                {
                    Id = Guid.NewGuid(),
                    Value = value,
                    Date = date,
                    DeviceId = deviceId,
                    Measurement_TypeId = measuremen_TipeId
                });
                await _context.SaveChangesAsync();

                return new ServiceResponse()
                {
                    Result = ServiceResponseType.Succeded,
                    InformationMessage = "Measurement add Correct"
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

        public async Task<Measurement?> GetMeasurement(Guid MeasurementId)
        {
            return await _context.Measurements.FindAsync(MeasurementId);
        }

        public async Task<IEnumerable<Measurement>> GetMeasurements()
        {
            return await _context.Measurements.ToListAsync();
        }

        public async Task<ServiceResponse> UpdateMeasurement(Guid MeasurementId, decimal value, DateTime date)
        {
            try
            {
                var measurement = await _context.Measurements.FindAsync(MeasurementId);
                if (measurement == null)
                {
                    return new ServiceResponse()
                    {
                        Result = ServiceResponseType.Failed,
                        ErrorMessage = "Measurement don't exist"
                    };
                }
                measurement.Value = value;
                measurement.Date = date;

                _context.Measurements.Update(measurement);

                await _context.SaveChangesAsync();
                return new ServiceResponse()
                {
                    Result = ServiceResponseType.Succeded
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

        public async Task<ServiceResponse> DeleteMeasurement(Guid MeasurementId)
        {
            try
            {
                var measurement = await _context.Measurements.FindAsync(MeasurementId);

                if (measurement == null)
                {
                    return new ServiceResponse()
                    {
                        Result = ServiceResponseType.Failed,
                        ErrorMessage = "Measurement don't exist"
                    };
                }
                _context.Measurements.Remove(measurement);
                await _context.SaveChangesAsync();

                return new ServiceResponse()
                {
                    Result = ServiceResponseType.Succeded

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

        public async Task<IEnumerable<Measurement>> GetMeasurementsByDate(Guid measurementTypeId, DateTime startDate, DateTime endDate)
        {
            return await _context.Measurements.Include(x => x.Measurement_Type).ThenInclude(x => x.Unit_Measurement)
                .Where(m => m.Measurement_TypeId == measurementTypeId &&
                            m.Date >= startDate &&
                            m.Date <= endDate.AddDays(1)).OrderBy(m => m.Date)
                .ToListAsync();
        }

        public async Task GetMeasurementsForPdf(Stream output, Guid measurementTypeId, DateTime startDate, DateTime endDate, string chartImageBase64)
        {
            var lineItems = await GetMeasurementsByDate(measurementTypeId, startDate, endDate);
            var firstItem = lineItems.FirstOrDefault();
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
                    page.DefaultTextStyle(x => x.FontSize(16));

                    // Encabezado
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
                                .Text("NUEVA NATURALEZA")
                                .SemiBold().FontSize(24).FontColor(Colors.Green.Darken2);
                        });
                    });

                    // Contenido
                    page.Content().PaddingTop(10).Column(column =>
                    {
                        column.Item().Height(30);
                        column.Spacing(5);

                        column.Item().AlignCenter()
                            .Text(firstItem?.Measurement_Type?.Name ?? "Reporte de Mediciones")
                            .SemiBold().FontSize(20).FontColor(Colors.Grey.Darken4);

                        column.Item().Height(20);

                        if (chartImageBytes != null)
                        {
                            column.Item().AlignCenter().Image(chartImageBytes).FitWidth();
                        }

                        column.Item().Height(20);

                        // Tabla con estilo local
                        column.Item().Table(table =>
                        {
                            var headerColor = "#2FA449";  // Verde institucional
                            var zebra1 = "#FFFFFF";
                            var zebra2 = "#F4F6F5";

                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(headerColor).Padding(6).Text("Fecha").FontColor("#FFFFFF").FontSize(10).SemiBold().AlignCenter();
                                header.Cell().Background(headerColor).Padding(6).Text("Valor").FontColor("#FFFFFF").FontSize(10).SemiBold().AlignCenter();
                                header.Cell().Background(headerColor).Padding(6).Text("Unidad").FontColor("#FFFFFF").FontSize(10).SemiBold().AlignCenter();
                            });

                            bool odd = false;
                            foreach (var item in lineItems)
                            {
                                odd = !odd;
                                var rowColor = odd ? zebra1 : zebra2;

                                table.Cell().Background(rowColor).Padding(5).Text(item?.Date.ToString("yyyy-MM-dd HH:mm:ss") ?? "-").FontSize(9).AlignLeft();

                                table.Cell().Background(rowColor).Padding(5).Text(item?.Value.ToString() ?? "-").FontSize(9).AlignCenter();

                                table.Cell().Background(rowColor).Padding(5).Text(item?.Measurement_Type?.Unit_Measurement?.Name ?? "-").FontSize(9).AlignCenter();

                            }
                        });
                    });

                    // Pie de página
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

        public async Task GenerateAllMeasurementsCsv(Stream output, DateTime startDate, DateTime endDate, Guid? userId)
        {
            // 🔹 Traer todas las mediciones de sensores en el rango
            var measurementsQuery = _context.Measurements
                .Include(m => m.Device)
                    .ThenInclude(d => d.Device_Type)
                .Include(m => m.Measurement_Type)
                    .ThenInclude(mt => mt.Unit_Measurement)
                .AsQueryable();

            measurementsQuery = measurementsQuery
                .Where(m => m.Date >= startDate && m.Date <= endDate.AddDays(1))
                .Where(m => m.Device.Device_Type.Device_Type_Name == "Sensor")
                .OrderByDescending(m => m.Date);

            var measurements = await measurementsQuery.ToListAsync();

            // 🔹 Agrupar por fecha
            var grouped = measurements
                .GroupBy(m => m.Date.ToString("yyyy-MM-dd HH:mm:ss"))
                .OrderByDescending(g => g.Key);

            // 🔹 Identificar todos los tipos de medición distintos en el periodo
            var measurementTypes = measurements
                .Select(m => new
                {
                    Name = m.Measurement_Type?.Name ?? "Desconocido",
                    Unit = m.Measurement_Type?.Unit_Measurement?.Name ?? ""
                })
                .Distinct()
                .OrderBy(mt => mt.Name) // opcional, para mantener orden alfabético
                .ToList();

            using (var writer = new StreamWriter(output, new UTF8Encoding(true)))
            {
                // 🔹 Construir encabezado dinámico
                var header = new List<string> { "FechaHora" };
                foreach (var mt in measurementTypes)
                {
                    header.Add(mt.Name);
                    header.Add($"Unidad{mt.Name}");
                }
                await writer.WriteLineAsync(string.Join(",", header));

                // 🔹 Construir cada fila
                foreach (var group in grouped)
                {
                    var row = new List<string> { group.Key };

                    foreach (var mt in measurementTypes)
                    {
                        var measurement = group.FirstOrDefault(m => m.Measurement_Type.Name == mt.Name);

                        if (measurement != null)
                        {
                            row.Add(measurement.Value.ToString());
                            row.Add(measurement.Measurement_Type?.Unit_Measurement?.Name ?? "");
                        }
                        else
                        {
                            row.Add(""); // valor vacío
                            row.Add(""); // unidad vacía
                        }
                    }

                    await writer.WriteLineAsync(string.Join(",", row));
                }

                await writer.FlushAsync();
            }
        }

    }
}
