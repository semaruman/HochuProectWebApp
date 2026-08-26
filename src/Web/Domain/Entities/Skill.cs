namespace Web.Domain.Entities;

public class Skill
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<UserSkill> UserSkills { get; set; } = new List<UserSkill>();
}
