using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;
using Web.Common.Endpoints;
using Web.Common.Errors;
using Web.Common.Validation;
using Web.Domain.Entities;

namespace Web.Features.Auth;

public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Email, string Token, string NewPassword);

public class ForgotPasswordValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordValidator() => RuleFor(x => x.Email).NotEmpty().EmailAddress();
}

public class ResetPasswordValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8);
    }
}

public class PasswordResetEndpoints : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/forgot-password", async (
            ForgotPasswordRequest request,
            IValidator<ForgotPasswordRequest> validator,
            UserManager<ApplicationUser> userManager,
            IHostEnvironment env,
            ILogger<PasswordResetEndpoints> logger,
            CancellationToken ct) =>
        {
            await validator.ValidateOrThrowAsync(request, ct);
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user is not null)
            {
                var token = await userManager.GeneratePasswordResetTokenAsync(user);
                var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
                logger.LogInformation("Password reset token generated for {Email}", request.Email);
                if (env.IsDevelopment())
                    logger.LogWarning("Dev-only password reset token for {Email}: {Token}", request.Email, encoded);
            }

            return Results.Ok(new { message = "If the email exists, a reset link was generated." });
        });

        group.MapPost("/reset-password", async (
            ResetPasswordRequest request,
            IValidator<ResetPasswordRequest> validator,
            UserManager<ApplicationUser> userManager,
            CancellationToken ct) =>
        {
            await validator.ValidateOrThrowAsync(request, ct);
            var user = await userManager.FindByEmailAsync(request.Email)
                ?? throw AppErrors.BadRequest("Invalid reset request.");

            string decoded;
            try
            {
                decoded = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Token));
            }
            catch
            {
                throw AppErrors.BadRequest("Invalid token.");
            }

            var result = await userManager.ResetPasswordAsync(user, decoded, request.NewPassword);
            if (!result.Succeeded)
                throw AppErrors.BadRequest(string.Join("; ", result.Errors.Select(e => e.Description)));

            return Results.Ok(new { message = "Password updated." });
        });
    }
}
