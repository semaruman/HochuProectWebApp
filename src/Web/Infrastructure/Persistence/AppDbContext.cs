using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Web.Domain.Entities;
using Web.Domain.Enums;

namespace Web.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<UserSkill> UserSkills => Set<UserSkill>();
    public DbSet<PortfolioItem> PortfolioItems => Set<PortfolioItem>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectAttachment> ProjectAttachments => Set<ProjectAttachment>();
    public DbSet<Bid> Bids => Set<Bid>();
    public DbSet<Deal> Deals => Set<Deal>();
    public DbSet<DealDeliverable> DealDeliverables => Set<DealDeliverable>();
    public DbSet<DealDeliverableFile> DealDeliverableFiles => Set<DealDeliverableFile>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Profile>(e =>
        {
            e.HasKey(x => x.UserId);
            e.Property(x => x.DisplayName).HasMaxLength(100).IsRequired();
            e.Property(x => x.Bio).HasMaxLength(2000);
            e.Property(x => x.AvatarPath).HasMaxLength(500);
            e.Property(x => x.AverageRating).HasPrecision(3, 2);
            e.HasOne(x => x.User).WithOne(x => x.Profile).HasForeignKey<Profile>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Skill>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.Name).IsUnique();
        });

        builder.Entity<UserSkill>(e =>
        {
            e.HasKey(x => new { x.UserId, x.SkillId });
            e.HasOne(x => x.Profile).WithMany(x => x.UserSkills).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Skill).WithMany(x => x.UserSkills).HasForeignKey(x => x.SkillId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<PortfolioItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).HasMaxLength(2000);
            e.Property(x => x.Url).HasMaxLength(500);
            e.Property(x => x.FilePath).HasMaxLength(500);
            e.HasOne(x => x.Profile).WithMany(x => x.PortfolioItems).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Category>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Slug).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.Slug).IsUnique();
        });

        builder.Entity<Project>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).HasMaxLength(10000).IsRequired();
            e.Property(x => x.BudgetAmount).HasPrecision(18, 2);
            e.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            e.Property(x => x.RowVersion).IsConcurrencyToken();
            e.HasIndex(x => new { x.Status, x.CreatedAt });
            e.HasIndex(x => x.BuyerId);
            e.HasOne(x => x.Buyer).WithMany().HasForeignKey(x => x.BuyerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
            e.Ignore(x => x.DomainEvents);
        });

        builder.Entity<ProjectAttachment>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.StorageKey).HasMaxLength(500).IsRequired();
            e.Property(x => x.FileName).HasMaxLength(255).IsRequired();
            e.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.ProjectId);
            e.HasOne(x => x.Project).WithMany(x => x.Attachments).HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Bid>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Price).HasPrecision(18, 2);
            e.Property(x => x.CoverLetter).HasMaxLength(5000).IsRequired();
            e.HasIndex(x => x.ProjectId);
            e.HasIndex(x => x.SellerId);
            e.HasIndex(x => new { x.ProjectId, x.SellerId })
                .IsUnique()
                .HasFilter($"\"Status\" = {(int)BidStatus.Pending}");
            e.HasOne(x => x.Project).WithMany(x => x.Bids).HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Seller).WithMany().HasForeignKey(x => x.SellerId).OnDelete(DeleteBehavior.Restrict);
            e.Ignore(x => x.DomainEvents);
        });

        builder.Entity<Deal>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.LastRevisionComment).HasMaxLength(5000);
            e.Property(x => x.RowVersion).IsConcurrencyToken();
            e.HasIndex(x => x.ProjectId).IsUnique();
            e.HasIndex(x => x.BidId).IsUnique();
            e.HasIndex(x => x.BuyerId);
            e.HasIndex(x => x.SellerId);
            e.HasOne(x => x.Project).WithOne(x => x.Deal).HasForeignKey<Deal>(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Bid).WithOne(x => x.Deal).HasForeignKey<Deal>(x => x.BidId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Buyer).WithMany().HasForeignKey(x => x.BuyerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Seller).WithMany().HasForeignKey(x => x.SellerId).OnDelete(DeleteBehavior.Restrict);
            e.Ignore(x => x.DomainEvents);
        });

        builder.Entity<DealDeliverable>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Message).HasMaxLength(5000);
            e.HasOne(x => x.Deal).WithMany(x => x.Deliverables).HasForeignKey(x => x.DealId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<DealDeliverableFile>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.StorageKey).HasMaxLength(500).IsRequired();
            e.Property(x => x.FileName).HasMaxLength(255).IsRequired();
            e.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
            e.HasOne(x => x.Deliverable).WithMany(x => x.Files).HasForeignKey(x => x.DeliverableId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Payment>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Provider).HasMaxLength(50).IsRequired();
            e.Property(x => x.ProviderPaymentId).HasMaxLength(200).IsRequired();
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.HasIndex(x => x.DealId).IsUnique();
            e.HasOne(x => x.Deal).WithOne(x => x.Payment).HasForeignKey<Payment>(x => x.DealId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Conversation>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.DealId).IsUnique();
            e.HasOne(x => x.Deal).WithOne(x => x.Conversation).HasForeignKey<Conversation>(x => x.DealId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Message>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Text).HasMaxLength(4000).IsRequired();
            e.HasIndex(x => new { x.ConversationId, x.CreatedAt });
            e.HasOne(x => x.Conversation).WithMany(x => x.Messages).HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Sender).WithMany().HasForeignKey(x => x.SenderId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Review>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Comment).HasMaxLength(2000).IsRequired();
            e.HasIndex(x => new { x.DealId, x.AuthorId }).IsUnique();
            e.HasIndex(x => x.RecipientId);
            e.HasOne(x => x.Deal).WithMany(x => x.Reviews).HasForeignKey(x => x.DealId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Author).WithMany().HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Recipient).WithMany().HasForeignKey(x => x.RecipientId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Service>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).HasMaxLength(10000).IsRequired();
            e.Property(x => x.Price).HasPrecision(18, 2);
            e.HasIndex(x => new { x.Status, x.CategoryId });
            e.HasOne(x => x.Seller).WithMany().HasForeignKey(x => x.SellerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Notification>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Type).HasMaxLength(50).IsRequired();
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.Property(x => x.Body).HasMaxLength(1000).IsRequired();
            e.Property(x => x.LinkUrl).HasMaxLength(500);
            e.HasIndex(x => new { x.UserId, x.IsRead, x.CreatedAt });
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AuditLog>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Action).HasMaxLength(100).IsRequired();
            e.Property(x => x.EntityType).HasMaxLength(100).IsRequired();
            e.HasIndex(x => new { x.EntityType, x.EntityId });
            e.HasIndex(x => x.CreatedAt);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Properties.Any(p => p.Metadata.Name == "CreatedAt"))
                    entry.Property("CreatedAt").CurrentValue = utcNow;
                if (entry.Properties.Any(p => p.Metadata.Name == "UpdatedAt"))
                    entry.Property("UpdatedAt").CurrentValue = utcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                if (entry.Properties.Any(p => p.Metadata.Name == "UpdatedAt"))
                    entry.Property("UpdatedAt").CurrentValue = utcNow;
                if (entry.Properties.Any(p => p.Metadata.Name == "RowVersion")
                    && entry.Property("RowVersion").Metadata.ClrType == typeof(long)
                    && !entry.Property("RowVersion").IsModified)
                {
                    var original = entry.Property("RowVersion").OriginalValue is long value ? value : 0L;
                    entry.Property("RowVersion").CurrentValue = original + 1;
                }
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
