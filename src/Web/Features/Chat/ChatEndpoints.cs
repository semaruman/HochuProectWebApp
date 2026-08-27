using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Web.Common.Auth;
using Web.Common.Endpoints;
using Web.Common.Errors;
using Web.Common.Validation;
using Web.Domain.Entities;
using Web.Infrastructure.Notifications;
using Web.Infrastructure.Persistence;

namespace Web.Features.Chat;

public record SendMessageRequest(string Text);

public class SendMessageValidator : AbstractValidator<SendMessageRequest>
{
    public SendMessageValidator()
    {
        RuleFor(x => x.Text).NotEmpty().MaximumLength(4000);
    }
}

public class ChatEndpoints : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/deals/{dealId:guid}").WithTags("Chat").RequireAuthorization();

        group.MapGet("/messages", async (
            Guid dealId,
            Guid? afterId,
            int take,
            ICurrentUser currentUser,
            AppDbContext db,
            CancellationToken ct) =>
        {
            take = Math.Clamp(take <= 0 ? 50 : take, 1, 100);
            var userId = currentUser.UserId;
            var deal = await db.Deals.AsNoTracking().FirstOrDefaultAsync(d => d.Id == dealId, ct)
                ?? throw AppErrors.NotFound();
            if (!deal.IsParticipant(userId))
                throw AppErrors.Forbidden();

            var conversation = await db.Conversations.AsNoTracking().FirstOrDefaultAsync(c => c.DealId == dealId, ct)
                ?? throw AppErrors.NotFound("Conversation not found.");

            var query = db.Messages.AsNoTracking().Where(m => m.ConversationId == conversation.Id);
            if (afterId.HasValue)
            {
                var after = await db.Messages.AsNoTracking().FirstOrDefaultAsync(m => m.Id == afterId, ct);
                if (after is not null)
                    query = query.Where(m => m.CreatedAt > after.CreatedAt);
            }

            var messages = await query
                .OrderBy(m => m.CreatedAt)
                .Take(take)
                .Select(m => new
                {
                    m.Id,
                    m.SenderId,
                    m.Text,
                    m.CreatedAt,
                    m.ReadAt
                })
                .ToListAsync(ct);

            return Results.Ok(messages);
        });

        group.MapPost("/messages", async (
            Guid dealId,
            SendMessageRequest request,
            IValidator<SendMessageRequest> validator,
            ICurrentUser currentUser,
            AppDbContext db,
            INotificationService notifications,
            CancellationToken ct) =>
        {
            await validator.ValidateOrThrowAsync(request, ct);
            var userId = currentUser.UserId;
            var deal = await db.Deals.FirstOrDefaultAsync(d => d.Id == dealId, ct)
                ?? throw AppErrors.NotFound();
            if (!deal.IsParticipant(userId))
                throw AppErrors.Forbidden();

            var conversation = await db.Conversations.FirstOrDefaultAsync(c => c.DealId == dealId, ct)
                ?? throw AppErrors.NotFound("Conversation not found.");

            var message = Message.Create(conversation.Id, userId, request.Text, DateTime.UtcNow);
            db.Messages.Add(message);

            var recipient = deal.BuyerId == userId ? deal.SellerId : deal.BuyerId;
            await notifications.NotifyAsync(recipient, "NewMessage", "Новое сообщение",
                "Вам написали по сделке.", $"/Deals/Details/{deal.Id}", ct);

            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/deals/{dealId}/messages", new
            {
                message.Id,
                message.SenderId,
                message.Text,
                message.CreatedAt
            });
        });

        group.MapPost("/messages/read", async (
            Guid dealId,
            ICurrentUser currentUser,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var userId = currentUser.UserId;
            var deal = await db.Deals.AsNoTracking().FirstOrDefaultAsync(d => d.Id == dealId, ct)
                ?? throw AppErrors.NotFound();
            if (!deal.IsParticipant(userId))
                throw AppErrors.Forbidden();

            var conversation = await db.Conversations.FirstOrDefaultAsync(c => c.DealId == dealId, ct)
                ?? throw AppErrors.NotFound();

            var unread = await db.Messages
                .Where(m => m.ConversationId == conversation.Id && m.SenderId != userId && m.ReadAt == null)
                .ToListAsync(ct);
            var now = DateTime.UtcNow;
            foreach (var m in unread)
                m.MarkRead(now);
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { marked = unread.Count });
        });
    }
}
