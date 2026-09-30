using Microsoft.AspNetCore.Mvc;
using PMIS.Application.Interfaces;

namespace PMIS.Controllers;

[Route("app/requests")]
public class RequestReviewController(IRequestActionService requestActionService) : PmisAuthorizedControllerBase
{
    [HttpPost("{requestNumber}/approve")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Approve(string requestNumber, CancellationToken cancellationToken)
        => RunReviewActionAsync(actor => requestActionService.ApproveAsync(requestNumber, actor, cancellationToken));

    [HttpPost("{requestNumber}/return")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Return(string requestNumber, [FromBody] RequestNoteBody body, CancellationToken cancellationToken)
        => RunReviewActionAsync(actor => requestActionService.ReturnAsync(requestNumber, actor, body.Note, body.ReasonId, cancellationToken));

    [HttpPost("{requestNumber}/reject")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Reject(string requestNumber, [FromBody] RequestNoteBody body, CancellationToken cancellationToken)
        => RunReviewActionAsync(actor => requestActionService.RejectAsync(requestNumber, actor, body.Note, body.ReasonId, cancellationToken));

    [HttpPost("{requestNumber}/confirm-payment")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> ConfirmPayment(string requestNumber, CancellationToken cancellationToken)
        => RunReviewActionAsync(actor => requestActionService.ConfirmPaymentAsync(requestNumber, actor, cancellationToken));

    [HttpPost("{requestNumber}/activate")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Activate(string requestNumber, CancellationToken cancellationToken)
        => RunReviewActionAsync(actor => requestActionService.ActivateAsync(requestNumber, actor, cancellationToken));
}

public class RequestNoteBody
{
    public string Note { get; set; } = string.Empty;

    public required Guid ReasonId { get; set; }
}
