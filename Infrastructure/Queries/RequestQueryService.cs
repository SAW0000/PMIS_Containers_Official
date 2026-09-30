using Microsoft.EntityFrameworkCore;
using PMIS.Application.DTOs;
using PMIS.Application.Interfaces;
using PMIS.Application.Services;
using PMIS.Domain.Entities;
using PMIS.Domain.Enums;
using PMIS.Infrastructure.Persistence;
using PMIS.Infrastructure.Persistence.Configurations;

namespace PMIS.Infrastructure.Queries;

public class RequestQueryService(PmisDbContext context, IReferenceDataCache cache) : IRequestQueryService
{
    private const string ActiveSystemsCacheKey = "target-systems-active";
    private static readonly TimeSpan ActiveSystemsCacheTtl = TimeSpan.FromMinutes(5);

    public async Task<IReadOnlyList<RequestListItemDto>> GetVisibleRequestsAsync(
        Guid currentUserId, UserRole currentRole, bool includeDetails = true, CancellationToken cancellationToken = default)
    {
        IQueryable<Request> query = context.Requests
            .Include(r => r.User)
            .Include(r => r.Company)
            .Include(r => r.Project)
            .Include(r => r.System)
            .Include(r => r.License)
            .AsSplitQuery()
            .AsNoTrackingWithIdentityResolution()
            .Where(r => r.IsActive);

        if (includeDetails)
        {
            query = query
                .Include(r => r.RequestNotes).ThenInclude(n => n.Reason)
                .Include(r => r.RequestHistories);
        }

        if (currentRole == UserRole.ContractorConsultant)
        {
            query = query.Where(r => r.UserId == currentUserId);
        }

        var requests = await query
            .OrderByDescending(r => r.RequestDate)
            .ToListAsync(cancellationToken);

        return requests.Select(r => ToDto(r, currentUserId)).ToList();
    }

    public async Task<RequestListItemDto?> GetRequestDetailAsync(
        string requestNumber, Guid currentUserId, UserRole currentRole, CancellationToken cancellationToken = default)
    {
        IQueryable<Request> query = context.Requests
            .Include(r => r.User)
            .Include(r => r.Company)
            .Include(r => r.Project)
            .Include(r => r.System)
            .Include(r => r.RequestNotes).ThenInclude(n => n.Reason)
            .Include(r => r.RequestHistories)
            .Include(r => r.License)
            .AsSplitQuery()
            .AsNoTrackingWithIdentityResolution()
            .Where(r => r.IsActive && r.RequestNumber == requestNumber);

        if (currentRole == UserRole.ContractorConsultant)
        {
            query = query.Where(r => r.UserId == currentUserId);
        }

        var request = await query.FirstOrDefaultAsync(cancellationToken);
        return request is null ? null : ToDto(request, currentUserId);
    }

    public async Task<PagedRequestsDto> GetVisibleRequestsPageAsync(
        Guid currentUserId, UserRole currentRole, RequestListQuery query, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        IQueryable<Request> baseQuery = context.Requests
            .Include(r => r.User)
            .Include(r => r.Company)
            .Include(r => r.Project)
            .Include(r => r.System)
            .AsNoTracking()
            .Where(r => r.IsActive);

        if (currentRole == UserRole.ContractorConsultant)
        {
            baseQuery = baseQuery.Where(r => r.UserId == currentUserId);
        }

        baseQuery = ApplyStatusFilter(baseQuery, query.Statuses);
        baseQuery = ApplySearch(baseQuery, query.Search);

        if (query.Period is not (null or "all"))
        {
            var referenceDate = await LatestRequestDateAsync(baseQuery, cancellationToken);
            baseQuery = ApplyPeriodFilter(baseQuery, query.Period, referenceDate);
        }

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var sorted = ApplySort(baseQuery, query.SortCol, query.SortDir);

        var pageOfRequests = await sorted
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedRequestsDto
        {
            Items = pageOfRequests.Select(r => ToDto(r, currentUserId)).ToList(),
            TotalCount = totalCount
        };
    }

    internal static async Task<DateTime?> LatestRequestDateAsync(IQueryable<Request> scopedQuery, CancellationToken cancellationToken)
        => await scopedQuery.OrderByDescending(r => r.RequestDate).Select(r => (DateTime?)r.RequestDate).FirstOrDefaultAsync(cancellationToken);

    private static IQueryable<Request> ApplyStatusFilter(IQueryable<Request> query, List<string> mockStatusKeys)
    {
        if (mockStatusKeys is not { Count: > 0 })
        {
            return query.Where(_ => false);
        }

        var statuses = mockStatusKeys
            .Select(TryParseMockStatusKey)
            .Where(s => s is not null)
            .Select(s => s!.Value)
            .ToList();

        return query.Where(r => statuses.Contains(r.Status));
    }

    internal static IQueryable<Request> ApplyPeriodFilter(IQueryable<Request> query, string? period, DateTime? referenceDate)
    {
        if (period is null || period == "all" || referenceDate is null)
        {
            return query;
        }

        var ref_ = referenceDate.Value;
        switch (period)
        {
            case "today":
                return query.Where(r => r.RequestDate.Date == ref_.Date);
            case "week":
            {
                var daysSinceMonday = ((int)ref_.DayOfWeek + 6) % 7;
                var start = ref_.Date.AddDays(-daysSinceMonday);
                var end = start.AddDays(7);
                return query.Where(r => r.RequestDate >= start && r.RequestDate < end);
            }
            case "month":
                return query.Where(r => r.RequestDate.Year == ref_.Year && r.RequestDate.Month == ref_.Month);
            case "year":
                return query.Where(r => r.RequestDate.Year == ref_.Year);
            default:
                return query;
        }
    }

    private static IQueryable<Request> ApplySearch(IQueryable<Request> query, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return query;
        }

        var term = search.Trim();
        return query.Where(r =>
            EF.Functions.Like(r.RequestNumber, $"%{term}%") ||
            EF.Functions.Like(r.Project.ProjectName ?? string.Empty, $"%{term}%") ||
            EF.Functions.Like(r.User.FullName, $"%{term}%") ||
            EF.Functions.Like(r.Company.CompanyName ?? string.Empty, $"%{term}%"));
    }

    private static int StatusSortRank(RequestStatus status) => status switch
    {
        RequestStatus.Draft => 0,
        RequestStatus.Submitted => 1,
        RequestStatus.ReturnedForCorrection => 2,
        RequestStatus.Rejected => 3,
        RequestStatus.AwaitingPaymentConfirmation => 4,
        RequestStatus.PaymentConfirmed => 5,
        RequestStatus.Activated => 6,
        RequestStatus.Cancelled => 7,
        _ => 8
    };

    private static IQueryable<Request> ApplySort(IQueryable<Request> query, string? sortCol, string? sortDir)
    {
        var descending = sortDir == "desc";

        return sortCol switch
        {
            "id" => OrderBy(query, r => r.RequestNumber, descending),
            "applicant" => OrderBy(query, r => r.User.FullName, descending),
            "company" => OrderBy(query, r => r.Company.CompanyName, descending),
            "project" => OrderBy(query, r => r.Project.ProjectName, descending),
            "status" => OrderBy(query, r => StatusSortRank(r.Status), descending),
            _ => OrderBy(query, r => r.RequestDate, descending)
        };
    }

    private static IQueryable<Request> OrderBy<TKey>(IQueryable<Request> query, System.Linq.Expressions.Expression<Func<Request, TKey>> keySelector, bool descending)
        => descending ? query.OrderByDescending(keySelector) : query.OrderBy(keySelector);

    private static RequestStatus? TryParseMockStatusKey(string mockStatusKey) => mockStatusKey switch
    {
        "draft" => RequestStatus.Draft,
        "submitted" => RequestStatus.Submitted,
        "returned" => RequestStatus.ReturnedForCorrection,
        "rejected" => RequestStatus.Rejected,
        "awaiting_payment" => RequestStatus.AwaitingPaymentConfirmation,
        "approved" => RequestStatus.PaymentConfirmed,
        "activated" => RequestStatus.Activated,
        "cancelled" => RequestStatus.Cancelled,
        _ => null
    };

    public async Task<IReadOnlyList<TargetSystemDto>> GetActiveSystemsAsync(CancellationToken cancellationToken = default)
    {
        var cached = await cache.GetAsync<IReadOnlyList<TargetSystemDto>>(ActiveSystemsCacheKey);
        if (cached is not null)
        {
            return cached;
        }

        var systems = await context.TargetSystems
            .AsNoTracking()
            .Where(s => s.IsActive && !s.IsDelete)
            .OrderBy(s => s.SystemName)
            .Select(s => new TargetSystemDto { Id = s.Id, SystemName = s.SystemName })
            .ToListAsync(cancellationToken);

        await cache.SetAsync(ActiveSystemsCacheKey, systems, ActiveSystemsCacheTtl);
        return systems;
    }

    public Task<int> GetPaymentConfirmationNeededCountAsync(CancellationToken cancellationToken = default)
        => context.Requests
            .Where(r => r.IsActive && r.Status == RequestStatus.AwaitingPaymentConfirmation)
            .CountAsync(cancellationToken);

    public async Task<int> GetOverdueRequestCountAsync(CancellationToken cancellationToken = default)
    {
        var slaBusinessDays = await context.SystemSettings
            .Where(s => s.Id == SystemSettingsConfiguration.SingletonId)
            .Select(s => s.ResponseSlaBusinessDays)
            .FirstOrDefaultAsync(cancellationToken);

        if (slaBusinessDays < 1)
        {
            return 0;
        }

        var cutoff = BusinessDays.CutoffFor(DateTime.UtcNow, slaBusinessDays);

        return await context.Requests
            .Where(r => r.IsActive && r.Status == RequestStatus.Submitted && r.RequestDate < cutoff)
            .CountAsync(cancellationToken);
    }

    public Task<int> GetSubmittedCountAsync(Guid currentUserId, UserRole currentRole, CancellationToken cancellationToken = default)
    {
        IQueryable<Request> query = context.Requests.Where(r => r.IsActive && r.Status == RequestStatus.Submitted);

        if (currentRole == UserRole.ContractorConsultant)
        {
            query = query.Where(r => r.UserId == currentUserId);
        }

        return query.CountAsync(cancellationToken);
    }

    private static RequestListItemDto ToDto(Request request, Guid currentUserId)
    {
        var latestNote = request.RequestNotes
            .OrderByDescending(n => n.CreatedDate)
            .FirstOrDefault();

        return new RequestListItemDto
        {
            Id = request.RequestNumber,
            RequestId = request.Id,
            Applicant = request.User.FullName,
            ApplicantEmail = request.User.Email ?? string.Empty,
            UserType = UserTypeDisplayLabel(request.UserTypeDisplay),
            Company = request.Company.CompanyName ?? string.Empty,
            Project = request.Project.ProjectName ?? string.Empty,
            ContractNumber = request.Project.ContractNumber ?? string.Empty,
            TargetSystem = request.System?.SystemName ?? string.Empty,
            Date = request.RequestDate.ToString("yyyy-MM-dd"),
            Status = MockStatusKey(request.Status),
            Reason = latestNote?.Reason?.Label,
            IsOwnRequest = request.UserId == currentUserId,
            Notes = request.RequestNotes
                .OrderBy(n => n.CreatedDate)
                .Select(n => new RequestNoteDto
                {
                    Author = "موظف مكتب P3O",
                    Date = n.CreatedDate.ToString("yyyy-MM-dd"),
                    Text = n.NoteText
                })
                .ToList(),
            History = BuildHistory(request)
        };
    }

    private static List<List<string?>> BuildHistory(Request request)
        => request.RequestHistories
            .OrderBy(h => h.EventDate)
            .Select(h => new List<string?> { h.EventLabel, h.EventDate.ToString("yyyy-MM-dd") })
            .ToList();

    private static string MockStatusKey(RequestStatus status) => status switch
    {
        RequestStatus.Draft => "draft",
        RequestStatus.Submitted => "submitted",
        RequestStatus.ReturnedForCorrection => "returned",
        RequestStatus.Rejected => "rejected",
        RequestStatus.AwaitingPaymentConfirmation => "awaiting_payment",
        RequestStatus.PaymentConfirmed => "approved",
        RequestStatus.Activated => "activated",
        RequestStatus.Cancelled => "cancelled",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unmapped request status")
    };

    private static string UserTypeDisplayLabel(UserTypeDisplay? userType) => userType switch
    {
        UserTypeDisplay.Contractor => "مقاول",
        UserTypeDisplay.Consultant => "استشاري",
        null => string.Empty,
        _ => throw new ArgumentOutOfRangeException(nameof(userType), userType, "Unmapped user type display")
    };
}
