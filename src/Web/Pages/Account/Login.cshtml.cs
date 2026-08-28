using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Web.Domain.Entities;

namespace Web.Pages.Account;

public class LoginModel(SignInManager<ApplicationUser> signInManager) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();
    public string? Error { get; set; }

    public class InputModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;
        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        var result = await signInManager.PasswordSignInAsync(Input.Email, Input.Password, true, true);
        if (!result.Succeeded)
        {
            Error = "Неверный email или пароль.";
            return Page();
        }
        return RedirectToPage("/Index");
    }
}
