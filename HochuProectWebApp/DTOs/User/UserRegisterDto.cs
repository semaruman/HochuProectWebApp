using System.ComponentModel.DataAnnotations;

namespace HochuProectWebApp.DTOs.User
{
    public class UserRegisterDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [MinLength(6)]
        public string Password { get; set; }

        [Required, MaxLength(50)]
        public string Name { get; set; }
    }
}
