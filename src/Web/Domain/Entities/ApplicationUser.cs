using Microsoft.AspNetCore.Identity;

namespace Web.Domain.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public Profile? Profile { get; set; }
    public bool IsBlocked { get; set; }
    public DateTime? TermsAcceptedAt { get; set; }
    public DateTime? PrivacyPolicyAcceptedAt { get; set; }
}
