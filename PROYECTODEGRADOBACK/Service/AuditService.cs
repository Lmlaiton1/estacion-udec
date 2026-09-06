using Microsoft.EntityFrameworkCore;
using RR_Nueva_Naturaleza.DAL;
using RR_Nueva_Naturaleza.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text.RegularExpressions;

namespace RR_Nueva_Naturaleza.Service
{
    public class AuditService : IAuditService
    {
        private readonly RR_Nueva_NaturalezaContext _context;

        public AuditService(RR_Nueva_NaturalezaContext context) //Constructor con inyeccion de dependencias
        {
            _context = context;
        }

        public async Task<ServiceResponse> AddAudit(string action, string observation, Guid userId, Guid deviceId)
        {
            try
            {
                await _context.Audits.AddAsync(new Audit()
                {
                    Id = Guid.NewGuid(),
                    Action = action,
                    Date = DateTime.Now,
                    Observation = observation,
                    UserId = userId,
                    DeviceId = deviceId
                });
                await _context.SaveChangesAsync();

                return new ServiceResponse()
                {
                    Result = ServiceResponseType.Succeded,
                    InformationMessage = "Audit add Correct"
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

        public async Task<Audit?> GetAudit(Guid AuditId)
        {
            return await _context.Audits.FindAsync(AuditId);
        }

        public async Task<IEnumerable<Audit>> GetAudits()
        {
            return await _context.Audits.ToListAsync();
        }

        public async Task<ServiceResponse> UpdateAudit(Guid AuditId, string action, string observation, Guid deviceId)
        {
            try
            {
                var audit = await _context.Audits.FindAsync(AuditId);
                if (audit == null)
                {
                    return new ServiceResponse()
                    {
                        Result = ServiceResponseType.Failed,
                        ErrorMessage = "Audit don't exist"
                    };
                }
                audit.Action = action;
                audit.Observation = observation;
                audit.DeviceId = deviceId;

                _context.Audits.Update(audit);

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

        public async Task<ServiceResponse> DeleteAudit(Guid AuditId)
        {
            try
            {
                var audit = await _context.Audits.FindAsync(AuditId);

                if (audit == null)
                {
                    return new ServiceResponse()
                    {
                        Result = ServiceResponseType.Failed,
                        ErrorMessage = "Audit don't exist"
                    };
                }
                _context.Audits.Remove(audit);
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

        public async Task<IEnumerable<Audit>> GetAuditByUserDate(Guid userId, DateTime startDate, DateTime endDate)
        {
            return await _context.Audits
                .Include(a => a.User)    // Propiedad de navegación
                .Include(a => a.Device)  // Propiedad de navegación
                .Where(m => m.UserId == userId &&
                            m.Date >= startDate &&
                            m.Date <= endDate.AddDays(1))
                .OrderBy(m => m.Date)
                .ToListAsync();

        }

        public async Task GetAuditByUserDateForPdf(Stream output, Guid userId, DateTime startDate, DateTime endDate, string chartImageBase64)
        {
            var lineItems = await GetAuditByUserDate(userId, startDate, endDate);
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

                    // Encabezado con logo y fecha
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

                    // Contenido del reporte
                    page.Content().PaddingTop(10).Column(column =>
                    {
                        column.Item().Height(30);
                        column.Spacing(5);

                        column.Item().AlignCenter()
                            .Text($"{firstItem?.User?.Name} {firstItem?.User?.Last_Name ?? "Reporte por usuario"}")
                            .SemiBold().FontSize(20).FontColor(Colors.Grey.Darken4);

                        column.Item().Height(20);

                        column.Item().Table(table =>
                        {
                            var headerColor = "#2FA449";  // Verde institucional
                            var zebra1 = "#FFFFFF";
                            var zebra2 = "#F4F6F5";

                            // Column layout
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(1.2f); // Fecha
                                columns.RelativeColumn(1.0f); // Dispositivo
                                columns.RelativeColumn(2.0f); // Acción
                                columns.RelativeColumn(1.2f); // Observación
                            });

                            // Header
                            table.Header(header =>
                            {
                                header.Cell().Background(headerColor).Padding(6)
                                      .Text("Fecha").FontColor("#FFFFFF").FontSize(10).SemiBold();

                                header.Cell().Background(headerColor).Padding(6)
                                      .Text("Dispositivo").FontColor("#FFFFFF").FontSize(10).SemiBold().AlignCenter();

                                header.Cell().Background(headerColor).Padding(6)
                                      .Text("Acción").FontColor("#FFFFFF").FontSize(10).SemiBold().AlignCenter();

                                header.Cell().Background(headerColor).Padding(6)
                                      .Text("Observación").FontColor("#FFFFFF").FontSize(10).SemiBold().AlignCenter();
                            });

                            // Rows
                            bool odd = false;
                            foreach (var item in lineItems)
                            {
                                odd = !odd;
                                var rowColor = odd ? zebra1 : zebra2;

                                table.Cell().Background(rowColor).Padding(5)
                                    .Text(item.Date.ToString("yyyy-MM-dd HH:mm:ss")).FontSize(9).AlignLeft();

                                table.Cell().Background(rowColor).Padding(5)
                                    .Text(item.Device?.Device_Name ?? "-").FontSize(9).AlignCenter();

                                table.Cell().Background(rowColor).Padding(5)
                                    .Text(item.Action ?? "-").FontSize(9).AlignLeft();

                                table.Cell().Background(rowColor).Padding(5)
                                    .Text(item.Observation ?? "-").FontSize(9).AlignCenter();
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
    }
}
