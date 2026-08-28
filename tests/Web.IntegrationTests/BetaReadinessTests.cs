using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Web.Domain.Entities;
using Web.Domain.Enums;
using Web.Infrastructure.Persistence;
using Xunit;

namespace Web.IntegrationTests;

public class BetaReadinessTests : IClassFixture<IntegrationTestHost>
{
    private readonly IntegrationTestHost _host;

    public BetaReadinessTests(IntegrationTestHost host) => _host = host;

    [SkippableFact]
    public async Task AcceptBid_StartsDealInProgress_WithoutFund()
    {
        Skip.If(_host.SkipReason is not null, _host.SkipReason);

        var (_, _, dealId) = await _host.CreateDealAsync("buyer-flow@test.local", "seller-flow@test.local");

        await _host.LoginAsync("buyer-flow@test.local", "Buyer1!");
        var dealRes = await _host.Client.GetAsync($"/api/deals/{dealId}");
        dealRes.EnsureSuccessStatusCode();
        var deal = await dealRes.Content.ReadFromJsonAsync<DealDetailsResponse>(IntegrationTestHost.JsonOptions);
        deal!.Status.Should().Be(DealStatus.InProgress);
        deal.FundedAt.Should().NotBeNull();
    }

    [SkippableFact]
    public async Task DealFlow_Revision_Redeliver_Accept()
    {
        Skip.If(_host.SkipReason is not null, _host.SkipReason);

        var (_, _, dealId) = await _host.CreateDealAsync("buyer-rev@test.local", "seller-rev@test.local");

        await _host.LoginAsync("seller-rev@test.local", "Seller1!");
        (await _host.Client.PostAsJsonAsync($"/api/deals/{dealId}/submit", new { message = "Первая сдача" }))
            .EnsureSuccessStatusCode();

        await _host.LoginAsync("buyer-rev@test.local", "Buyer1!");
        var revision = await _host.Client.PostAsJsonAsync($"/api/deals/{dealId}/request-revision",
            new { comment = "Нужно исправить допуски на чертеже" });
        revision.EnsureSuccessStatusCode();

        await _host.LoginAsync("seller-rev@test.local", "Seller1!");
        (await _host.Client.PostAsJsonAsync($"/api/deals/{dealId}/submit", new { message = "Исправлено" }))
            .EnsureSuccessStatusCode();

        await _host.LoginAsync("buyer-rev@test.local", "Buyer1!");
        (await _host.Client.PostAsync($"/api/deals/{dealId}/accept", null)).EnsureSuccessStatusCode();

        var dealRes = await _host.Client.GetAsync($"/api/deals/{dealId}");
        var deal = await dealRes.Content.ReadFromJsonAsync<DealDetailsResponse>(IntegrationTestHost.JsonOptions);
        deal!.Status.Should().Be(DealStatus.Completed);
    }

    [SkippableFact]
    public async Task DeliverableFile_Download_AuthorizedOnly()
    {
        Skip.If(_host.SkipReason is not null, _host.SkipReason);

        var (_, _, dealId) = await _host.CreateDealAsync("buyer-file@test.local", "seller-file@test.local");

        await _host.LoginAsync("seller-file@test.local", "Seller1!");
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent("Готово"), "message");
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("test deliverable content"));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        content.Add(fileContent, "files", "result.txt");
        (await _host.Client.PostAsync($"/api/deals/{dealId}/submit", content)).EnsureSuccessStatusCode();

        Guid fileId;
        using (var scope = _host.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            fileId = await db.DealDeliverableFiles.Select(f => f.Id).FirstAsync();
        }

        await _host.LoginAsync("buyer-file@test.local", "Buyer1!");
        var download = await _host.Client.GetAsync($"/api/files/deliverable-files/{fileId}");
        download.StatusCode.Should().Be(HttpStatusCode.OK);
        download.Content.Headers.ContentType!.MediaType.Should().Be("text/plain");
        (await download.Content.ReadAsStringAsync()).Should().Be("test deliverable content");

        await _host.RegisterAsync("stranger@test.local", "Stranger1!", "Stranger");
        await _host.LoginAsync("stranger@test.local", "Stranger1!");
        var forbidden = await _host.Client.GetAsync($"/api/files/deliverable-files/{fileId}");
        forbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        await _host.LoginAsync("buyer-file@test.local", "Buyer1!");
        var missing = await _host.Client.GetAsync($"/api/files/deliverable-files/{Guid.NewGuid()}");
        missing.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [SkippableFact]
    public async Task Admin_RegularUserForbidden_AdminAllowed()
    {
        Skip.If(_host.SkipReason is not null, _host.SkipReason);

        await _host.RegisterAsync("regular@test.local", "Regular1!", "Regular");
        await _host.LoginAsync("regular@test.local", "Regular1!");
        (await _host.Client.GetAsync("/api/admin/users")).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        await _host.LoginAsync(IntegrationTestHost.AdminEmail, IntegrationTestHost.AdminPassword);
        (await _host.Client.GetAsync("/api/admin/users")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [SkippableFact]
    public async Task Admin_CanBlockUser_AndHideProject()
    {
        Skip.If(_host.SkipReason is not null, _host.SkipReason);

        var (projectId, _, dealId) = await _host.CreateDealAsync("buyer-admin@test.local", "seller-admin@test.local");
        await _host.RegisterAsync("victim@test.local", "Victim1!", "Victim");

        Guid victimId;
        using (var scope = _host.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            victimId = await db.Users.Where(u => u.Email == "victim@test.local").Select(u => u.Id).FirstAsync();
        }

        await _host.LoginAsync(IntegrationTestHost.AdminEmail, IntegrationTestHost.AdminPassword);
        (await _host.Client.PostAsync($"/api/admin/users/{victimId}/block", null)).EnsureSuccessStatusCode();
        (await _host.Client.PostAsync($"/api/admin/projects/{projectId}/hide", null)).EnsureSuccessStatusCode();
        (await _host.Client.GetAsync($"/api/admin/deals/{dealId}")).StatusCode.Should().Be(HttpStatusCode.OK);

        await _host.LoginAsync("victim@test.local", "Victim1!");
        (await _host.Client.PostAsJsonAsync("/api/projects", new
        {
            title = "Заблокированный пользователь",
            description = "Этот проект не должен создаваться для заблокированного аккаунта.",
            categoryId = await _host.GetCategoryIdAsync(),
            budgetAmount = 1000,
            deadline = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7))
        })).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using (var scope = _host.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var project = await db.Projects.FirstAsync(p => p.Id == projectId);
            project.Status.Should().Be(ProjectStatus.Hidden);
        }
    }

    [SkippableFact]
    public async Task Register_RequiresTermsAcceptance()
    {
        Skip.If(_host.SkipReason is not null, _host.SkipReason);

        var client = _host.CreateClient();
        var res = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "noterms@test.local",
            password = "NoTerms1!",
            displayName = "No Terms",
            acceptTerms = false
        });
        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [SkippableFact]
    public async Task PasswordReset_ValidAndInvalidToken()
    {
        Skip.If(_host.SkipReason is not null, _host.SkipReason);

        await _host.RegisterAsync("reset@test.local", "ResetOld1!", "Reset User");

        string encodedToken;
        using (var scope = _host.Factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByEmailAsync("reset@test.local");
            var token = await userManager.GeneratePasswordResetTokenAsync(user!);
            encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        }

        var client = _host.CreateClient();
        (await client.PostAsJsonAsync("/api/auth/reset-password", new
        {
            email = "reset@test.local",
            token = encodedToken,
            newPassword = "ResetNew1!"
        })).StatusCode.Should().Be(HttpStatusCode.OK);

        await IntegrationTestHost.LoginAsync(client, "reset@test.local", "ResetNew1!");

        (await client.PostAsJsonAsync("/api/auth/reset-password", new
        {
            email = "reset@test.local",
            token = "invalid-token",
            newPassword = "Another1!"
        })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private sealed record DealDetailsResponse(
        Guid Id,
        DealStatus Status,
        DateTime? FundedAt,
        DateTime? SubmittedAt,
        DateTime? CompletedAt);
}
