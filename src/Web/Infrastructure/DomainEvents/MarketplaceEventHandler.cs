using Web.Domain.Events;
using Web.Infrastructure.Audit;
using Web.Infrastructure.Notifications;

namespace Web.Infrastructure.DomainEvents;

public sealed class MarketplaceEventHandler(INotificationService notifications, IAuditService audit)
    : IDomainEventHandler<BidPlaced>,
      IDomainEventHandler<BidAccepted>,
      IDomainEventHandler<DealFunded>,
      IDomainEventHandler<WorkSubmitted>,
      IDomainEventHandler<DealCompleted>,
      IDomainEventHandler<DealCancelled>
{
    public Task HandleAsync(BidPlaced domainEvent, CancellationToken cancellationToken = default)
        => notifications.NotifyAsync(
            domainEvent.BuyerId,
            "NewBid",
            "Новый отклик",
            $"На проект «{domainEvent.ProjectTitle}» пришёл новый отклик.",
            $"/Projects/Bids/{domainEvent.ProjectId}",
            cancellationToken);

    public async Task HandleAsync(BidAccepted domainEvent, CancellationToken cancellationToken = default)
    {
        var dealUrl = $"/Deals/Details/{domainEvent.DealId}";
        await audit.WriteAsync(domainEvent.BuyerId, "BidAccepted", "Bid", domainEvent.BidId,
            new { domainEvent.DealId, domainEvent.ProjectId }, cancellationToken);
        await audit.WriteAsync(domainEvent.BuyerId, "DealCreated", "Deal", domainEvent.DealId,
            new { domainEvent.BidId, domainEvent.ProjectId }, cancellationToken);
        await notifications.NotifyAsync(domainEvent.SellerId, "BidAccepted", "Отклик принят",
            $"Ваш отклик на «{domainEvent.ProjectTitle}» принят.", dealUrl, cancellationToken);
    }

    public async Task HandleAsync(DealFunded domainEvent, CancellationToken cancellationToken = default)
    {
        var dealUrl = $"/Deals/Details/{domainEvent.DealId}";
        await audit.WriteAsync(domainEvent.BuyerId, "DealFunded", "Deal", domainEvent.DealId,
            new { domainEvent.Amount }, cancellationToken);
        await notifications.NotifyAsync(domainEvent.SellerId, "DealFunded", "Сделка оплачена",
            "Заказчик зарезервировал оплату. Можно приступать к работе.", dealUrl, cancellationToken);
    }

    public async Task HandleAsync(WorkSubmitted domainEvent, CancellationToken cancellationToken = default)
    {
        var dealUrl = $"/Deals/Details/{domainEvent.DealId}";
        await audit.WriteAsync(domainEvent.SellerId, "WorkSubmitted", "Deal", domainEvent.DealId, null, cancellationToken);
        await notifications.NotifyAsync(domainEvent.BuyerId, "WorkSubmitted", "Работа сдана",
            "Исполнитель отправил результат на проверку.", dealUrl, cancellationToken);
    }

    public async Task HandleAsync(DealCompleted domainEvent, CancellationToken cancellationToken = default)
    {
        var dealUrl = $"/Deals/Details/{domainEvent.DealId}";
        await audit.WriteAsync(domainEvent.BuyerId, "DealCompleted", "Deal", domainEvent.DealId, null, cancellationToken);
        if (domainEvent.PaymentId is { } paymentId)
            await audit.WriteAsync(domainEvent.BuyerId, "PaymentReleased", "Payment", paymentId, null, cancellationToken);
        await notifications.NotifyAsync(domainEvent.SellerId, "WorkAccepted", "Работа принята",
            "Заказчик принял результат. Сделка завершена.", dealUrl, cancellationToken);
        await notifications.NotifyAsync(domainEvent.BuyerId, "DealCompleted", "Сделка завершена",
            "Вы можете оставить отзыв исполнителю.", dealUrl, cancellationToken);
    }

    public async Task HandleAsync(DealCancelled domainEvent, CancellationToken cancellationToken = default)
    {
        var dealUrl = $"/Deals/Details/{domainEvent.DealId}";
        await audit.WriteAsync(domainEvent.ActorId, "DealCancelled", "Deal", domainEvent.DealId, null, cancellationToken);
        var other = domainEvent.ActorId == domainEvent.BuyerId ? domainEvent.SellerId : domainEvent.BuyerId;
        await notifications.NotifyAsync(other, "DealCancelled", "Сделка отменена",
            "Сделка была отменена.", dealUrl, cancellationToken);
    }
}
