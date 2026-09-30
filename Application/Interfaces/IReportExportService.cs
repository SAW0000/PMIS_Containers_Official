using PMIS.Application.DTOs;
using PMIS.Domain.Enums;

namespace PMIS.Application.Interfaces;

public interface IReportExportService
{
    Task<ReportExportFile> ExportAsync(
        Guid currentUserId, UserRole currentRole, ReportPeriod period, ReportExportFormat format,
        CancellationToken cancellationToken = default);
}

public record ReportExportFile(byte[] Content, string ContentType, string FileName);
