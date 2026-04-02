using Microsoft.EntityFrameworkCore;
using HochuProectWebApp.Models;

namespace HochuProectWebApp.Data
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }

        public DbSet<Advertisement> Advertisements { get; set; }

        public DbSet<Category> Categories { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddUserSecrets<ApplicationDbContext>()
                .Build();

            string connectionString = config.GetConnectionString("PostgresConnection");

            optionsBuilder.UseNpgsql(connectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Advertisement>()
                .HasOne(a => a.User)
                .WithMany(u => u.Advertisements)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade); 

            modelBuilder.Entity<Advertisement>()
                .HasOne(a => a.Category)
                .WithMany(c => c.Advertisements)
                .HasForeignKey(a => a.CategoryId)
                .OnDelete(DeleteBehavior.Restrict); 

            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(e => e.Name).HasMaxLength(100);
                entity.Property(e => e.Email).HasMaxLength(100);
                entity.HasIndex(e => e.Email).IsUnique(); 
            });

            modelBuilder.Entity<Category>(entity =>
            {
                entity.Property(e => e.Name).HasMaxLength(50).IsRequired();
                entity.HasIndex(e => e.Name).IsUnique();
            });

            modelBuilder.Entity<Advertisement>(entity =>
            {
                entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(3000);
            });

            //найстройка сохранения списка фотографий для postgre sql
            modelBuilder.Entity<Advertisement>(entity =>
            {
                entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(3000);
            });
        }
    }
}
