using Web.Domain.Events;
using Web.Infrastructure.Persistence;

namespace Web.Infrastructure.DomainEvents;

public static class DomainEventDbContextExtensions
{
    public static async Task SaveAndDispatchAsync(
        this AppDbContext db,
        IDomainEventDispatcher dispatcher,
        CancellationToken cancellationToken = default)
    {
        var events = db.ChangeTracker.Entries()
            .Select(entry => entry.Entity)
            .OfType<Entity>()
            .SelectMany(entity =>
            {
                var raised = entity.DomainEvents.ToArray();
                entity.ClearDomainEvents();
                return raised;
            })
            .ToList();

        await dispatcher.DispatchAsync(events, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }
}
