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

        public async Task<IEnumerable<LecturaMeteo>> GetLecturasByEstacion(int numero)
        {
            return await _context.Lecturas_Meteo
                .Where(l => l.EstacionNumero == numero)
                .OrderByDescending(l => l.FechaHora)
                .ToListAsync();
        }

        public async Task<IEnumerable<LecturaMeteo>> GetLecturasByEstacionAndVariable(int numero, string variable)
        {
            return await _context.Lecturas_Meteo
                .Where(l => l.EstacionNumero == numero && l.Variable == variable)
                .OrderByDescending(l => l.FechaHora)
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
                    .Where(l => l.FechaHora.TimeOfDay >= filtro.HoraInicio.Value && l.FechaHora.TimeOfDay <= filtro.HoraFin.Value)
                    .ToList();
            }

            return lecturas;
        }

        public async Task GenerarPdf(Stream output, LecturaMeteoPdfRequestDto filtro)
        {
            var lecturas = (await GetLecturasParaReporte(filtro)).ToList();
            var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/UdeC.jpg");

            var estacionTexto = filtro.EstacionNumero.HasValue ? $"Estación {filtro.EstacionNumero}" : "Todas las estaciones";
            var variableTexto = string.IsNullOrEmpty(filtro.Variable) ? "Todas las variables" : filtro.Variable;
            var horaTexto = (filtro.HoraInicio.HasValue && filtro.HoraFin.HasValue)
                ? $" · Horario: {filtro.HoraInicio:hh\\:mm} a {filtro.HoraFin:hh\\:mm}"
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
