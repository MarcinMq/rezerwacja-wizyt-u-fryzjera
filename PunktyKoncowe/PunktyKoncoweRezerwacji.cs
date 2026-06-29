using System.ComponentModel.DataAnnotations;
using FryzjerBooking.Models;
using FryzjerBooking.Services;
using Microsoft.AspNetCore.Mvc;

namespace FryzjerBooking.PunktyKoncowe;

public static class PunktyKoncoweRezerwacji
{
    public static IEndpointRouteBuilder MapujPunktyKoncoweRezerwacji(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api").WithTags("Rezerwacje");

        group.MapGet("/uslugi", async (SerwisRezerwacji rezerwacje) =>
            Results.Ok(await rezerwacje.PobierzAktywneUslugiAsync()));

        group.MapGet("/fryzjerzy", async (SerwisRezerwacji rezerwacje) =>
            Results.Ok(await rezerwacje.PobierzAktywnychFryzjerowAsync()));

        group.MapGet("/dostepnosc", async (
            [FromQuery] int fryzjerId,
            [FromQuery] int uslugaId,
            [FromQuery] DateOnly data,
            SerwisRezerwacji rezerwacje) =>
        {
            var terminy = await rezerwacje.PobierzDostepneTerminyAsync(fryzjerId, uslugaId, data);
            return Results.Ok(terminy);
        });

        group.MapGet("/wizyty/moje", async (HttpContext httpContext, SerwisRezerwacji rezerwacje) =>
        {
            var uzytkownikId = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return string.IsNullOrWhiteSpace(uzytkownikId)
                ? Results.Unauthorized()
                : Results.Ok(await rezerwacje.PobierzWizytyUzytkownikaAsync(uzytkownikId));
        }).RequireAuthorization();

        group.MapPost("/wizyty", async (
            DaneNowejWizyty request,
            HttpContext httpContext,
            SerwisRezerwacji rezerwacje) =>
        {
            var uzytkownikId = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(uzytkownikId))
            {
                return Results.Unauthorized();
            }

            var result = await rezerwacje.ZarezerwujAsync(
                uzytkownikId,
                request.FryzjerId,
                request.UslugaId,
                request.RozpoczynaSie,
                request.Notatka);

            return result.Powodzenie
                ? Results.Created("/api/wizyty/moje", null)
                : Results.BadRequest(new[] { result.Blad });
        }).RequireAuthorization();

        group.MapPatch("/wizyty/{wizytaId:int}/odwolaj", async (
            int wizytaId,
            HttpContext httpContext,
            SerwisRezerwacji rezerwacje) =>
        {
            var uzytkownikId = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(uzytkownikId))
            {
                return Results.Unauthorized();
            }

            var result = await rezerwacje.OdwolajAsync(uzytkownikId, wizytaId);
            return result.Powodzenie
                ? Results.NoContent()
                : Results.BadRequest(new[] { result.Blad });
        }).RequireAuthorization();

        return app;
    }

    public sealed record DaneNowejWizyty(
        [Range(1, int.MaxValue)] int FryzjerId,
        [Range(1, int.MaxValue)] int UslugaId,
        DateTimeOffset RozpoczynaSie,
        [StringLength(500)] string? Notatka);
}
