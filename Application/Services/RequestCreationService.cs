using Amazon.S3;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PMIS.Application.DTOs;
using PMIS.Application.Interfaces;
using PMIS.Domain.Entities;
using PMIS.Domain.Enums;
using PMIS.Domain.ValueObjects;
using PMIS.Infrastructure.Persistence;

namespace PMIS.Application.Services;

public partial class RequestCreationService(
    PmisDbContext context, IUnitOfWork unitOfWork, IAuditLogRepository auditLogRepository,
    INotificationService notificationService, ISystemSettingsService systemSettingsService,
    IAttachmentStorage attachmentStorage, ILogger<RequestCreationService> logger)
    : IRequestCreationService
{
    private static readonly RequestStatus[] TerminalStatuses =
    [
        RequestStatus.Cancelled,
        RequestStatus.Rejected,
        RequestStatus.Activated
    ];

    private const int MaxRequestNumberInsertAttempts = 3;

    private const string RequestNotFoundError = "الطلب غير موجود";
    private const string RequestEntityType = "Request";

    public Task<(RequestCreationResultDto? Result, string? Error)> SaveDraftAsync(
        RequestCreationDto dto, RequestActor actor, CancellationToken cancellationToken = default)
        => CreateAsync(dto, actor, RequestStatus.Draft, cancellationToken);

    public Task<(RequestCreationResultDto? Result, string? Error)> SubmitAsync(
        RequestCreationDto dto, RequestActor actor, CancellationToken cancellationToken = default)
        => CreateAsync(dto, actor, RequestStatus.Submitted, cancellationToken);

    public async Task<string?> SubmitExistingDraftAsync(
        Guid requestId, RequestActor actor, bool agreedToTerms, CancellationToken cancellationToken = default)
    {
        if (!agreedToTerms) return "يجب الموافقة على الشروط والأحكام قبل إرسال الطلب";

        var request = await context.Requests.FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken);
        if (request is null || request.UserId != actor.UserId) return RequestNotFoundError;
        if (request.Status != RequestStatus.Draft) return "لا يمكن إرسال هذا الطلب في حالته الحالية";

        request.Status = RequestStatus.Submitted;
        request.RequestDate = DateTime.UtcNow;

        await unitOfWork.RequestHistories.AddAsync(new RequestHistory
        {
            Request = request,
            EventLabel = "إرسال الطلب",
            EventDate = DateTime.UtcNow
        }, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await auditLogRepository.AddAsync(new AuditLogEntry
        {
            EntityType = RequestEntityType,
            EntityId = request.Id,
            Action = "RequestSubmitted",
            RequestId = request.Id,
            RequestNumberSnapshot = request.RequestNumber,
            PerformedBy = new AuditLogActor
            {
                UserId = actor.UserId,
                FullNameSnapshot = actor.FullName,
                Role = actor.Role.ToString()
            },
            FromState = "Draft",
            ToState = "Submitted",
            Timestamp = DateTime.UtcNow,
            IpAddress = actor.IpAddress
        }, cancellationToken);

        await notificationService.NotifyRequestSubmittedAsync(request, cancellationToken);
        return null;
    }

    public async Task<RequestDraftDetailsDto?> GetDraftDetailsAsync(
        Guid requestId, Guid userId, CancellationToken cancellationToken = default)
    {
        var request = await context.Requests
            .Include(r => r.Company)
            .Include(r => r.Project)
            .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken);

        if (request is null || request.UserId != userId
            || request.Status is not (RequestStatus.Draft or RequestStatus.ReturnedForCorrection))
        {
            return null;
        }

        return new RequestDraftDetailsDto
        {
            RequestId = request.Id,
            FirstName = request.ApplicantFirstName ?? string.Empty,
            LastName = request.ApplicantLastName ?? string.Empty,
            Email = request.ApplicantEmail ?? string.Empty,
            Mobile = request.ApplicantMobile ?? string.Empty,
            UserType = request.UserTypeDisplay?.ToString() ?? string.Empty,
            CompanyName = request.Company.CompanyName ?? string.Empty,
            CommercialNo = request.Company.CommercialNo ?? string.Empty,
            CompanyActivity = request.Company.CompanyActivity,
            SystemId = request.SystemId,
            ProjectName = request.Project.ProjectName ?? string.Empty,
            ContractNumber = request.Project.ContractNumber ?? string.Empty,
            ProjectNumber = request.Project.ProjectNumber
        };
    }

    public async Task<string?> CancelDraftAsync(
        Guid requestId, RequestActor actor, CancellationToken cancellationToken = default)
    {
        var request = await context.Requests.FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken);
        if (request is null || request.UserId != actor.UserId) return RequestNotFoundError;
        if (request.Status != RequestStatus.Draft) return "لا يمكن حذف هذا الطلب في حالته الحالية";

        request.Status = RequestStatus.Cancelled;

        await unitOfWork.RequestHistories.AddAsync(new RequestHistory
        {
            Request = request,
            EventLabel = "حذف المسودة",
            EventDate = DateTime.UtcNow
        }, cancellationToken);

        var removedAttachments = await RemoveAttachmentRowsAsync(requestId, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await DeleteAttachmentObjectsAsync(removedAttachments, cancellationToken);

        await auditLogRepository.AddAsync(new AuditLogEntry
        {
            EntityType = RequestEntityType,
            EntityId = request.Id,
            Action = "RequestCancelled",
            RequestId = request.Id,
            RequestNumberSnapshot = request.RequestNumber,
            PerformedBy = new AuditLogActor
            {
                UserId = actor.UserId,
                FullNameSnapshot = actor.FullName,
                Role = actor.Role.ToString()
            },
            FromState = "Draft",
            ToState = "Cancelled",
            Timestamp = DateTime.UtcNow,
            IpAddress = actor.IpAddress
        }, cancellationToken);

        return null;
    }

    private static readonly RequestStatus[] CancellableNonDraftStatuses =
    [
        RequestStatus.Submitted,
        RequestStatus.ReturnedForCorrection,
        RequestStatus.AwaitingPaymentConfirmation,
        RequestStatus.PaymentConfirmed
    ];

    public async Task<string?> CancelRequestAsync(
        Guid requestId, RequestActor actor, CancellationToken cancellationToken = default)
    {
        var request = await context.Requests.FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken);
        if (request is null || request.UserId != actor.UserId) return RequestNotFoundError;
        if (!CancellableNonDraftStatuses.Contains(request.Status)) return "لا يمكن إلغاء هذا الطلب في حالته الحالية";

        var previousStatus = request.Status;
        var fromState = previousStatus.ToString();
        request.Status = RequestStatus.Cancelled;

        await unitOfWork.RequestHistories.AddAsync(new RequestHistory
        {
            Request = request,
            EventLabel = "إلغاء الطلب",
            EventDate = DateTime.UtcNow
        }, cancellationToken);

        var removedAttachments = previousStatus == RequestStatus.ReturnedForCorrection
            ? await RemoveAttachmentRowsAsync(requestId, cancellationToken)
            : [];

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await DeleteAttachmentObjectsAsync(removedAttachments, cancellationToken);

        await auditLogRepository.AddAsync(new AuditLogEntry
        {
            EntityType = RequestEntityType,
            EntityId = request.Id,
            Action = "RequestCancelled",
            RequestId = request.Id,
            RequestNumberSnapshot = request.RequestNumber,
            PerformedBy = new AuditLogActor
            {
                UserId = actor.UserId,
                FullNameSnapshot = actor.FullName,
                Role = actor.Role.ToString()
            },
            FromState = fromState,
            ToState = "Cancelled",
            Timestamp = DateTime.UtcNow,
            IpAddress = actor.IpAddress
        }, cancellationToken);

        await notificationService.NotifyRequestCancelledByBeneficiaryAsync(request, cancellationToken);
        return null;
    }

    private async Task<List<Attachment>> RemoveAttachmentRowsAsync(Guid requestId, CancellationToken cancellationToken)
    {
        var attachments = await context.Attachments
            .Where(a => a.RequestId == requestId)
            .ToListAsync(cancellationToken);

        if (attachments.Count > 0)
        {
            context.Attachments.RemoveRange(attachments);
        }

        return attachments;
    }

    private async Task DeleteAttachmentObjectsAsync(IReadOnlyList<Attachment> attachments, CancellationToken cancellationToken)
    {
        foreach (var attachment in attachments)
        {
            try
            {
                await attachmentStorage.DeleteAsync(attachment.FilePath, cancellationToken);
            }
            catch (AmazonS3Exception ex)
            {
                logger.LogError(ex,
                    "Failed to delete attachment object {ObjectKey} for cancelled request {RequestId}.",
                    attachment.FilePath, attachment.RequestId);
            }
        }
    }

    public async Task<string?> ResubmitAsync(
        Guid requestId, RequestCreationDto dto, RequestActor actor, CancellationToken cancellationToken = default)
    {
        var error = Validate(dto, RequestStatus.Submitted);
        if (error is not null) return error;

        var request = await context.Requests
            .Include(r => r.Company)
            .Include(r => r.Project)
            .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken);

        if (request is null || request.UserId != actor.UserId) return RequestNotFoundError;
        if (request.Status != RequestStatus.ReturnedForCorrection) return "لا يمكن إعادة إرسال هذا الطلب في حالته الحالية";

        var userType = Enum.TryParse<UserTypeDisplay>(dto.UserType, out var parsedUserType) ? parsedUserType : (UserTypeDisplay?)null;

        var systemExists = await context.TargetSystems.AnyAsync(s => s.Id == dto.SystemId && s.IsActive, cancellationToken);
        if (!systemExists) return "النظام المحدد غير متاح";

        request.ApplicantFirstName = NullIfBlank(dto.FirstName);
        request.ApplicantLastName = NullIfBlank(dto.LastName);
        request.ApplicantEmail = NullIfBlank(dto.Email);
        request.ApplicantMobile = NullIfBlank(dto.Mobile);
        request.UserTypeDisplay = userType;
        request.SystemId = dto.SystemId;
        request.Company.CompanyName = NullIfBlank(dto.CompanyName);
        request.Company.CommercialNo = NullIfBlank(dto.CommercialNo);
        request.Company.CompanyActivity = NullIfBlank(dto.CompanyActivity);
        request.Project.ProjectName = NullIfBlank(dto.ProjectName);
        request.Project.ContractNumber = NullIfBlank(dto.ContractNumber);
        request.Project.ProjectNumber = NullIfBlank(dto.ProjectNumber);

        request.Status = RequestStatus.Submitted;
        request.RequestDate = DateTime.UtcNow;
        request.ReviewDate = null;
        request.ReviewedById = null;

        await unitOfWork.RequestHistories.AddAsync(new RequestHistory
        {
            Request = request,
            EventLabel = "إعادة إرسال الطلب",
            EventDate = DateTime.UtcNow
        }, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await auditLogRepository.AddAsync(new AuditLogEntry
        {
            EntityType = RequestEntityType,
            EntityId = request.Id,
            Action = "RequestResubmitted",
            RequestId = request.Id,
            RequestNumberSnapshot = request.RequestNumber,
            PerformedBy = new AuditLogActor
            {
                UserId = actor.UserId,
                FullNameSnapshot = actor.FullName,
                Role = actor.Role.ToString()
            },
            FromState = "ReturnedForCorrection",
            ToState = "Submitted",
            Timestamp = DateTime.UtcNow,
            IpAddress = actor.IpAddress
        }, cancellationToken);

        await notificationService.NotifyRequestResubmittedAsync(request, cancellationToken);

        return null;
    }

    private async Task<(RequestCreationResultDto? Result, string? Error)> CreateAsync(
        RequestCreationDto dto, RequestActor actor, RequestStatus status, CancellationToken cancellationToken)
    {
        var error = Validate(dto, status);
        if (error is not null) return (null, error);

        var activeRequestLimitError = await CheckActiveRequestLimitAsync(actor.UserId, cancellationToken);
        if (activeRequestLimitError is not null) return (null, activeRequestLimitError);

        var userType = Enum.TryParse<UserTypeDisplay>(dto.UserType, out var parsedUserType) ? parsedUserType : (UserTypeDisplay?)null;

        Guid? systemId = dto.SystemId == Guid.Empty ? null : dto.SystemId;
        if (systemId is not null)
        {
            var systemExists = await context.TargetSystems.AnyAsync(s => s.Id == systemId && s.IsActive, cancellationToken);
            if (!systemExists) return (null, "النظام المحدد غير متاح");
        }

        var company = new Company
        {
            CompanyName = NullIfBlank(dto.CompanyName),
            CommercialNo = NullIfBlank(dto.CommercialNo),
            CompanyActivity = NullIfBlank(dto.CompanyActivity),
            NationalAddress = null,
            Iban = null
        };
        await unitOfWork.Companies.AddAsync(company, cancellationToken);

        var project = new Project
        {
            ProjectName = NullIfBlank(dto.ProjectName),
            ProjectNumber = NullIfBlank(dto.ProjectNumber),
            ContractNumber = NullIfBlank(dto.ContractNumber)
        };
        await unitOfWork.Projects.AddAsync(project, cancellationToken);

        var request = new Request
        {
            UserId = actor.UserId,
            ApplicantFirstName = NullIfBlank(dto.FirstName),
            ApplicantLastName = NullIfBlank(dto.LastName),
            ApplicantEmail = NullIfBlank(dto.Email),
            ApplicantMobile = NullIfBlank(dto.Mobile),
            Company = company,
            Project = project,
            SystemId = systemId,
            UserTypeDisplay = userType,
            Status = status,
            RequestDate = DateTime.UtcNow
        };

        await SaveNewRequestWithUniqueNumberAsync(request, status, cancellationToken);

        await auditLogRepository.AddAsync(new AuditLogEntry
        {
            EntityType = RequestEntityType,
            EntityId = request.Id,
            Action = status == RequestStatus.Draft ? "RequestCreated" : "RequestSubmitted",
            RequestId = request.Id,
            RequestNumberSnapshot = request.RequestNumber,
            PerformedBy = new AuditLogActor
            {
                UserId = actor.UserId,
                FullNameSnapshot = actor.FullName,
                Role = actor.Role.ToString()
            },
            FromState = null,
            ToState = status.ToString(),
            Timestamp = DateTime.UtcNow,
            IpAddress = actor.IpAddress
        }, cancellationToken);

        if (status == RequestStatus.Submitted)
        {
            await notificationService.NotifyRequestSubmittedAsync(request, cancellationToken);
        }

        return (new RequestCreationResultDto { RequestId = request.Id, RequestNumber = request.RequestNumber }, null);
    }

    private async Task SaveNewRequestWithUniqueNumberAsync(
        Request request, RequestStatus status, CancellationToken cancellationToken)
    {
        request.RequestNumber = await GenerateRequestNumberAsync(cancellationToken);
        await unitOfWork.Requests.AddAsync(request, cancellationToken);
        await unitOfWork.RequestHistories.AddAsync(new RequestHistory
        {
            Request = request,
            EventLabel = status == RequestStatus.Draft ? "حفظ الطلب كمسودة" : "إرسال الطلب",
            EventDate = DateTime.UtcNow
        }, cancellationToken);

        for (var attempt = 1; attempt <= MaxRequestNumberInsertAttempts; attempt++)
        {
            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return;
            }
            catch (DbUpdateException ex) when (
                attempt < MaxRequestNumberInsertAttempts && IsRequestNumberCollision(ex))
            {
                request.RequestNumber = await GenerateRequestNumberAsync(cancellationToken);
            }
        }
    }

    private static bool IsRequestNumberCollision(DbUpdateException ex)
        => ex.InnerException is SqlException { Number: 2601 or 2627 } sqlEx
            && sqlEx.Message.Contains("IX_Requests_RequestNumber", StringComparison.Ordinal);

    private async Task<string?> CheckActiveRequestLimitAsync(Guid userId, CancellationToken cancellationToken)
    {
        var maxActiveRequests = (await systemSettingsService.GetAsync(cancellationToken)).MaxActiveRequestsPerUser;

        var activeRequestCount = await context.Requests
            .CountAsync(r => r.UserId == userId && !TerminalStatuses.Contains(r.Status), cancellationToken);

        return activeRequestCount >= maxActiveRequests
            ? "لقد تجاوزت الحد الأقصى المسموح به لعدد الطلبات المفتوحة"
            : null;
    }

    private const int NameMaxLength = 100;
    private const int CompanyNameMaxLength = 200;
    private const int CommercialNoMaxLength = 50;
    private const int CompanyActivityMaxLength = 300;
    private const int ProjectNameMaxLength = 300;
    private const int ProjectOrContractNumberMaxLength = 50;

    private static string? Validate(RequestCreationDto dto, RequestStatus status)
    {
        if (string.IsNullOrWhiteSpace(dto.FirstName) || string.IsNullOrWhiteSpace(dto.LastName)
            || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Mobile))
        {
            return "الاسم الأول والاسم الأخير والبريد الإلكتروني ورقم الجوال مطلوبة لحفظ المسودة";
        }

        if (!EmailFormat.IsValid(dto.Email))
        {
            return "صيغة البريد الإلكتروني غير صحيحة";
        }

        if (!MobileFormat.IsValid(dto.Mobile))
        {
            return "رقم الجوال غير صحيح — يجب أن يبدأ بـ 05 ويتكون من 10 أرقام";
        }

        if (!Enum.TryParse<UserTypeDisplay>(dto.UserType, out _))
        {
            return "نوع المستخدم غير صحيح";
        }

        var freeTextError = ValidateFreeTextFields(dto);
        if (freeTextError is not null)
        {
            return freeTextError;
        }

        if (status != RequestStatus.Submitted)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(dto.CompanyName) || string.IsNullOrWhiteSpace(dto.CommercialNo)
            || string.IsNullOrWhiteSpace(dto.ProjectName) || string.IsNullOrWhiteSpace(dto.ContractNumber))
        {
            return "جميع الحقول الإلزامية مطلوبة";
        }

        if (dto.SystemId == Guid.Empty)
        {
            return "الرجاء اختيار النظام المطلوب تفعيله";
        }

        if (!dto.AgreedToTerms)
        {
            return "يجب الموافقة على الشروط والأحكام قبل إرسال الطلب";
        }

        return null;
    }

    private static string? ValidateFreeTextFields(RequestCreationDto dto)
    {
        if (dto.FirstName.Length > NameMaxLength || dto.LastName.Length > NameMaxLength)
        {
            return $"الاسم الأول والاسم الأخير يجب ألا يتجاوز كل منهما {NameMaxLength} حرفًا";
        }

        (string? Value, int MaxLength, string Label)[] boundedFields =
        [
            (dto.CompanyName, CompanyNameMaxLength, "اسم الشركة"),
            (dto.CommercialNo, CommercialNoMaxLength, "رقم السجل التجاري"),
            (dto.CompanyActivity, CompanyActivityMaxLength, "نشاط الشركة"),
            (dto.ProjectName, ProjectNameMaxLength, "اسم المشروع"),
            (dto.ContractNumber, ProjectOrContractNumberMaxLength, "رقم العقد المرجعي"),
            (dto.ProjectNumber, ProjectOrContractNumberMaxLength, "رقم المشروع")
        ];

        foreach (var (value, maxLength, label) in boundedFields)
        {
            if (string.IsNullOrEmpty(value))
            {
                continue;
            }

            if (value.Length > maxLength)
            {
                return $"{label} يجب ألا يتجاوز {maxLength} حرفًا";
            }

            if (TextInput.HasControlCharacters(value))
            {
                return $"{label} يحتوي على أحرف غير مسموح بها";
            }
        }

        return null;
    }

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private async Task<string> GenerateRequestNumberAsync(CancellationToken cancellationToken)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"REQ-{year}-";
        var sequenceStart = prefix.Length;

        var maxSequence = await context.Requests
            .Where(r => r.RequestNumber.StartsWith(prefix))
            .Select(r => Convert.ToInt32(r.RequestNumber.Substring(sequenceStart)))
            .OrderByDescending(n => n)
            .FirstOrDefaultAsync(cancellationToken);

        return $"{prefix}{maxSequence + 1:D4}";
    }
}
