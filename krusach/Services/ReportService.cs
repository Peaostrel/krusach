using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Previewer;
using krusach.Models;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace krusach.Services
{
    public class ReportService
    {
        static ReportService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public static void GeneratePriceList(IEnumerable<Product> products, string filePath)
        {
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Verdana));

                    page.Header().Text("ПРАЙС-ЛИСТ ОПТОВОЙ БАЗЫ").FontSize(20).SemiBold().FontColor(Colors.Blue.Medium);

                    page.Content().PaddingVertical(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(CellStyle).Text("Название");
                            header.Cell().Element(CellStyle).Text("Артикул");
                            header.Cell().Element(CellStyle).Text("Цена");
                            header.Cell().Element(CellStyle).Text("Остаток");

                            static IContainer CellStyle(IContainer container)
                            {
                                return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                            }
                        });

                        foreach (var item in products)
                        {
                            table.Cell().Element(CellStyle).Text(item.Name);
                            table.Cell().Element(CellStyle).Text(item.Sku);
                            table.Cell().Element(CellStyle).Text($"{item.WholesalePrice:N2} ₽");
                            table.Cell().Element(CellStyle).Text($"{item.StockQuantity} шт");

                            static IContainer CellStyle(IContainer container)
                            {
                                return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                            }
                        }
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Страница ");
                        x.CurrentPageNumber();
                    });
                });
            }).GeneratePdf(filePath);
        }

        public static void GenerateInvoice(Order order, string filePath)
        {
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Verdana));

                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text($"НАКЛАДНАЯ №{order.Id}").FontSize(20).SemiBold().FontColor(Colors.Blue.Medium);
                            col.Item().Text($"Дата: {order.OrderDate:dd.MM.yyyy}");
                        });

                        row.RelativeItem().AlignRight().Column(col =>
                        {
                            col.Item().Text("Оптовая База 2.0").FontSize(14).SemiBold();
                            col.Item().Text("Контрагент: " + (order.Contractor?.CompanyName ?? "---"));
                        });
                    });

                    page.Content().PaddingVertical(20).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(30);
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(CellStyle).Text("№");
                            header.Cell().Element(CellStyle).Text("Товар");
                            header.Cell().Element(CellStyle).Text("Кол-во");
                            header.Cell().Element(CellStyle).Text("Цена");
                            header.Cell().Element(CellStyle).Text("Сумма");

                            static IContainer CellStyle(IContainer container)
                            {
                                return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                            }
                        });

                        int index = 1;
                        foreach (var detail in order.OrderDetails)
                        {
                            table.Cell().Element(CellStyle).Text(index++.ToString());
                            table.Cell().Element(CellStyle).Text(detail.Product?.Name ?? "---");
                            table.Cell().Element(CellStyle).Text(detail.Quantity.ToString());
                            table.Cell().Element(CellStyle).Text($"{detail.UnitPrice:N2} ₽");
                            table.Cell().Element(CellStyle).Text($"{(detail.Quantity * detail.UnitPrice):N2} ₽");

                            static IContainer CellStyle(IContainer container)
                            {
                                return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                            }
                        }
                    });

                    page.Footer().PaddingTop(20).Column(col =>
                    {
                        col.Item().AlignRight().Text($"ИТОГО К ОПЛАТЕ: {order.TotalSum:N2} ₽").FontSize(14).SemiBold();
                        col.Item().PaddingTop(10).Text("Подпись: _________________").AlignRight();
                    });
                });
            }).GeneratePdf(filePath);
        }
    }
}
