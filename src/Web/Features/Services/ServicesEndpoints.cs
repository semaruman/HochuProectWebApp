using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Web.Common.Auth;
using Web.Common.Endpoints;
using Web.Common.Errors;
using Web.Common.Validation;
using Web.Domain.Entities;
using Web.Domain.Enums;
using Web.Domain.ValueObjects;
using Web.Infrastructure.Persistence;

namespace Web.Features.Services;

public record CreateServiceRequest(string Title, string Description, Guid CategoryId, decimal Price, int DeliveryDays);
public record UpdateServiceRequest(string Title, string Description, Guid CategoryId, decimal Price, int DeliveryDays);

public class CreateServiceValidator : AbstractValidator<CreateServiceRequest>
{
    public CreateServiceValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MinimumLength(5).MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MinimumLength(20).MaximumLength(10000);
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.DeliveryDays).GreaterThan(0).LessThanOrEqualTo(3650);
    }
}

public class UpdateServiceValidator : AbstractValidator<UpdateServiceRequest>
{
    public UpdateServiceValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MinimumLength(5).MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MinimumLength(20).MaximumLength(10000);
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.DeliveryDays).GreaterThan(0).LessThanOrEqualTo(3650);
    }
}

public class ServicesEndpoints : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/services").WithTags("Services");

        group.MapGet("/", async (AppDbContext db, Guid? categoryId, int page = 1, int pageSize = 20, CancellationToken ct = default) =>
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 50);
            var query = db.Services.AsNoTracking()
                .Include(s => s.Category)
                .Where(s => s.Status == ServiceStatus.Published);
            if (categoryId.HasValue)
                query = query.Where(s => s.CategoryId == categoryId);

            var total = await query.CountAsync(ct);
            var items = await query.OrderByDescending(s => s.CreatedAt)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(s => new
                {
                    s.Id,
                    s.Title,
                    s.Price,
                    s.DeliveryDays,
                    s.SellerId,
                    Category = new { s.Category.Id, s.Category.Name }
                })
                .ToListAsync(ct);
            return Results.Ok(new { total, page, pageSize, items });
        });

        group.MapGet("/{id:guid}", async (Guid id, ICurrentUser currentUser, AppDbContext db, CancellationToken ct) =>
        {
            var service = await db.Services.AsNoTracking()
                .Include(s => s.Category)
                .FirstOrDefaultAsync(s => s.Id == id, ct)
                ?? throw AppErrors.NotFound();
            var userId = currentUser.TryGetUserId();
            if (service.Status != ServiceStatus.Published && service.SellerId != userId)
                throw AppErrors.NotFound();

            return Results.Ok(new
            {
                service.Id,
                service.Title,
                service.Description,
                service.Price,
                service.DeliveryDays,
                service.SellerId,
                service.Status,
                Category = new { service.Category.Id, service.Category.Name, service.Category.Slug }
            });
        });

        group.MapPost("/", async (
            CreateServiceRequest request,
            IValidator<CreateServiceRequest> validator,
            ICurrentUser currentUser,
            AppDbContext db,
            CancellationToken ct) =>
        {
            await validator.ValidateOrThrowAsync(request, ct);
            if (!await db.Categories.AnyAsync(c => c.Id == request.CategoryId, ct))
                throw AppErrors.BadRequest("Category not found.");

            var service = Service.Create(
                currentUser.UserId,
                request.CategoryId,
                request.Title,
                request.Description,
                Money.Rub(request.Price),
                request.DeliveryDays,
                DateTime.UtcNow);
            db.Services.Add(service);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/services/{service.Id}", service);
        }).RequireAuthorization();

        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateServiceRequest request,
            IValidator<UpdateServiceRequest> validator,
            ICurrentUser currentUser,
            AppDbContext db,
            CancellationToken ct) =>
        {
            await validator.ValidateOrThrowAsync(request, ct);
            var service = await db.Services.FirstOrDefaultAsync(s => s.Id == id, ct)
                ?? throw AppErrors.NotFound();
            if (service.SellerId != currentUser.UserId)
                throw AppErrors.Forbidden();

            service.Update(
                request.Title,
                request.Description,
                request.CategoryId,
                Money.Rub(request.Price),
                request.DeliveryDays,
                DateTime.UtcNow);
            await db.SaveChangesAsync(ct);
            return Results.Ok(service);
        }).RequireAuthorization();

        group.MapPost("/{id:guid}/publish", async (Guid id, ICurrentUser currentUser, AppDbContext db, CancellationToken ct) =>
        {
            var service = await db.Services.FirstOrDefaultAsync(s => s.Id == id, ct)
                ?? throw AppErrors.NotFound();
            if (service.SellerId != currentUser.UserId)
                throw AppErrors.Forbidden();
            service.Publish(DateTime.UtcNow);
            await db.SaveChangesAsync(ct);
            return Results.Ok(service);
        }).RequireAuthorization();

        group.MapPost("/{id:guid}/archive", async (Guid id, ICurrentUser currentUser, AppDbContext db, CancellationToken ct) =>
        {
            var service = await db.Services.FirstOrDefaultAsync(s => s.Id == id, ct)
                ?? throw AppErrors.NotFound();
            if (service.SellerId != currentUser.UserId)
                throw AppErrors.Forbidden();
            service.Archive(DateTime.UtcNow);
            await db.SaveChangesAsync(ct);
            return Results.Ok(service);
        }).RequireAuthorization();
    }
}
