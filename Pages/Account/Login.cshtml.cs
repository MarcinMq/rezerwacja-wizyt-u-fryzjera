using System.ComponentModel.DataAnnotations;
using FryzjerBooking.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FryzjerBooking.Pages.Account;

public sealed class LoginModel(SignInManager<UzytkownikAplikacji> signInManager) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await signInManager.PasswordSignInAsync(
            Input.Email,
            Input.Haslo,
            Input.ZapamietajMnie,
            lockoutOnFailure: false);

        if (result.Succeeded)
        {
            return LocalRedirect(returnUrl);
        }

        ModelState.AddModelError(string.Empty, "Nieprawidłowy e-mail lub hasło.");
        return Page();
    }

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Podaj e-mail.")]
        [EmailAddress(ErrorMessage = "Podaj poprawny adres e-mail.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Podaj hasło.")]
        [DataType(DataType.Password)]
        public string Haslo { get; set; } = string.Empty;

        public bool ZapamietajMnie { get; set; }
    }
}
