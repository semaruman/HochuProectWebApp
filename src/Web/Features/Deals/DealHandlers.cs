using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Web.Common.Errors;
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
    public async Task<DealActionResult> HandleAsync(Guid dealId, Guid buyerId, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var deal = await db.Deals.FirstOrDefaultAsync(d => d.Id == dealId, ct)
            ?? throw AppErrors.NotFound();
        if (deal.BuyerId != buyerId)
            throw AppErrors.Forbidden();
        if (deal.IsFunded)
            return new DealActionResult(deal.Id, deal.Status, deal.FundedAt, deal.CompletedAt, Idempotent: true);

        var utcNow = DateTime.UtcNow;
        var paymentResult = await payments.CreateAndAuthorizeAsync(deal.Id, deal.Amount, ct);
        if (!paymentResult.Success)
            throw AppErrors.Business(paymentResult.Error ?? "Payment failed.");

        deal.Fund(utcNow);
        db.Payments.Add(Payment.Authorize(
            deal.Id,
            deal.Amount,
            paymentOptions.Value.Provider,
            paymentResult.ProviderPaymentId,
            utcNow));

        await db.SaveAndDispatchAsync(dispatcher, ct);
        await tx.CommitAsync(ct);
        return new DealActionResult(deal.Id, deal.Status, deal.FundedAt, deal.CompletedAt);
    }
}

public sealed class SubmitWorkHandler(AppDbContext db, IFileStorage files, IDomainEventDispatcher dispatcher)
{
    public async Task<SubmitWorkResult> HandleAsync(
        Guid dealId,
        Guid sellerId,
        string? message,
        IReadOnlyList<DeliverableUpload>? uploads,
        CancellationToken ct)
    {
        var deal = await db.Deals.FirstOrDefaultAsync(d => d.Id == dealId, ct)
            ?? throw AppErrors.NotFound();
        if (deal.SellerId != sellerId)
            throw AppErrors.Forbidden();

        var utcNow = DateTime.UtcNow;
        var deliverable = deal.SubmitWork(message, utcNow);
        db.DealDeliverables.Add(deliverable);

        if (uploads is not null)
        {
            foreach (var upload in uploads.Take(5))
            {
                var stored = await files.SaveAsync(
                    upload.Content, upload.FileName, upload.ContentType, $"deals/{deal.Id}/deliverables", ct);
                deliverable.AddFile(stored.StorageKey, stored.FileName, stored.ContentType, stored.SizeBytes);
            }
        }

        await db.SaveAndDispatchAsync(dispatcher, ct);
        return new SubmitWorkResult(deal.Id, deal.Status, deliverable.Id);
    }
}

public sealed class AcceptWorkHandler(AppDbContext db, IPaymentService payments, IDomainEventDispatcher dispatcher)
{
    public async Task<DealActionResult> HandleAsync(Guid dealId, Guid buyerId, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var deal = await db.Deals.Include(d => d.Payment).FirstOrDefaultAsync(d => d.Id == dealId, ct)
            ?? throw AppErrors.NotFound();
        if (deal.BuyerId != buyerId)
            throw AppErrors.Forbidden();
        if (deal.IsCompleted)
            return new DealActionResult(deal.Id, deal.Status, deal.FundedAt, deal.CompletedAt, Idempotent: true);

        var utcNow = DateTime.UtcNow;
        deal.Accept(utcNow);
        var project = await db.Projects.FirstAsync(p => p.Id == deal.ProjectId, ct);
        project.MarkCompleted(utcNow);

        if (deal.Payment is not null)
        {
            var capture = await payments.CaptureAsync(deal.Payment.ProviderPaymentId, ct);
            if (!capture.Success)
                throw AppErrors.Business(capture.Error ?? "Capture failed.");
            deal.Payment.MarkCaptured(utcNow);
        }

        await db.SaveAndDispatchAsync(dispatcher, ct);
        await tx.CommitAsync(ct);
        return new DealActionResult(deal.Id, deal.Status, deal.FundedAt, deal.CompletedAt);
    }
}

public sealed class CancelDealHandler(AppDbContext db, IPaymentService payments, IDomainEventDispatcher dispatcher)
{
    public async Task<DealActionResult> HandleAsync(Guid dealId, Guid actorId, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var deal = await db.Deals.Include(d => d.Payment).FirstOrDefaultAsync(d => d.Id == dealId, ct)
            ?? throw AppErrors.NotFound();
        if (!deal.IsParticipant(actorId))
            throw AppErrors.Forbidden();

        var utcNow = DateTime.UtcNow;
        deal.Cancel(actorId, utcNow);

        if (deal.Payment is { Status: PaymentStatus.Authorized })
        {
            var refund = await payments.RefundAsync(deal.Payment.ProviderPaymentId, ct);
            if (refund.Success)
                deal.Payment.MarkRefunded(utcNow);
        }

        var project = await db.Projects.FirstAsync(p => p.Id == deal.ProjectId, ct);
        if (project.Status == ProjectStatus.InProgress)
            project.Cancel(utcNow);

        await db.SaveAndDispatchAsync(dispatcher, ct);
        await tx.CommitAsync(ct);
        return new DealActionResult(deal.Id, deal.Status, deal.FundedAt, deal.CompletedAt);
    }
}
