using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Web.Domain.Enums;
using Web.Infrastructure.Persistence;
using Xunit;

namespace Web.IntegrationTests;

public class MarketplaceFlowTests : IAsyncLifetime
{
    private PostgreSqlContainer? _postgres;
    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;
    private string? _skipReason;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task InitializeAsync()
    {
        var externalCs = Environment.GetEnvironmentVariable("HOCHU_TEST_PG")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__Default");

        string connectionString;
        if (!string.IsNullOrWhiteSpace(externalCs))
        {
            connectionString = externalCs;
        }
        else
        {
            try
            {
                _postgres = new PostgreSqlBuilder()
                    .WithImage("postgres:16-alpine")
                    .WithDatabase("hochuproect_test")
                    .WithUsername("postgres")
                    .WithPassword("postgres")
                    .Build();
                await _postgres.StartAsync();
                connectionString = _postgres.GetConnectionString();
            }
            catch (Exception ex)
            {
                _skipReason = $"PostgreSQL for integration tests is unavailable: {ex.Message}";
                return;
            }
        }

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:Default", connectionString);
            builder.UseSetting("Database:MigrateOnStartup", "true");
            builder.UseSetting("FileStorage:Root", Path.Combine(Path.GetTempPath(), "hochuproect-tests", Guid.NewGuid().ToString("N")));
        });

        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        if (_factory is not null)
            await _factory.DisposeAsync();
        if (_postgres is not null)
            await _postgres.DisposeAsync();
    }

    [SkippableFact]
    public async Task HappyPath_CreatePublishBidAcceptFundSubmitAcceptReview()
    {
        Skip.If(_skipReason is not null, _skipReason);

        await RegisterAsync("buyer@test.local", "Buyer1!", "Buyer");
        await RegisterAsync("seller@test.local", "Seller1!", "Seller");

        await LoginAsync("buyer@test.local", "Buyer1!");
        Guid categoryId;
        using (var scope = _factory!.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            categoryId = await db.Categories.Select(c => c.Id).FirstAsync();
        }

        var createRes = await _client!.PostAsJsonAsync("/api/projects", new
        {
            title = "3D-модель корпуса оборудования",
            description = "Нужна параметрическая 3D-модель корпуса по чертежам с допусками.",
            categoryId,
            budgetAmount = 50000,
            deadline = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20))
        });
        createRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var project = await createRes.Content.ReadFromJsonAsync<IdDto>(JsonOptions);
        project.Should().NotBeNull();

        var publish = await _client.PostAsync($"/api/projects/{project!.Id}/publish", null);
        publish.EnsureSuccessStatusCode();

        await LoginAsync("seller@test.local", "Seller1!");
        var bidRes = await _client.PostAsJsonAsync($"/api/projects/{project.Id}/bids", new
        {
            price = 45000,
            estimatedDays = 10,
            coverLetter = "Сделаю модель в SolidWorks с чертежами и STEP."
        });
        bidRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var bid = await bidRes.Content.ReadFromJsonAsync<IdDto>(JsonOptions);

        await LoginAsync("buyer@test.local", "Buyer1!");
        var accept = await _client.PostAsync($"/api/bids/{bid!.Id}/accept", null);
        accept.EnsureSuccessStatusCode();
        var acceptBody = await accept.Content.ReadFromJsonAsync<AcceptDto>(JsonOptions);
        acceptBody!.DealId.Should().NotBeEmpty();

        var fund = await _client.PostAsync($"/api/deals/{acceptBody.DealId}/fund", null);
        fund.EnsureSuccessStatusCode();

        await LoginAsync("seller@test.local", "Seller1!");
        var submit = await _client.PostAsJsonAsync($"/api/deals/{acceptBody.DealId}/submit", new { message = "Готово, файлы в архиве." });
        submit.EnsureSuccessStatusCode();

        var msg = await _client.PostAsJsonAsync($"/api/deals/{acceptBody.DealId}/messages", new { text = "Модель приложена к сдаче." });
        msg.StatusCode.Should().Be(HttpStatusCode.Created);

        await LoginAsync("buyer@test.local", "Buyer1!");
        var acceptWork = await _client.PostAsync($"/api/deals/{acceptBody.DealId}/accept", null);
        acceptWork.EnsureSuccessStatusCode();

        var review = await _client.PostAsJsonAsync($"/api/deals/{acceptBody.DealId}/reviews", new
        {
            rating = 5,
            comment = "Отличная инженерная работа, всё по ТЗ."
        });
        review.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [SkippableFact]
    public async Task ConcurrentAcceptBid_OnlyOneSucceeds()
    {
        Skip.If(_skipReason is not null, _skipReason);

        await RegisterAsync("buyer2@test.local", "Buyer1!", "Buyer2");
        await RegisterAsync("sellerA@test.local", "Seller1!", "SellerA");
        await RegisterAsync("sellerB@test.local", "Seller1!", "SellerB");

        await LoginAsync("buyer2@test.local", "Buyer1!");
        Guid categoryId;
        using (var scope = _factory!.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            categoryId = await db.Categories.Select(c => c.Id).FirstAsync();
        }

        var createRes = await _client!.PostAsJsonAsync("/api/projects", new
        {
            title = "Расчёт прочности кронштейна",
            description = "Требуется FEM-расчёт кронштейна под статическую нагрузку по ТЗ.",
            categoryId,
            budgetAmount = 30000,
            deadline = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(15))
        });
        var project = await createRes.Content.ReadFromJsonAsync<IdDto>(JsonOptions);
        await _client.PostAsync($"/api/projects/{project!.Id}/publish", null);

        await LoginAsync("sellerA@test.local", "Seller1!");
        var bidA = await (await _client.PostAsJsonAsync($"/api/projects/{project.Id}/bids", new
        {
            price = 28000,
            estimatedDays = 7,
            coverLetter = "Сделаю расчёт в ANSYS с отчётом и рекомендациями."
        })).Content.ReadFromJsonAsync<IdDto>(JsonOptions);

        await LoginAsync("sellerB@test.local", "Seller1!");
        var bidB = await (await _client.PostAsJsonAsync($"/api/projects/{project.Id}/bids", new
        {
            price = 27000,
            estimatedDays = 8,
            coverLetter = "Выполню прочностной расчёт и оформлю пояснительную записку."
        })).Content.ReadFromJsonAsync<IdDto>(JsonOptions);

        var buyerClient1 = _factory!.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var buyerClient2 = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await LoginWithClientAsync(buyerClient1, "buyer2@test.local", "Buyer1!");
        await LoginWithClientAsync(buyerClient2, "buyer2@test.local", "Buyer1!");

        var t1 = buyerClient1.PostAsync($"/api/bids/{bidA!.Id}/accept", null);
        var t2 = buyerClient2.PostAsync($"/api/bids/{bidB!.Id}/accept", null);
        await Task.WhenAll(t1, t2);

        var statuses = new[] { t1.Result.StatusCode, t2.Result.StatusCode };
        statuses.Count(s => s is HttpStatusCode.OK or HttpStatusCode.Created).Should().Be(1);
        statuses.Count(s => s is HttpStatusCode.Conflict or HttpStatusCode.UnprocessableEntity).Should().Be(1);

        using var scope2 = _factory.Services.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db2.Deals.CountAsync(d => d.ProjectId == project.Id)).Should().Be(1);
        (await db2.Bids.CountAsync(b => b.ProjectId == project.Id && b.Status == BidStatus.Accepted)).Should().Be(1);
    }

    private async Task RegisterAsync(string email, string password, string name)
    {
        var res = await _client!.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password,
            displayName = name
        });
        res.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.BadRequest);
    }

    private Task LoginAsync(string email, string password) => LoginWithClientAsync(_client!, email, password);

    private static async Task LoginWithClientAsync(HttpClient client, string email, string password)
    {
        var res = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        res.EnsureSuccessStatusCode();
    }

    private sealed record IdDto(Guid Id);
    private sealed record AcceptDto(Guid DealId, Guid ProjectId, Guid BidId);
}
