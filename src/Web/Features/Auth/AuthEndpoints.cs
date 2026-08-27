using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Web.Common.Auth;
using Web.Common.Endpoints;
using Web.Common.Errors;
using Web.Common.Validation;
using Web.Domain.Entities;
using Web.Infrastructure.Persistence;

namespace Web.Features.Auth;

public record RegisterRequest(string Email, string Password, string DisplayName);
public record LoginRequest(string Email, string Password);

public class RegisterValidator : AbstractValidator<RegisterRequest>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(100);
        RuleFor(x => x.DisplayName).NotEmpty().MinimumLength(2).MaximumLength(100);
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
            CancellationToken ct) =>
        {
            await validator.ValidateOrThrowAsync(request, ct);

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = request.Email,
                Email = request.Email,
                EmailConfirmed = env.IsDevelopment()
            };

            var result = await userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
                throw AppErrors.BadRequest(string.Join("; ", result.Errors.Select(e => e.Description)));

            db.Profiles.Add(Profile.Create(user.Id, request.DisplayName, DateTime.UtcNow));
            await db.SaveChangesAsync(ct);
            await signInManager.SignInAsync(user, isPersistent: true);

            return Results.Created($"/api/profiles/{user.Id}", new { user.Id, request.Email, request.DisplayName });
        });

        group.MapPost("/login", async (
            LoginRequest request,
            IValidator<LoginRequest> validator,
            SignInManager<ApplicationUser> signInManager,
            CancellationToken ct) =>
        {
            await validator.ValidateOrThrowAsync(request, ct);
            var result = await signInManager.PasswordSignInAsync(request.Email, request.Password, true, lockoutOnFailure: true);
            if (!result.Succeeded)
                throw AppErrors.BadRequest("Invalid email or password.");
            return Results.Ok(new { message = "Logged in" });
        });

        group.MapPost("/logout", async (SignInManager<ApplicationUser> signInManager) =>
        {
            await signInManager.SignOutAsync();
            return Results.Ok(new { message = "Logged out" });
        }).RequireAuthorization();

        group.MapGet("/me", (ICurrentUser currentUser) => Results.Ok(new { userId = currentUser.UserId }))
            .RequireAuthorization();
    }
}
