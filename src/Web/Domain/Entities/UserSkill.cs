namespace Web.Domain.Entities;

public class UserSkill
{
    public Guid UserId { get; set; }
    public Guid SkillId { get; set; }
    public Profile Profile { get; set; } = null!;
    public Skill Skill { get; set; } = null!;
}
