using Web.Domain.Entities;
using Web.Infrastructure.Persistence;

namespace Web.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (!db.Categories.Any())
        {
            var categories = new (string Name, string Slug)[]
            {
                ("CAD", "cad"),
                ("3D Modeling", "3d-modeling"),
                ("Engineering Calculations", "engineering-calculations"),
                ("Mechanical Engineering", "mechanical-engineering"),
                ("Electrical Engineering", "electrical-engineering"),
                ("PCB", "pcb"),
                ("BIM", "bim"),
                ("Reverse Engineering", "reverse-engineering"),
                ("Technical Documentation", "technical-documentation"),
                ("Automation", "automation")
            };

            foreach (var (name, slug) in categories)
            {
                db.Categories.Add(new Category
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Slug = slug
                });
            }
        }

        if (!db.Skills.Any())
        {
            var skills = new[]
            {
                "SolidWorks", "AutoCAD", "Inventor", "CATIA", "Kompas-3D",
                "ANSYS", "MATLAB", "Altium Designer", "Revit", "Fusion 360"
            };
            foreach (var skill in skills)
            {
                db.Skills.Add(new Skill { Id = Guid.NewGuid(), Name = skill });
            }
        }

        await db.SaveChangesAsync();
    }
}
