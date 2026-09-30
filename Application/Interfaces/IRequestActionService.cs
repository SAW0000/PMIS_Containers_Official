namespace PMIS.Application.Interfaces;

public interface IRequestActionService
{
    Task<string?> ApproveAsync(string requestNumber, RequestActor actor, CancellationToken cancellationToken = default);

    Task<string?> ReturnAsync(string requestNumber, RequestActor actor, string note, Guid reasonId, CancellationToken cancellationToken = default);

    Task<string?> RejectAsync(string requestNumber, RequestActor actor, string note, Guid reasonId, CancellationToken cancellationToken = default);

    Task<string?> ConfirmPaymentAsync(string requestNumber, RequestActor actor, CancellationToken cancellationToken = default);

    Task<string?> ActivateAsync(string requestNumber, RequestActor actor, CancellationToken cancellationToken = default);
}
