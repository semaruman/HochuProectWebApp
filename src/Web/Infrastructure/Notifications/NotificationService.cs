using Web.Domain.Entities;
using Web.Infrastructure.Persistence;

namespace Web.Infrastructure.Notifications;

public interface INotificationService
{
    Task NotifyAsync(Guid userId, string type, string title, string body, string? linkUrl = null, CancellationToken ct = default);
}

public class NotificationService(AppDbContext db) : INotificationService
{
    public async Task NotifyAsync(Guid userId, string type, string title, string body, string? linkUrl = null, CancellationToken ct = default)
    {
        db.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = type,
            Title = title,
            Body = body,
            LinkUrl = linkUrl,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });
        await Task.CompletedTask;
    }
}
