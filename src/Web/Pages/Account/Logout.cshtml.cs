using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Web.Domain.Entities;

namespace Web.Pages.Account;

public class LogoutModel(SignInManager<ApplicationUser> signInManager) : PageModel
{
    public async Task<IActionResult> OnPostAsync()
    {
        await signInManager.SignOutAsync();
        return RedirectToPage("/Index");
    }
}
