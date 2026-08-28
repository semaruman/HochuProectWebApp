using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Web.Common.Results;
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

        var budget = Money.Rub(45000);
        if (budget.IsFailure) return;

        var projectResult = Project.Create(
            buyer.Id,
            category.Id,
            "3D-модель корпуса промышленного оборудования",
            "Нужно разработать параметрическую 3D-модель корпуса по чертежам. Форматы: STEP, SolidWorks. Учесть посадочные места и допуски.",
            budget.Value,
            DateOnly.FromDateTime(utcNow.AddDays(21)),
            utcNow);
        if (projectResult.IsFailure) return;

        var project = projectResult.Value;
        if (project.Publish(utcNow).IsFailure) return;
        db.Projects.Add(project);

        var bidPrice = Money.Rub(42000);
        if (bidPrice.IsFailure) return;

        var bidResult = Bid.Place(
            project,
            seller.Id,
            bidPrice.Value,
            12,
            "Сделаю модель в SolidWorks, передам STEP/SLDPRT и краткий отчёт по допускам.",
            utcNow);
        if (bidResult.IsFailure) return;
        db.Bids.Add(bidResult.Value);

        var servicePrice = Money.Rub(15000);
        if (servicePrice.IsFailure) return;

        var serviceResult = Service.Create(
            seller.Id,
            calc.Id,
            "Прочностной расчёт детали (FEM)",
            "Статический расчёт детали/узла в ANSYS или аналоге. Результат: отчёт PDF + рекомендации по изменению геометрии.",
            servicePrice.Value,
            5,
            utcNow);
        if (serviceResult.IsFailure) return;

        var service = serviceResult.Value;
        if (service.Publish(utcNow).IsFailure) return;
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
            return user;

        var profileResult = Profile.Create(user.Id, displayName, DateTime.UtcNow, "Демо-аккаунт для локальной разработки.");
        if (profileResult.IsSuccess)
            db.Profiles.Add(profileResult.Value);

        await db.SaveChangesAsync();
        return user;
    }
}
