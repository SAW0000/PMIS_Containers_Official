using ClosedXML.Excel;
using PMIS.Application.Interfaces;
using PMIS.Application.Services;

namespace PMIS.Infrastructure.Reporting;

public class ReportExcelBuilder : IReportExcelBuilder
{
    private static readonly XLColor BrandDark = XLColor.FromHtml("#006600");   
    private static readonly XLColor Brand = XLColor.FromHtml("#008000");      
    private static readonly XLColor Tint = XLColor.FromHtml("#e5f2e5");       
    private static readonly XLColor TintAlt = XLColor.FromHtml("#d5ead5");    
    private static readonly XLColor TextDark = XLColor.FromHtml("#1f2b1c");   
    private static readonly XLColor TextMid = XLColor.FromHtml("#586b52");    
    private static readonly XLColor White = XLColor.White;
    private static readonly XLColor StripeColor = XLColor.FromHtml("#f4f9f1"); 
    private const string BaseFont = "Calibri";

    public ReportExportFile Build(ReportExportData data)
    {
        using var workbook = new XLWorkbook();
        workbook.Style.Font.FontName = BaseFont;
        WriteSummarySheet(workbook, data);
        WriteRequestsSheet(workbook, data);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        var fileName = $"pmis-report-{data.Period.ToString().ToLowerInvariant()}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.xlsx";
        return new ReportExportFile(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    private static void WriteSummarySheet(XLWorkbook workbook, ReportExportData data)
    {
        var sheet = workbook.Worksheets.Add("ملخص التقرير");
        sheet.RightToLeft = true;
        sheet.ShowGridLines = false;

        var row = WriteTitleBanner(sheet, data);
        row++;
        row = WriteStatCards(sheet, data, row);
        row++;
        row = WriteCompanyBreakdown(sheet, data, row);
        row++;
        WriteReasonBreakdown(sheet, data, row);

        sheet.Column(1).Width = 26;
        for (var col = 2; col <= 4; col++)
        {
            sheet.Column(col).Width = 16;
        }
    }

    private static int WriteTitleBanner(IXLWorksheet sheet, ReportExportData data)
    {
        var band = sheet.Range(1, 1, 2, 4);
        band.Merge();
        band.Style.Fill.BackgroundColor = BrandDark;

        var cell = sheet.Cell(1, 1);
        cell.Value = $"التقارير — {ReportStatusLabels.PeriodLabels[data.Period.ToString()]}";
        cell.Style.Font.FontColor = White;
        cell.Style.Font.FontSize = 18;
        cell.Style.Font.Bold = true;
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

        sheet.Row(1).Height = 20;
        sheet.Row(2).Height = 20;
        return 3;
    }

    private static int WriteStatCards(IXLWorksheet sheet, ReportExportData data, int row)
    {
        var cards = new (string Label, int Value)[]
        {
            ("إجمالي الطلبات", data.Requests.Count),
            ("معاد للمستفيد", data.Requests.Count(r => r.Status == "returned")),
            ("مرفوض", data.Requests.Count(r => r.Status == "rejected")),
            ("تم التفعيل", data.Requests.Count(r => r.Status == "activated"))
        };

        for (var i = 0; i < cards.Length; i++)
        {
            var col = i + 1;
            var valueCell = sheet.Cell(row, col);
            valueCell.Value = cards[i].Value;
            valueCell.Style.Font.FontSize = 20;
            valueCell.Style.Font.Bold = true;
            valueCell.Style.Font.FontColor = BrandDark;
            valueCell.Style.Fill.BackgroundColor = Tint;
            valueCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            valueCell.Style.Border.TopBorder = XLBorderStyleValues.Medium;
            valueCell.Style.Border.TopBorderColor = Brand;

            var labelCell = sheet.Cell(row + 1, col);
            labelCell.Value = cards[i].Label;
            labelCell.Style.Font.FontSize = 10;
            labelCell.Style.Font.FontColor = TextMid;
            labelCell.Style.Fill.BackgroundColor = Tint;
            labelCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        sheet.Row(row).Height = 26;

        var slaRow = row + 2;
        var slaBand = sheet.Range(slaRow, 1, slaRow, 4);
        slaBand.Merge();
        var slaCell = sheet.Cell(slaRow, 1);
        slaCell.Value = $"الالتزام بمهلة الرد ({data.SlaTargetDays} أيام عمل): {data.SlaPercent}% ({data.SlaWithinCount}/{data.SlaTotalRespondedCount})";
        slaCell.Style.Font.Bold = true;
        slaCell.Style.Font.FontColor = BrandDark;
        slaCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        return slaRow + 1;
    }

    private static int WriteCompanyBreakdown(IXLWorksheet sheet, ReportExportData data, int row)
    {
        row = WriteSectionTitle(sheet, "الطلبات حسب الجهة", row, 4);
        row = WriteTableHeader(sheet, row, "الشركة", "الإجمالي", "مفعّلة", "مرفوضة");

        var startRow = row;
        foreach (var company in data.CompanyBreakdown)
        {
            sheet.Cell(row, 1).Value = SanitizeForExcel(company.Company);
            sheet.Cell(row, 2).Value = company.Total;
            sheet.Cell(row, 3).Value = company.Activated;
            sheet.Cell(row, 4).Value = company.Rejected;
            row++;
        }

        StripeRows(sheet, startRow, row - 1, 1, 4);
        BorderTable(sheet, startRow - 1, row - 1, 1, 4);
        return row;
    }

    private static void WriteReasonBreakdown(IXLWorksheet sheet, ReportExportData data, int row)
    {
        row = WriteSectionTitle(sheet, "أسباب الإعادة والرفض", row, 2);
        row = WriteTableHeader(sheet, row, "السبب", "العدد");

        var startRow = row;
        foreach (var reason in data.ReasonBreakdown)
        {
            sheet.Cell(row, 1).Value = SanitizeForExcel(reason.LabelArabic);
            sheet.Cell(row, 2).Value = reason.Count;
            row++;
        }

        StripeRows(sheet, startRow, row - 1, 1, 2);
        BorderTable(sheet, startRow - 1, row - 1, 1, 2);
    }

    private static int WriteSectionTitle(IXLWorksheet sheet, string title, int row, int spanCols)
    {
        var band = sheet.Range(row, 1, row, spanCols);
        band.Merge();
        var cell = sheet.Cell(row, 1);
        cell.Value = title;
        cell.Style.Font.Bold = true;
        cell.Style.Font.FontSize = 13;
        cell.Style.Font.FontColor = TextDark;
        cell.Style.Border.RightBorder = XLBorderStyleValues.Thick;
        cell.Style.Border.RightBorderColor = Brand;
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        cell.Style.Alignment.Indent = 1;
        return row + 1;
    }

    private static int WriteTableHeader(IXLWorksheet sheet, int row, params string[] headers)
    {
        for (var col = 0; col < headers.Length; col++)
        {
            var cell = sheet.Cell(row, col + 1);
            cell.Value = headers[col];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = White;
            cell.Style.Fill.BackgroundColor = Brand;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }

        sheet.Row(row).Height = 20;
        return row + 1;
    }

    private static void StripeRows(IXLWorksheet sheet, int firstRow, int lastRow, int firstCol, int lastCol)
    {
        for (var row = firstRow; row <= lastRow; row++)
        {
            if ((row - firstRow) % 2 == 1)
            {
                sheet.Range(row, firstCol, row, lastCol).Style.Fill.BackgroundColor = StripeColor;
            }

            sheet.Range(row, firstCol, row, lastCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            sheet.Cell(row, firstCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        }
    }

    private static void BorderTable(IXLWorksheet sheet, int headerRow, int lastRow, int firstCol, int lastCol)
    {
        var range = sheet.Range(headerRow, firstCol, lastRow, lastCol);
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
        range.Style.Border.OutsideBorderColor = Brand;
        range.Style.Border.InsideBorder = XLBorderStyleValues.Hair;
        range.Style.Border.InsideBorderColor = TintAlt;
    }

    private static void WriteRequestsSheet(XLWorkbook workbook, ReportExportData data)
    {
        var sheet = workbook.Worksheets.Add("الطلبات");
        sheet.RightToLeft = true;
        sheet.ShowGridLines = false;

        string[] headers = ["رقم الطلب", "مقدّم الطلب", "الشركة", "المشروع", "النظام المستهدف", "تاريخ التقديم", "الحالة"];
        var headerRow = WriteTableHeader(sheet, 1, headers);

        var row = headerRow;
        foreach (var request in data.Requests)
        {
            sheet.Cell(row, 1).Value = request.Id;
            sheet.Cell(row, 2).Value = SanitizeForExcel(request.Applicant);
            sheet.Cell(row, 3).Value = SanitizeForExcel(request.Company);
            sheet.Cell(row, 4).Value = SanitizeForExcel(request.Project);
            sheet.Cell(row, 5).Value = request.TargetSystem;
            sheet.Cell(row, 6).Value = request.Date;
            var statusCell = sheet.Cell(row, 7);
            statusCell.Value = ReportStatusLabels.For(request.Status);
            StyleStatusBadge(statusCell, request.Status);
            row++;
        }

        if (row > headerRow)
        {
            StripeRows(sheet, headerRow, row - 1, 1, headers.Length);
            BorderTable(sheet, 1, row - 1, 1, headers.Length);
        }

        sheet.SheetView.Freeze(1, 0);
        sheet.Columns().AdjustToContents();
        sheet.Column(3).Width = Math.Min(sheet.Column(3).Width, 30);
        sheet.Column(4).Width = Math.Min(sheet.Column(4).Width, 30);
    }

    private static string SanitizeForExcel(string value) =>
        !string.IsNullOrEmpty(value) && value[0] is '=' or '+' or '-' or '@'
            ? "'" + value
            : value;

    private static void StyleStatusBadge(IXLCell cell, string statusKey)
    {
        var isPositive = statusKey is "approved" or "activated";
        cell.Style.Font.Bold = true;
        cell.Style.Font.FontColor = isPositive ? BrandDark : TextMid;
        cell.Style.Fill.BackgroundColor = isPositive ? Tint : XLColor.FromHtml("#f0f0f0");
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
    }
}
