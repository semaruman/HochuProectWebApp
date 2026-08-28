using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Web.Infrastructure.Persistence;

namespace Web.IntegrationTests;

public sealed class IntegrationTestHost : IAsyncLifetime
{
    private PostgreSqlContainer? _postgres;
    public string? SkipReason { get; private set; }
    public WebApplicationFactory<Program> Factory { get; private set; } = null!;
    public HttpClient Client { get; private set; } = null!;

    public static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public const string AdminEmail = "admin@test.local";
    public const string AdminPassword = "Admin123!";

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
                SkipReason = $"PostgreSQL for integration tests is unavailable: {ex.Message}";
                Factory = null!;
                Client = null!;
                return;
            }
        }

        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:Default", connectionString);
            builder.UseSetting("Database:MigrateOnStartup", "true");
            builder.UseSetting("FileStorage:Root", Path.Combine(Path.GetTempPath(), "hochuproect-tests", Guid.NewGuid().ToString("N")));
            builder.UseSetting("Admin:Email", AdminEmail);
            builder.UseSetting("Admin:Password", AdminPassword);
        });

        Client = Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();
        if (Factory is not null)
            await Factory.DisposeAsync();
        if (_postgres is not null)
            await _postgres.DisposeAsync();
    }

    public HttpClient CreateClient() =>
        Factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

    public async Task RegisterAsync(HttpClient client, string email, string password, string name, bool acceptTerms = true)
    {
        var res = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password,
            displayName = name,
            acceptTerms
        });
        res.StatusCode.Should().BeOneOf(System.Net.HttpStatusCode.Created, System.Net.HttpStatusCode.BadRequest);
    }

    public Task RegisterAsync(string email, string password, string name, bool acceptTerms = true) =>
        RegisterAsync(Client, email, password, name, acceptTerms);

    public static async Task LoginAsync(HttpClient client, string email, string password)
    {
        var res = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        res.EnsureSuccessStatusCode();
    }

    public Task LoginAsync(string email, string password) => LoginAsync(Client, email, password);

    public async Task<Guid> GetCategoryIdAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Categories.Select(c => c.Id).FirstAsync();
    }

    public async Task<(Guid ProjectId, Guid BidId, Guid DealId)> CreateDealAsync(
        string buyerEmail = "buyer@test.local",
        string sellerEmail = "seller@test.local")
    {
        await RegisterAsync(buyerEmail, "Buyer1!", "Buyer");
        await RegisterAsync(sellerEmail, "Seller1!", "Seller");

        await LoginAsync(buyerEmail, "Buyer1!");
        var categoryId = await GetCategoryIdAsync();

        var createRes = await Client.PostAsJsonAsync("/api/projects", new
        {
            title = "Тестовый инженерный проект",
            description = "Описание тестового проекта для интеграционных проверок beta flow.",
            categoryId,
            budgetAmount = 50000,
            deadline = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20))
        });
        createRes.EnsureSuccessStatusCode();
        var project = await createRes.Content.ReadFromJsonAsync<IdDto>(JsonOptions)
            ?? throw new InvalidOperationException("Project creation failed.");

        await Client.PostAsync($"/api/projects/{project.Id}/publish", null);

        await LoginAsync(sellerEmail, "Seller1!");
        var bidRes = await Client.PostAsJsonAsync($"/api/projects/{project.Id}/bids", new
        {
            price = 45000,
            estimatedDays = 10,
            coverLetter = "Выполню работу в срок с полным комплектом документации."
        });
        bidRes.EnsureSuccessStatusCode();
        var bid = await bidRes.Content.ReadFromJsonAsync<IdDto>(JsonOptions)
            ?? throw new InvalidOperationException("Bid creation failed.");

        await LoginAsync(buyerEmail, "Buyer1!");
        var accept = await Client.PostAsync($"/api/bids/{bid.Id}/accept", null);
        accept.EnsureSuccessStatusCode();
        var acceptBody = await accept.Content.ReadFromJsonAsync<AcceptDto>(JsonOptions)
            ?? throw new InvalidOperationException("Bid accept failed.");

        return (project.Id, bid.Id, acceptBody.DealId);
    }

    public sealed record IdDto(Guid Id);
    public sealed record AcceptDto(Guid DealId, Guid ProjectId, Guid BidId);
}
