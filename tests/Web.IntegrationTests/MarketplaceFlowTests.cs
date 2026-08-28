using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Web.Infrastructure.Persistence;
using Xunit;

namespace Web.IntegrationTests;

public class MarketplaceFlowTests : IClassFixture<IntegrationTestHost>
{
    private readonly IntegrationTestHost _host;

    public MarketplaceFlowTests(IntegrationTestHost host) => _host = host;

    [SkippableFact]
    public async Task HappyPath_CreatePublishBidAcceptSubmitAcceptReview()
    {
        Skip.If(_host.SkipReason is not null, _host.SkipReason);

        await _host.RegisterAsync("buyer@test.local", "Buyer1!", "Buyer");
        await _host.RegisterAsync("seller@test.local", "Seller1!", "Seller");

        await _host.LoginAsync("buyer@test.local", "Buyer1!");
        var categoryId = await _host.GetCategoryIdAsync();

        var createRes = await _host.Client.PostAsJsonAsync("/api/projects", new
        {
            title = "3D-модель корпуса оборудования",
            description = "Нужна параметрическая 3D-модель корпуса по чертежам с допусками.",
            categoryId,
            budgetAmount = 50000,
            deadline = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20))
        });
        createRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var project = await createRes.Content.ReadFromJsonAsync<IntegrationTestHost.IdDto>(IntegrationTestHost.JsonOptions);
        project.Should().NotBeNull();

        var publish = await _host.Client.PostAsync($"/api/projects/{project!.Id}/publish", null);
        publish.EnsureSuccessStatusCode();

        await _host.LoginAsync("seller@test.local", "Seller1!");
        var bidRes = await _host.Client.PostAsJsonAsync($"/api/projects/{project.Id}/bids", new
        {
            price = 45000,
            estimatedDays = 10,
            coverLetter = "Сделаю модель в SolidWorks с чертежами и STEP."
        });
        bidRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var bid = await bidRes.Content.ReadFromJsonAsync<IntegrationTestHost.IdDto>(IntegrationTestHost.JsonOptions);

        await _host.LoginAsync("buyer@test.local", "Buyer1!");
        var accept = await _host.Client.PostAsync($"/api/bids/{bid!.Id}/accept", null);
        accept.EnsureSuccessStatusCode();
        var acceptBody = await accept.Content.ReadFromJsonAsync<IntegrationTestHost.AcceptDto>(IntegrationTestHost.JsonOptions);
        acceptBody!.DealId.Should().NotBeEmpty();

        await _host.LoginAsync("seller@test.local", "Seller1!");
        var submit = await _host.Client.PostAsJsonAsync($"/api/deals/{acceptBody.DealId}/submit", new { message = "Готово, файлы в архиве." });
        submit.EnsureSuccessStatusCode();

        var msg = await _host.Client.PostAsJsonAsync($"/api/deals/{acceptBody.DealId}/messages", new { text = "Модель приложена к сдаче." });
        msg.StatusCode.Should().Be(HttpStatusCode.Created);

        await _host.LoginAsync("buyer@test.local", "Buyer1!");
        var acceptWork = await _host.Client.PostAsync($"/api/deals/{acceptBody.DealId}/accept", null);
        acceptWork.EnsureSuccessStatusCode();

        var review = await _host.Client.PostAsJsonAsync($"/api/deals/{acceptBody.DealId}/reviews", new
        {
            rating = 5,
            comment = "Отличная инженерная работа, всё по ТЗ."
        });
        review.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [SkippableFact]
    public async Task ConcurrentAcceptBid_OnlyOneSucceeds()
    {
        Skip.If(_host.SkipReason is not null, _host.SkipReason);

        await _host.RegisterAsync("buyer2@test.local", "Buyer1!", "Buyer2");
        await _host.RegisterAsync("sellerA@test.local", "Seller1!", "SellerA");
        await _host.RegisterAsync("sellerB@test.local", "Seller1!", "SellerB");

        await _host.LoginAsync("buyer2@test.local", "Buyer1!");
        var categoryId = await _host.GetCategoryIdAsync();

        var createRes = await _host.Client.PostAsJsonAsync("/api/projects", new
        {
            title = "Расчёт прочности кронштейна",
            description = "Требуется FEM-расчёт кронштейна под статическую нагрузку по ТЗ.",
            categoryId,
            budgetAmount = 30000,
            deadline = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(15))
        });
        var project = await createRes.Content.ReadFromJsonAsync<IntegrationTestHost.IdDto>(IntegrationTestHost.JsonOptions);
        await _host.Client.PostAsync($"/api/projects/{project!.Id}/publish", null);

        await _host.LoginAsync("sellerA@test.local", "Seller1!");
        var bidA = await (await _host.Client.PostAsJsonAsync($"/api/projects/{project.Id}/bids", new
        {
            price = 28000,
            estimatedDays = 7,
            coverLetter = "Сделаю расчёт в ANSYS с отчётом и рекомендациями."
        })).Content.ReadFromJsonAsync<IntegrationTestHost.IdDto>(IntegrationTestHost.JsonOptions);

        await _host.LoginAsync("sellerB@test.local", "Seller1!");
        var bidB = await (await _host.Client.PostAsJsonAsync($"/api/projects/{project.Id}/bids", new
        {
            price = 27000,
            estimatedDays = 8,
            coverLetter = "Выполню прочностной расчёт и оформлю пояснительную записку."
        })).Content.ReadFromJsonAsync<IntegrationTestHost.IdDto>(IntegrationTestHost.JsonOptions);

        var buyerClient1 = _host.Factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { HandleCookies = true });
        var buyerClient2 = _host.Factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { HandleCookies = true });
        await IntegrationTestHost.LoginAsync(buyerClient1, "buyer2@test.local", "Buyer1!");
        await IntegrationTestHost.LoginAsync(buyerClient2, "buyer2@test.local", "Buyer1!");

        var t1 = buyerClient1.PostAsync($"/api/bids/{bidA!.Id}/accept", null);
        var t2 = buyerClient2.PostAsync($"/api/bids/{bidB!.Id}/accept", null);
        await Task.WhenAll(t1, t2);

        var statuses = new[] { t1.Result.StatusCode, t2.Result.StatusCode };
        statuses.Count(s => s is HttpStatusCode.OK or HttpStatusCode.Created).Should().Be(1);
        statuses.Count(s => s is HttpStatusCode.Conflict or HttpStatusCode.UnprocessableEntity).Should().Be(1);

        using var scope2 = _host.Factory.Services.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db2.Deals.CountAsync(d => d.ProjectId == project.Id)).Should().Be(1);
        (await db2.Bids.CountAsync(b => b.ProjectId == project.Id && b.Status == Web.Domain.Enums.BidStatus.Accepted)).Should().Be(1);
    }
}
