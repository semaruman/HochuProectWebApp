using FluentAssertions;
using Web.Domain.Entities;
using Web.Domain.Enums;
using Web.Domain.Events;
using Web.Domain.Exceptions;
using Web.Domain.ValueObjects;
using Web.Features.Bids;
using Web.Features.Projects;

namespace Web.UnitTests;

public class MoneyTests
{
    [Fact]
    public void Rub_RejectsNonPositiveAmount()
    {
        var act = () => Money.Rub(0);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Constructor_NormalizesCurrency()
    {
        var money = new Money(10.555m, "rub");
        money.Currency.Should().Be("RUB");
        money.Amount.Should().Be(10.56m);
    }
}

public class DomainStateMachineTests
{
    private static readonly DateTime Now = new(2026, 8, 19, 12, 0, 0, DateTimeKind.Utc);

    private static Project DraftProject() => Project.Create(
        Guid.NewGuid(),
        Guid.NewGuid(),
        "3D-модель корпуса",
        "Нужна параметрическая 3D-модель корпуса по чертежам заказчика.",
        Money.Rub(10_000),
        DateOnly.FromDateTime(Now.AddDays(14)),
        Now);

    [Fact]
    public void Project_Publish_FromDraft_Succeeds()
    {
        var project = DraftProject();
        project.Publish(Now);
        project.Status.Should().Be(ProjectStatus.Published);
    }

    [Fact]
    public void Project_Publish_FromPublished_Throws()
    {
        var project = DraftProject();
        project.Publish(Now);
        var act = () => project.Publish(Now);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Bid_Withdraw_OnlyPending()
    {
        var project = DraftProject();
        project.Publish(Now);
        var bid = Bid.Place(project, Guid.NewGuid(), Money.Rub(9_000), 5,
            "Сделаю модель в SolidWorks с чертежами и STEP файлом.", Now);
        bid.Accept(Now);
        var act = () => bid.Withdraw(Now);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void RecordAcceptedBid_CreatesDeal_AndRaisesEvent()
    {
        var project = DraftProject();
        project.Publish(Now);
        var sellerId = Guid.NewGuid();
        var bid = Bid.Place(project, sellerId, Money.Rub(9_000), 5,
            "Сделаю модель в SolidWorks с чертежами и STEP файлом.", Now);
        var other = Bid.Place(project, Guid.NewGuid(), Money.Rub(8_500), 6,
            "Выполню модель быстрее и приложу пояснительную записку.", Now);
        project.MarkInProgress(Now);

        var deal = project.RecordAcceptedBid(bid, [other], Now);

        bid.Status.Should().Be(BidStatus.Accepted);
        other.Status.Should().Be(BidStatus.Rejected);
        deal.SellerId.Should().Be(sellerId);
        deal.Amount.Should().Be(9_000);
        deal.Conversation.Should().NotBeNull();
        project.DomainEvents.OfType<BidAccepted>().Should().ContainSingle(e => e.DealId == deal.Id);
    }

    [Fact]
    public void Deal_Fund_Submit_Accept_HappyPath()
    {
        var project = DraftProject();
        project.Publish(Now);
        var bid = Bid.Place(project, Guid.NewGuid(), Money.Rub(9_000), 5,
            "Сделаю модель в SolidWorks с чертежами и STEP файлом.", Now);
        project.MarkInProgress(Now);
        var deal = project.RecordAcceptedBid(bid, [], Now);

        deal.Fund(Now);
        deal.Status.Should().Be(DealStatus.InProgress);
        deal.DomainEvents.OfType<DealFunded>().Should().ContainSingle();

        var deliverable = deal.SubmitWork("Готово", Now.AddHours(1));
        deal.Status.Should().Be(DealStatus.Submitted);
        deliverable.DealId.Should().Be(deal.Id);
        deal.DomainEvents.OfType<WorkSubmitted>().Should().ContainSingle();

        deal.Accept(Now.AddHours(2));
        deal.Status.Should().Be(DealStatus.Completed);
        deal.DomainEvents.OfType<DealCompleted>().Should().ContainSingle();
    }

    [Fact]
    public void Service_Publish_AndArchive()
    {
        var service = Service.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Расчёт прочности детали",
            "Статический FEM-расчёт детали с отчётом и рекомендациями по геометрии.",
            Money.Rub(15_000),
            5,
            Now);
        service.Publish(Now);
        service.Status.Should().Be(ServiceStatus.Published);
        service.Archive(Now);
        service.Status.Should().Be(ServiceStatus.Archived);
        var act = () => service.Publish(Now);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Profile_RecalculateRating()
    {
        var profile = Profile.Create(Guid.NewGuid(), "Инженер", Now);
        profile.RecalculateRating([5, 4, 4]);
        profile.ReviewCount.Should().Be(3);
        profile.AverageRating.Should().Be(4.33m);
    }
}

public class ValidatorTests
{
    [Fact]
    public async Task CreateProjectValidator_RejectsShortTitle()
    {
        var validator = new CreateProjectValidator();
        var result = await validator.ValidateAsync(new CreateProjectRequest(
            "ab",
            "Достаточно длинное описание инженерной задачи для валидации",
            Guid.NewGuid(),
            1000,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7))));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task CreateBidValidator_RequiresCoverLetter()
    {
        var validator = new CreateBidValidator();
        var result = await validator.ValidateAsync(new CreateBidRequest(1000, 5, "short"));
        result.IsValid.Should().BeFalse();
    }
}
