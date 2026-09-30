using PMIS.Application.Services;

namespace PMIS.Application.Interfaces;

public interface IReportExcelBuilder
{
    ReportExportFile Build(ReportExportData data);
}
