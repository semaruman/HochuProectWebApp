namespace Web.Domain.Events;

public sealed record BidPlaced(
    Guid BidId,
    Guid ProjectId,
    Guid BuyerId,
    Guid SellerId,
    string ProjectTitle,
    DateTime OccurredOn) : IDomainEvent;

public sealed record BidAccepted(
    Guid ProjectId,
    Guid BidId,
    Guid DealId,
    Guid BuyerId,
    Guid SellerId,
    string ProjectTitle,
    DateTime OccurredOn) : IDomainEvent;

public sealed record DealFunded(
    Guid DealId,
    Guid BuyerId,
    Guid SellerId,
    decimal Amount,
    DateTime OccurredOn) : IDomainEvent;

public sealed record WorkSubmitted(
    Guid DealId,
    Guid BuyerId,
    Guid SellerId,
    Guid DeliverableId,
    DateTime OccurredOn) : IDomainEvent;

public sealed record WorkRevisionRequested(
    Guid DealId,
    Guid BuyerId,
    Guid SellerId,
    string Comment,
    DateTime OccurredOn) : IDomainEvent;

public sealed record DealCompleted(
    Guid DealId,
    Guid BuyerId,
    Guid SellerId,
    Guid? PaymentId,
    DateTime OccurredOn) : IDomainEvent;

public sealed record DealCancelled(
    Guid DealId,
    Guid ActorId,
    Guid BuyerId,
    Guid SellerId,
    DateTime OccurredOn) : IDomainEvent;
