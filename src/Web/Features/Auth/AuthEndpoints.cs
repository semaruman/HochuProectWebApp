using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text;
using Web.Common.Auth;
using Web.Common.Endpoints;
using Web.Common.Results;
using Web.Common.Validation;
using Web.Domain.Entities;
using Web.Infrastructure.Email;
using Web.Infrastructure.Persistence;

namespace Web.Features.Auth;

public record RegisterRequest(string Email, string Password, string DisplayName, bool AcceptTerms);
public record LoginRequest(string Email, string Password);
public record ResendConfirmationRequest(string Email);

public class RegisterValidator : AbstractValidator<RegisterRequest>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(100);
        RuleFor(x => x.DisplayName).NotEmpty().MinimumLength(2).MaximumLength(100);
        RuleFor(x => x.AcceptTerms).Equal(true).WithMessage("You must accept the terms and privacy policy.");
    }
}

public class LoginValidator : AbstractValidator<LoginRequest>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class AuthEndpoints : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/register", async (
            RegisterRequest request,
            IValidator<RegisterRequest> validator,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IHostEnvironment env,
            AppDbContext db,
            IEmailService email,
            IOptions<AppOptions> appOptions,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateRequestAsync(request, ct);
            if (validation.IsFailure)
                return validation.ToHttpResult(() => Results.Ok());

            var utcNow = DateTime.UtcNow;
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = request.Email,
                Email = request.Email,
                EmailConfirmed = env.IsDevelopment(),
                TermsAcceptedAt = utcNow,
                PrivacyPolicyAcceptedAt = utcNow
            };

            var result = await userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
                return ResultErrors.BadRequest(string.Join("; ", result.Errors.Select(e => e.Description))).ToProblemResult();

            var profileResult = Profile.Create(user.Id, request.DisplayName, utcNow);
            if (profileResult.IsFailure)
                return profileResult.ToHttpResult(_ => Results.Ok());

            db.Profiles.Add(profileResult.Value);
            await db.SaveChangesAsync(ct);

            if (!user.EmailConfirmed)
            {
                var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
                var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
                var link = $"{appOptions.Value.PublicBaseUrl.TrimEnd('/')}/verify-email.html?userId={user.Id}&token={encoded}";
                try
                {
                    await email.SendAsync(user.Email!, "Подтвердите email — Хочу Проект",
                        $"<p>Здравствуйте, {request.DisplayName}!</p><p><a href=\"{link}\">Подтвердить email</a></p>", ct);
                }
                catch
                {
                    // registration must succeed even if email fails
                }

                return Results.Created($"/api/profiles/{user.Id}", new
                {
                    user.Id,
                    request.Email,
                    request.DisplayName,
                    message = "Registration successful. Please confirm your email."
                });
            }

            await signInManager.SignInAsync(user, isPersistent: true);
            return Results.Created($"/api/profiles/{user.Id}", new { user.Id, request.Email, request.DisplayName });
        });

        group.MapPost("/login", async (
            LoginRequest request,
            IValidator<LoginRequest> validator,
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateRequestAsync(request, ct);
            if (validation.IsFailure)
                return validation.ToHttpResult(() => Results.Ok());

            var user = await userManager.FindByEmailAsync(request.Email);
            if (user?.IsBlocked == true)
                return ResultErrors.Forbidden("Account is blocked.").ToProblemResult();

            var result = await signInManager.PasswordSignInAsync(request.Email, request.Password, true, lockoutOnFailure: true);
            if (!result.Succeeded)
                return ResultErrors.BadRequest("Invalid email or password.").ToProblemResult();
            return Results.Ok(new { message = "Logged in" });
        });

        group.MapPost("/logout", async (SignInManager<ApplicationUser> signInManager) =>
        {
            await signInManager.SignOutAsync();
            return Results.Ok(new { message = "Logged out" });
        }).RequireAuthorization();

        group.MapGet("/me", async (ICurrentUser currentUser, UserManager<ApplicationUser> userManager) =>
        {
            var userIdResult = currentUser.GetUserId();
            if (userIdResult.IsFailure)
                return userIdResult.ToHttpResult(_ => Results.Ok());
            var userId = userIdResult.Value;

            var user = await userManager.FindByIdAsync(userId.ToString());
            return Results.Ok(new
            {
                userId,
                email = user?.Email,
                emailConfirmed = user?.EmailConfirmed == true,
                isBlocked = user?.IsBlocked == true
            });
        }).RequireAuthorization();

        group.MapPost("/confirm-email", async (
            ConfirmEmailRequest request,
            UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.FindByIdAsync(request.UserId.ToString());
            if (user is null)
                return ResultErrors.BadRequest("Invalid confirmation request.").ToProblemResult();

            string decoded;
            try
            {
                decoded = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Token));
            }
            catch
            {
                return ResultErrors.BadRequest("Invalid token.").ToProblemResult();
            }

            var result = await userManager.ConfirmEmailAsync(user, decoded);
            if (!result.Succeeded)
                return ResultErrors.BadRequest("Invalid or expired confirmation token.").ToProblemResult();

            return Results.Ok(new { message = "Email confirmed." });
        });

        group.MapPost("/resend-confirmation", async (
            ResendConfirmationRequest request,
            UserManager<ApplicationUser> userManager,
            IEmailService email,
            IOptions<AppOptions> appOptions) =>
        {
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user is not null && !user.EmailConfirmed)
            {
                var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
                var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
                var link = $"{appOptions.Value.PublicBaseUrl.TrimEnd('/')}/verify-email.html?userId={user.Id}&token={encoded}";
                try
                {
                    await email.SendAsync(user.Email!, "Подтвердите email — Хочу Проект",
                        $"<p><a href=\"{link}\">Подтвердить email</a></p>");
                }
                catch
                {
                    // ignore
                }
            }

            return Results.Ok(new { message = "If the email exists, a confirmation link was sent." });
        });
    }
}

public record ConfirmEmailRequest(Guid UserId, string Token);
