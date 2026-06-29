using System.ComponentModel.DataAnnotations;
using FryzjerBooking.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FryzjerBooking.Pages.Account;

public sealed class RegisterModel(
    UserManager<UzytkownikAplikacji> userManager,
    SignInManager<UzytkownikAplikacji> signInManager)
    : PageModel
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

        var user = new UzytkownikAplikacji
        {
            UserName = Input.Email,
            Email = Input.Email,
            ImieINazwisko = Input.ImieINazwisko,
            PhoneNumber = Input.Telefon
        };

        var result = await userManager.CreateAsync(user, Input.Haslo);
        if (result.Succeeded)
        {
            await signInManager.SignInAsync(user, isPersistent: true);
            return LocalRedirect(returnUrl);
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return Page();
    }

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Podaj imię i nazwisko.")]
        [StringLength(160)]
        public string ImieINazwisko { get; set; } = string.Empty;

        [Required(ErrorMessage = "Podaj e-mail.")]
        [EmailAddress(ErrorMessage = "Podaj poprawny adres e-mail.")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Podaj poprawny numer telefonu.")]
        public string? Telefon { get; set; }

        [Required(ErrorMessage = "Podaj hasło.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Hasło musi mieć co najmniej 8 znaków.")]
        [DataType(DataType.Password)]
        public string Haslo { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Compare(nameof(Haslo), ErrorMessage = "Hasła muszą być takie same.")]
        public string PotwierdzHaslo { get; set; } = string.Empty;
    }
}
