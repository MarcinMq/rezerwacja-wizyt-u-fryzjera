using FryzjerBooking.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FryzjerBooking.Pages.Account;

[IgnoreAntiforgeryToken]
public sealed class LogoutModel(SignInManager<UzytkownikAplikacji> signInManager) : PageModel
{
    public async Task<IActionResult> OnPostAsync()
    {
        await signInManager.SignOutAsync();
        return LocalRedirect("~/");
    }
}
