using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Web.Domain.Events;
using Web.Infrastructure.Audit;
using Web.Infrastructure.Email;
using Web.Infrastructure.Notifications;
using Web.Infrastructure.Persistence;

namespace Web.Infrastructure.DomainEvents;

public sealed class MarketplaceEventHandler(
    INotificationService notifications,
    IAuditService audit,
    IEmailService email,
    AppDbContext db,
    IOptions<AppOptions> appOptions,
    ILogger<MarketplaceEventHandler> logger)
    : IDomainEventHandler<BidPlaced>,
      IDomainEventHandler<BidAccepted>,
      IDomainEventHandler<DealFunded>,
      IDomainEventHandler<WorkSubmitted>,
      IDomainEventHandler<WorkRevisionRequested>,
      IDomainEventHandler<DealCompleted>,
      IDomainEventHandler<DealCancelled>
{
    private string BaseUrl => appOptions.Value.PublicBaseUrl.TrimEnd('/');

    public Task HandleAsync(BidPlaced domainEvent, CancellationToken cancellationToken = default)
        => SafeNotifyAndEmailAsync(
            domainEvent.BuyerId,
            "NewBid",
            "Новый отклик",
            $"На проект «{domainEvent.ProjectTitle}» пришёл новый отклик.",
            $"/project.html?id={domainEvent.ProjectId}",
            $"Новый отклик на проект «{domainEvent.ProjectTitle}»",
            cancellationToken);

    public async Task HandleAsync(BidAccepted domainEvent, CancellationToken cancellationToken = default)
    {
        var dealUrl = $"/deal.html?id={domainEvent.DealId}";
        await audit.WriteAsync(domainEvent.BuyerId, "BidAccepted", "Bid", domainEvent.BidId,
            new { domainEvent.DealId, domainEvent.ProjectId }, cancellationToken);
        await audit.WriteAsync(domainEvent.BuyerId, "DealCreated", "Deal", domainEvent.DealId,
            new { domainEvent.BidId, domainEvent.ProjectId }, cancellationToken);
        await SafeNotifyAndEmailAsync(
            domainEvent.SellerId,
            "BidAccepted",
            "Отклик принят",
            $"Ваш отклик на «{domainEvent.ProjectTitle}» принят. Работа начата.",
            dealUrl,
            "Ваш отклик принят — работа начата",
            cancellationToken);
    }

    public async Task HandleAsync(DealFunded domainEvent, CancellationToken cancellationToken = default)
    {
        var dealUrl = $"/Deals/Details/{domainEvent.DealId}";
        await audit.WriteAsync(domainEvent.BuyerId, "DealFunded", "Deal", domainEvent.DealId,
            new { domainEvent.Amount }, cancellationToken);
        await SafeNotifyAndEmailAsync(domainEvent.SellerId, "DealFunded", "Сделка начата",
            "Заказчик подтвердил старт работы.", dealUrl, "Сделка начата", cancellationToken);
    }

    public async Task HandleAsync(WorkSubmitted domainEvent, CancellationToken cancellationToken = default)
    {
        var dealUrl = $"/deal.html?id={domainEvent.DealId}";
        await audit.WriteAsync(domainEvent.SellerId, "WorkSubmitted", "Deal", domainEvent.DealId, null, cancellationToken);
        await SafeNotifyAndEmailAsync(domainEvent.BuyerId, "WorkSubmitted", "Работа сдана",
            "Исполнитель отправил результат на проверку.", dealUrl,
            "Исполнитель сдал работу", cancellationToken);
    }

    public async Task HandleAsync(WorkRevisionRequested domainEvent, CancellationToken cancellationToken = default)
    {
        var dealUrl = $"/deal.html?id={domainEvent.DealId}";
        await SafeNotifyAndEmailAsync(domainEvent.SellerId, "RevisionRequired", "Нужна доработка",
            $"Заказчик вернул работу на доработку: {domainEvent.Comment}", dealUrl,
            "Работа возвращена на доработку", cancellationToken);
    }

    public async Task HandleAsync(DealCompleted domainEvent, CancellationToken cancellationToken = default)
    {
        var dealUrl = $"/deal.html?id={domainEvent.DealId}";
        await audit.WriteAsync(domainEvent.BuyerId, "DealCompleted", "Deal", domainEvent.DealId, null, cancellationToken);
        if (domainEvent.PaymentId is { } paymentId)
            await audit.WriteAsync(domainEvent.BuyerId, "PaymentReleased", "Payment", paymentId, null, cancellationToken);
        await SafeNotifyAndEmailAsync(domainEvent.SellerId, "WorkAccepted", "Работа принята",
            "Заказчик принял результат. Сделка завершена.", dealUrl, "Работа принята", cancellationToken);
        await SafeNotifyAndEmailAsync(domainEvent.BuyerId, "DealCompleted", "Сделка завершена",
            "Вы можете оставить отзыв исполнителю.", dealUrl, "Сделка завершена", cancellationToken);
    }

    public async Task HandleAsync(DealCancelled domainEvent, CancellationToken cancellationToken = default)
    {
        var dealUrl = $"/deal.html?id={domainEvent.DealId}";
        await audit.WriteAsync(domainEvent.ActorId, "DealCancelled", "Deal", domainEvent.DealId, null, cancellationToken);
        var other = domainEvent.ActorId == domainEvent.BuyerId ? domainEvent.SellerId : domainEvent.BuyerId;
        await notifications.NotifyAsync(other, "DealCancelled", "Сделка отменена",
            "Сделка была отменена.", dealUrl, cancellationToken);
    }

    private async Task SafeNotifyAndEmailAsync(
        Guid userId,
        string type,
        string title,
        string body,
        string linkUrl,
        string emailSubject,
        CancellationToken ct)
    {
        try
        {
            await notifications.NotifyAsync(userId, type, title, body, linkUrl, ct);
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to persist in-app notification for user {UserId}", userId);
        }

        try
        {
            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
            if (user?.Email is { Length: > 0 } emailAddress)
            {
                var link = linkUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                    ? linkUrl
                    : $"{BaseUrl}{linkUrl}";
                await email.SendAsync(
                    emailAddress,
                    emailSubject,
                    $"<p>{body}</p><p><a href=\"{link}\">Открыть в приложении</a></p>",
                    ct);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email to user {UserId}", userId);
        }
    }
}
