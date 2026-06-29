using System.ComponentModel.DataAnnotations;
using FryzjerBooking.Models;
using Microsoft.AspNetCore.Identity;

namespace FryzjerBooking.PunktyKoncowe;

public static class PunktyKoncoweKonta
{
    public static IEndpointRouteBuilder MapujPunktyKoncoweKonta(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/konto").WithTags("Konto");

        group.MapGet("/ja", (HttpContext httpContext) =>
        {
            if (httpContext.User.Identity?.IsAuthenticated != true)
            {
                return Results.Unauthorized();
            }

            return Results.Ok(new
            {
                email = httpContext.User.Identity.Name,
                uzytkownikId = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            });
        });

        group.MapPost("/rejestracja", async (
            DaneRejestracji request,
            UserManager<UzytkownikAplikacji> userManager,
            SignInManager<UzytkownikAplikacji> signInManager) =>
        {
            var user = new UzytkownikAplikacji
            {
                UserName = request.Email,
                Email = request.Email,
                ImieINazwisko = request.ImieINazwisko,
                PhoneNumber = request.Telefon
            };

            var result = await userManager.CreateAsync(user, request.Haslo);
            if (!result.Succeeded)
            {
                return Results.BadRequest(result.Errors.Select(error => error.Description));
            }

            await signInManager.SignInAsync(user, isPersistent: true);
            return Results.Created("/api/konto/ja", new { user.Id, user.Email, user.ImieINazwisko });
        });

        group.MapPost("/logowanie", async (
            DaneLogowania request,
            SignInManager<UzytkownikAplikacji> signInManager) =>
        {
            var result = await signInManager.PasswordSignInAsync(
                request.Email,
                request.Haslo,
                request.ZapamietajMnie,
                lockoutOnFailure: false);

            return result.Succeeded
                ? Results.Ok()
                : Results.BadRequest(new[] { "Nieprawidłowy e-mail lub hasło." });
        });

        group.MapPost("/wylogowanie", async (SignInManager<UzytkownikAplikacji> signInManager) =>
        {
            await signInManager.SignOutAsync();
            return Results.Ok();
        }).RequireAuthorization();

        return app;
    }

    public sealed record DaneRejestracji(
        [Required, EmailAddress] string Email,
        [Required, MinLength(8)] string Haslo,
        [Required, StringLength(160)] string ImieINazwisko,
        [Phone] string? Telefon);

    public sealed record DaneLogowania(
        [Required, EmailAddress] string Email,
        [Required] string Haslo,
        bool ZapamietajMnie);
}
