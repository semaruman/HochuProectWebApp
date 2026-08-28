using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Web.Common.Results;
using Web.Domain.Entities;
using Web.Domain.Enums;
using Web.Infrastructure.DomainEvents;
using Web.Infrastructure.Files;
using Web.Infrastructure.Payments;
using Web.Infrastructure.Persistence;

namespace Web.Features.Deals;

public sealed record DealActionResult(Guid Id, DealStatus Status, DateTime? FundedAt, DateTime? CompletedAt, bool Idempotent = false);

public sealed record SubmitWorkResult(Guid Id, DealStatus Status, Guid DeliverableId);

public sealed record DeliverableUpload(Stream Content, string FileName, string ContentType);

public sealed class FundDealHandler(
    AppDbContext db,
    IPaymentService payments,
    IOptions<PaymentOptions> paymentOptions,
    IDomainEventDispatcher dispatcher)
{
    public async Task<Result<DealActionResult>> HandleAsync(Guid dealId, Guid buyerId, CancellationToken ct)
    {
        var deal = await db.Deals.FirstOrDefaultAsync(d => d.Id == dealId, ct);
        if (deal is null)
            return ResultErrors.NotFound();
        if (deal.BuyerId != buyerId)
            return ResultErrors.Forbidden();
        if (deal.IsWorkStarted)
            return new DealActionResult(deal.Id, deal.Status, deal.FundedAt, deal.CompletedAt, Idempotent: true);

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var utcNow = DateTime.UtcNow;
        var paymentResult = await payments.CreateAndAuthorizeAsync(deal.Id, deal.Amount, ct);
        if (!paymentResult.Success)
            return ResultErrors.Business(paymentResult.Error ?? "Payment failed.");

        var fund = deal.Fund(utcNow);
        if (fund.IsFailure) return fund.Error;

        var payment = Payment.Authorize(
            deal.Id,
            deal.Amount,
            paymentOptions.Value.Provider,
            paymentResult.ProviderPaymentId,
            utcNow);
        if (payment.IsFailure) return payment.Error;
        db.Payments.Add(payment.Value);

        await db.SaveAndDispatchAsync(dispatcher, ct);
        await tx.CommitAsync(ct);
        return new DealActionResult(deal.Id, deal.Status, deal.FundedAt, deal.CompletedAt);
    }
}

public sealed class SubmitWorkHandler(AppDbContext db, IFileStorage files, IDomainEventDispatcher dispatcher)
{
    public async Task<Result<SubmitWorkResult>> HandleAsync(
        Guid dealId,
        Guid sellerId,
        string? message,
        IReadOnlyList<DeliverableUpload>? uploads,
        CancellationToken ct)
    {
        var deal = await db.Deals.FirstOrDefaultAsync(d => d.Id == dealId, ct);
        if (deal is null)
            return ResultErrors.NotFound();
        if (deal.SellerId != sellerId)
            return ResultErrors.Forbidden();

        var utcNow = DateTime.UtcNow;
        var submit = deal.SubmitWork(message, utcNow);
        if (submit.IsFailure) return submit.Error;

        var deliverable = submit.Value;
        db.DealDeliverables.Add(deliverable);

        if (uploads is not null)
        {
            foreach (var upload in uploads.Take(5))
            {
                var stored = await files.SaveAsync(
                    upload.Content, upload.FileName, upload.ContentType, $"deals/{deal.Id}/deliverables", ct);
                if (stored.IsFailure) return stored.Error;
                deliverable.AddFile(stored.Value.StorageKey, stored.Value.FileName, stored.Value.ContentType, stored.Value.SizeBytes);
            }
        }

        await db.SaveAndDispatchAsync(dispatcher, ct);
        return new SubmitWorkResult(deal.Id, deal.Status, deliverable.Id);
    }
}

public sealed class AcceptWorkHandler(AppDbContext db, IPaymentService payments, IDomainEventDispatcher dispatcher)
{
    public async Task<Result<DealActionResult>> HandleAsync(Guid dealId, Guid buyerId, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var deal = await db.Deals.Include(d => d.Payment).FirstOrDefaultAsync(d => d.Id == dealId, ct);
        if (deal is null)
            return ResultErrors.NotFound();
        if (deal.BuyerId != buyerId)
            return ResultErrors.Forbidden();
        if (deal.IsCompleted)
            return new DealActionResult(deal.Id, deal.Status, deal.FundedAt, deal.CompletedAt, Idempotent: true);

        var utcNow = DateTime.UtcNow;
        var accept = deal.Accept(utcNow);
        if (accept.IsFailure) return accept.Error;

        var project = await db.Projects.FirstAsync(p => p.Id == deal.ProjectId, ct);
        var complete = project.MarkCompleted(utcNow);
        if (complete.IsFailure) return complete.Error;

        if (deal.Payment is not null)
        {
            var capture = await payments.CaptureAsync(deal.Payment.ProviderPaymentId, ct);
            if (!capture.Success)
                return ResultErrors.Business(capture.Error ?? "Capture failed.");
            var captured = deal.Payment.MarkCaptured(utcNow);
            if (captured.IsFailure) return captured.Error;
        }

        await db.SaveAndDispatchAsync(dispatcher, ct);
        await tx.CommitAsync(ct);
        return new DealActionResult(deal.Id, deal.Status, deal.FundedAt, deal.CompletedAt);
    }
}

public sealed class CancelDealHandler(AppDbContext db, IPaymentService payments, IDomainEventDispatcher dispatcher)
{
    public async Task<Result<DealActionResult>> HandleAsync(Guid dealId, Guid actorId, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var deal = await db.Deals.Include(d => d.Payment).FirstOrDefaultAsync(d => d.Id == dealId, ct);
        if (deal is null)
            return ResultErrors.NotFound();
        if (!deal.IsParticipant(actorId))
            return ResultErrors.Forbidden();

        var utcNow = DateTime.UtcNow;
        var cancel = deal.Cancel(actorId, utcNow);
        if (cancel.IsFailure) return cancel.Error;

        if (deal.Payment is { Status: PaymentStatus.Authorized })
        {
            var refund = await payments.RefundAsync(deal.Payment.ProviderPaymentId, ct);
            if (refund.Success)
            {
                var refunded = deal.Payment.MarkRefunded(utcNow);
                if (refunded.IsFailure) return refunded.Error;
            }
        }

        var project = await db.Projects.FirstAsync(p => p.Id == deal.ProjectId, ct);
        if (project.Status == ProjectStatus.InProgress)
        {
            var projectCancel = project.Cancel(utcNow);
            if (projectCancel.IsFailure) return projectCancel.Error;
        }

        await db.SaveAndDispatchAsync(dispatcher, ct);
        await tx.CommitAsync(ct);
        return new DealActionResult(deal.Id, deal.Status, deal.FundedAt, deal.CompletedAt);
    }
}

public sealed record RequestRevisionResult(Guid Id, DealStatus Status, string Comment);

public sealed class RequestRevisionHandler(AppDbContext db, IDomainEventDispatcher dispatcher)
{
    public async Task<Result<RequestRevisionResult>> HandleAsync(Guid dealId, Guid buyerId, string comment, CancellationToken ct)
    {
        var deal = await db.Deals.FirstOrDefaultAsync(d => d.Id == dealId, ct);
        if (deal is null)
            return ResultErrors.NotFound();
        if (deal.BuyerId != buyerId)
            return ResultErrors.Forbidden();

        var revision = deal.RequestRevision(comment, DateTime.UtcNow);
        if (revision.IsFailure) return revision.Error;

        await db.SaveAndDispatchAsync(dispatcher, ct);
        return new RequestRevisionResult(deal.Id, deal.Status, deal.LastRevisionComment ?? comment);
    }
}
