using FluentAssertions;
using Web.Common.Results;
using Web.Domain.Entities;
using Web.Domain.Enums;
using Web.Domain.Events;
using Web.Domain.ValueObjects;
using Web.Features.Bids;
using Web.Features.Auth;
using Web.Features.Projects;

namespace Web.UnitTests;

public class MoneyTests
{
    [Fact]
    public void Rub_RejectsNonPositiveAmount()
    {
        var result = Money.Rub(0);
        result.IsFailure.Should().BeTrue();
        result.Error.Kind.Should().Be(ErrorKind.Business);
    }

    [Fact]
    public void Constructor_NormalizesCurrency()
    {
        var result = Money.TryCreate(10.555m, "rub");
        result.IsSuccess.Should().BeTrue();
        result.Value.Currency.Should().Be("RUB");
        result.Value.Amount.Should().Be(10.56m);
    }
}

public class DomainStateMachineTests
{
    private static readonly DateTime Now = new(2026, 8, 19, 12, 0, 0, DateTimeKind.Utc);

    private static Project DraftProject()
    {
        var budget = Money.Rub(10_000).Value;
        return Project.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "3D-модель корпуса",
            "Нужна параметрическая 3D-модель корпуса по чертежам заказчика.",
            budget,
            DateOnly.FromDateTime(Now.AddDays(14)),
            Now).Value;
    }

    [Fact]
    public void Project_Publish_FromDraft_Succeeds()
    {
        var project = DraftProject();
        project.Publish(Now).IsSuccess.Should().BeTrue();
        project.Status.Should().Be(ProjectStatus.Published);
    }

    [Fact]
    public void Project_Publish_FromPublished_Fails()
    {
        var project = DraftProject();
        project.Publish(Now).IsSuccess.Should().BeTrue();
        project.Publish(Now).IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Bid_Withdraw_OnlyPending()
    {
        var project = DraftProject();
        project.Publish(Now);
        var bid = Bid.Place(project, Guid.NewGuid(), Money.Rub(9_000).Value, 5,
            "Сделаю модель в SolidWorks с чертежами и STEP файлом.", Now).Value;
        bid.Accept(Now).IsSuccess.Should().BeTrue();
        bid.Withdraw(Now).IsFailure.Should().BeTrue();
    }

    [Fact]
    public void RecordAcceptedBid_CreatesDeal_AndRaisesEvent()
    {
        var project = DraftProject();
        project.Publish(Now);
        var sellerId = Guid.NewGuid();
        var bid = Bid.Place(project, sellerId, Money.Rub(9_000).Value, 5,
            "Сделаю модель в SolidWorks с чертежами и STEP файлом.", Now).Value;
        var other = Bid.Place(project, Guid.NewGuid(), Money.Rub(8_500).Value, 6,
            "Выполню модель быстрее и приложу пояснительную записку.", Now).Value;
        project.MarkInProgress(Now).IsSuccess.Should().BeTrue();

        var dealResult = project.RecordAcceptedBid(bid, [other], Now);
        dealResult.IsSuccess.Should().BeTrue();
        var deal = dealResult.Value;

        bid.Status.Should().Be(BidStatus.Accepted);
        other.Status.Should().Be(BidStatus.Rejected);
        deal.SellerId.Should().Be(sellerId);
        deal.Amount.Should().Be(9_000);
        deal.Conversation.Should().NotBeNull();
        deal.Status.Should().Be(DealStatus.InProgress);
        project.DomainEvents.OfType<BidAccepted>().Should().ContainSingle(e => e.DealId == deal.Id);
    }

    [Fact]
    public void Deal_BetaFlow_Submit_Revision_Accept()
    {
        var project = DraftProject();
        project.Publish(Now);
        var bid = Bid.Place(project, Guid.NewGuid(), Money.Rub(9_000).Value, 5,
            "Сделаю модель в SolidWorks с чертежами и STEP файлом.", Now).Value;
        project.MarkInProgress(Now);
        var deal = project.RecordAcceptedBid(bid, [], Now).Value;

        deal.Status.Should().Be(DealStatus.InProgress);
        deal.FundedAt.Should().NotBeNull();

        var deliverable = deal.SubmitWork("Готово", Now.AddHours(1));
        deliverable.IsSuccess.Should().BeTrue();
        deal.Status.Should().Be(DealStatus.Submitted);
        deliverable.Value.DealId.Should().Be(deal.Id);
        deal.DomainEvents.OfType<WorkSubmitted>().Should().ContainSingle();

        deal.RequestRevision("Нужно поправить чертёж по допускам", Now.AddHours(2)).IsSuccess.Should().BeTrue();
        deal.Status.Should().Be(DealStatus.RevisionRequired);

        deal.SubmitWork("Исправлено", Now.AddHours(3)).IsSuccess.Should().BeTrue();
        deal.Status.Should().Be(DealStatus.Submitted);

        deal.Accept(Now.AddHours(4)).IsSuccess.Should().BeTrue();
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
            Money.Rub(15_000).Value,
            5,
            Now).Value;
        service.Publish(Now).IsSuccess.Should().BeTrue();
        service.Status.Should().Be(ServiceStatus.Published);
        service.Archive(Now).IsSuccess.Should().BeTrue();
        service.Status.Should().Be(ServiceStatus.Archived);
        service.Publish(Now).IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Profile_RecalculateRating()
    {
        var profile = Profile.Create(Guid.NewGuid(), "Инженер", Now).Value;
        profile.RecalculateRating([5, 4, 4]);
        profile.ReviewCount.Should().Be(3);
        profile.AverageRating.Should().Be(4.33m);
    }
}

public class ValidatorTests
{
    [Fact]
    public async Task RegisterValidator_RejectsMissingTerms()
    {
        var validator = new RegisterValidator();
        var result = await validator.ValidateAsync(new RegisterRequest(
            "user@test.local", "Password1", "User", AcceptTerms: false));
        result.IsValid.Should().BeFalse();
    }

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
