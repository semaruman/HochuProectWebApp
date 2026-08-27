using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Web.Domain.Entities;
using Web.Domain.ValueObjects;

namespace Web.Infrastructure.Persistence;

public static class DemoDataSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<AppDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        if (await db.Projects.AnyAsync())
            return;

        var buyer = await EnsureUserAsync(userManager, db, "buyer@demo.local", "Demo1234!", "Демо Заказчик");
        var seller = await EnsureUserAsync(userManager, db, "seller@demo.local", "Demo1234!", "Демо Инженер");

        var category = await db.Categories.FirstAsync(c => c.Slug == "3d-modeling");
        var calc = await db.Categories.FirstAsync(c => c.Slug == "engineering-calculations");
        var utcNow = DateTime.UtcNow;

        var project = Project.Create(
            buyer.Id,
            category.Id,
            "3D-модель корпуса промышленного оборудования",
            "Нужно разработать параметрическую 3D-модель корпуса по чертежам. Форматы: STEP, SolidWorks. Учесть посадочные места и допуски.",
            Money.Rub(45000),
            DateOnly.FromDateTime(utcNow.AddDays(21)),
            utcNow);
        project.Publish(utcNow);
        db.Projects.Add(project);

        db.Bids.Add(Bid.Place(
            project,
            seller.Id,
            Money.Rub(42000),
            12,
            "Сделаю модель в SolidWorks, передам STEP/SLDPRT и краткий отчёт по допускам.",
            utcNow));

        var service = Service.Create(
            seller.Id,
            calc.Id,
            "Прочностной расчёт детали (FEM)",
            "Статический расчёт детали/узла в ANSYS или аналоге. Результат: отчёт PDF + рекомендации по изменению геометрии.",
            Money.Rub(15000),
            5,
            utcNow);
        service.Publish(utcNow);
        db.Services.Add(service);

        await db.SaveChangesAsync();
    }

    private static async Task<ApplicationUser> EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        AppDbContext db,
        string email,
        string password,
        string displayName)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
            return existing;

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email,
            EmailConfirmed = true
        };
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));

        db.Profiles.Add(Profile.Create(user.Id, displayName, DateTime.UtcNow, "Демо-аккаунт для локальной разработки."));
        await db.SaveChangesAsync();
        return user;
    }
}
