using System.ComponentModel.DataAnnotations;

namespace HochuProectWebApp.DTOs.User
{
    public class UserLoginDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [MinLength(6)]
        public string Password { get; set; }
    }
}
