using Microsoft.AspNetCore.Identity;

namespace Web.Domain.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public Profile? Profile { get; set; }
}
