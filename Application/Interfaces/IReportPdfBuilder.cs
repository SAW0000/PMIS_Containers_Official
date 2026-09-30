using PMIS.Application.Services;

namespace PMIS.Application.Interfaces;

public interface IReportPdfBuilder
{
    ReportExportFile Build(ReportExportData data);
}
