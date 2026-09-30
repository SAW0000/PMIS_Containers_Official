using PMIS.Application.Interfaces;
using PMIS.Application.Services;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PMIS.Infrastructure.Reporting;

public class ReportPdfBuilder : IReportPdfBuilder
{
    private const string FontFamily = "Cairo";
    private static bool _fontsRegistered;
    private static readonly object FontLock = new();

    public ReportPdfBuilder(IWebHostEnvironment environment)
    {
        EnsureFontsRegistered(environment);
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public ReportExportFile Build(ReportExportData data)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.ContentFromRightToLeft();
                page.DefaultTextStyle(style => style.FontFamily(FontFamily).FontSize(10));

                page.Header().Element(c => ComposeHeader(c, data));
                page.Content().Element(c => ComposeContent(c, data));
                page.Footer().AlignCenter().Text(text =>
                {
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        });

        var fileName = $"pmis-report-{data.Period.ToString().ToLowerInvariant()}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.pdf";
        return new ReportExportFile(document.GeneratePdf(), "application/pdf", fileName);
    }

    private static void ComposeHeader(QuestPDF.Infrastructure.IContainer container, ReportExportData data)
    {
        container.Column(column =>
        {
            column.Item().Text("التقارير").FontSize(18).Bold();
            column.Item().Text($"نظرة عامة على حالة الطلبات والرخص المفعّلة — {ReportStatusLabels.PeriodLabels[data.Period.ToString()]}").FontSize(10).FontColor(Colors.Grey.Darken1);
            column.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
        });
    }

    private static void ComposeContent(QuestPDF.Infrastructure.IContainer container, ReportExportData data)
    {
        container.PaddingTop(10).Column(column =>
        {
            column.Spacing(14);
            column.Item().Element(c => ComposeStatSummary(c, data));
            column.Item().Element(c => ComposeCompanyBreakdown(c, data));
            column.Item().Element(c => ComposeReasonBreakdown(c, data));
            column.Item().Element(c => ComposeRequestsTable(c, data));
        });
    }

    private static void ComposeStatSummary(QuestPDF.Infrastructure.IContainer container, ReportExportData data)
    {
        container.Column(column =>
        {
            column.Item().Text("نظرة عامة").FontSize(13).Bold();
            column.Item().PaddingTop(4).Row(row =>
            {
                StatCell(row, "إجمالي الطلبات", data.Requests.Count.ToString());
                StatCell(row, ReportStatusLabels.For("returned"), data.Requests.Count(r => r.Status == "returned").ToString());
                StatCell(row, ReportStatusLabels.For("rejected"), data.Requests.Count(r => r.Status == "rejected").ToString());
                StatCell(row, ReportStatusLabels.For("activated"), data.Requests.Count(r => r.Status == "activated").ToString());
            });
            column.Item().PaddingTop(6).Text($"الالتزام بمهلة الرد ({data.SlaTargetDays} أيام عمل): {data.SlaPercent}% ({data.SlaWithinCount}/{data.SlaTotalRespondedCount})");
        });
    }

    private static void StatCell(QuestPDF.Fluent.RowDescriptor row, string label, string value)
    {
        row.RelativeItem().Column(column =>
        {
            column.Item().Text(value).FontSize(16).Bold();
            column.Item().Text(label).FontSize(9).FontColor(Colors.Grey.Darken1);
        });
    }

    private static void ComposeCompanyBreakdown(QuestPDF.Infrastructure.IContainer container, ReportExportData data)
    {
        container.Column(column =>
        {
            column.Item().Text("الطلبات حسب الجهة").FontSize(13).Bold();
            if (data.CompanyBreakdown.Count == 0)
            {
                column.Item().Text("لا توجد بيانات").FontColor(Colors.Grey.Darken1);
                return;
            }

            column.Item().PaddingTop(4).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                });

                TableHeaderRow(table, "الشركة", "الإجمالي", "مفعّلة", "مرفوضة");
                foreach (var company in data.CompanyBreakdown)
                {
                    table.Cell().Element(CellStyle).Text(company.Company);
                    table.Cell().Element(CellStyle).Text(company.Total.ToString());
                    table.Cell().Element(CellStyle).Text(company.Activated.ToString());
                    table.Cell().Element(CellStyle).Text(company.Rejected.ToString());
                }
            });
        });
    }

    private static void ComposeReasonBreakdown(QuestPDF.Infrastructure.IContainer container, ReportExportData data)
    {
        container.Column(column =>
        {
            column.Item().Text("أسباب الإعادة والرفض").FontSize(13).Bold();
            if (data.ReasonBreakdown.Count == 0)
            {
                column.Item().Text("لا توجد طلبات معادة أو مرفوضة").FontColor(Colors.Grey.Darken1);
                return;
            }

            column.Item().PaddingTop(4).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(1);
                });

                TableHeaderRow(table, "السبب", "العدد");
                foreach (var reason in data.ReasonBreakdown)
                {
                    table.Cell().Element(CellStyle).Text(reason.LabelArabic);
                    table.Cell().Element(CellStyle).Text(reason.Count.ToString());
                }
            });
        });
    }

    private static void ComposeRequestsTable(QuestPDF.Infrastructure.IContainer container, ReportExportData data)
    {
        container.Column(column =>
        {
            column.Item().Text("الطلبات").FontSize(13).Bold();
            column.Item().PaddingTop(4).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(1.5f);
                    columns.RelativeColumn(1.5f);
                });

                TableHeaderRow(table, "رقم الطلب", "مقدّم الطلب", "الشركة", "المشروع", "التاريخ", "الحالة");
                foreach (var request in data.Requests)
                {
                    table.Cell().Element(CellStyleLtr).Text(request.Id);
                    table.Cell().Element(CellStyle).Text(request.Applicant);
                    table.Cell().Element(CellStyle).Text(request.Company);
                    table.Cell().Element(CellStyle).Text(request.Project);
                    table.Cell().Element(CellStyleLtr).Text(request.Date);
                    table.Cell().Element(CellStyle).Text(ReportStatusLabels.For(request.Status));
                }
            });
        });
    }

    private static void TableHeaderRow(TableDescriptor table, params string[] headers)
    {
        foreach (var header in headers)
        {
            table.Cell().Element(c => c.Background(Colors.Grey.Lighten3).Padding(4)).Text(header).Bold();
        }
    }

    private static QuestPDF.Infrastructure.IContainer CellStyle(QuestPDF.Infrastructure.IContainer container)
        => container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(4).PaddingHorizontal(2);

    private static QuestPDF.Infrastructure.IContainer CellStyleLtr(QuestPDF.Infrastructure.IContainer container)
        => CellStyle(container).ContentFromLeftToRight();

    private static void EnsureFontsRegistered(IWebHostEnvironment environment)
    {
        if (_fontsRegistered)
        {
            return;
        }

        lock (FontLock)
        {
            if (_fontsRegistered)
            {
                return;
            }

            var fontsDir = Path.Combine(environment.WebRootPath, "assets", "fonts");
            RegisterFont(fontsDir, "Cairo-Regular.ttf");
            RegisterFont(fontsDir, "Cairo-Medium.ttf");
            RegisterFont(fontsDir, "Cairo-SemiBold.ttf");
            RegisterFont(fontsDir, "Cairo-Bold.ttf");

            _fontsRegistered = true;
        }
    }

    private static void RegisterFont(string fontsDir, string fileName)
    {
        var path = Path.Combine(fontsDir, fileName);
        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"Required PDF font file is missing: {path}");
        }

        using var stream = File.OpenRead(path);
        FontManager.RegisterFont(stream);
    }
}
