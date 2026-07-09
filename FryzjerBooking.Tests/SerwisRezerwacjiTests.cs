using FryzjerBooking.Data;
using FryzjerBooking.Models;
using FryzjerBooking.Services;
using Microsoft.EntityFrameworkCore;

namespace FryzjerBooking.Tests;

public sealed class SerwisRezerwacjiTests : IAsyncLifetime
{
    private readonly string sciezkaBazy = Path.Combine(
        Path.GetTempPath(),
        $"fryzjer-booking-tests-{Guid.NewGuid():N}.db");

    [Fact]
    public async Task ZarezerwujAsync_NiePozwalaNaPonownaRezerwacjeTegoSamegoTerminu()
    {
        var termin = PobierzPrzyszlyTermin();

        await using var pierwszyKontekst = UtworzKontekst();
        var pierwszyWynik = await new SerwisRezerwacji(pierwszyKontekst)
            .ZarezerwujAsync("klient-1", 1, 1, termin, null);

        await using var drugiKontekst = UtworzKontekst();
        var drugiWynik = await new SerwisRezerwacji(drugiKontekst)
            .ZarezerwujAsync("klient-2", 1, 1, termin, null);

        Assert.True(pierwszyWynik.Powodzenie);
        Assert.False(drugiWynik.Powodzenie);

        await using var kontekstWeryfikujacy = UtworzKontekst();
        Assert.Equal(1, await kontekstWeryfikujacy.Wizyty.CountAsync());
    }

    [Fact]
    public async Task ZarezerwujAsync_PrzyRownoleglychProbachTworzyTylkoJednaWizyte()
    {
        var termin = PobierzPrzyszlyTermin();

        await using var pierwszyKontekst = UtworzKontekst();
        await using var drugiKontekst = UtworzKontekst();
        var pierwszaUsluga = new SerwisRezerwacji(pierwszyKontekst);
        var drugaUsluga = new SerwisRezerwacji(drugiKontekst);
        using var bramkaStartu = new Barrier(2);

        var wyniki = await Task.WhenAll(
            Task.Run(async () =>
            {
                bramkaStartu.SignalAndWait();
                return await pierwszaUsluga.ZarezerwujAsync("klient-1", 1, 1, termin, null);
            }),
            Task.Run(async () =>
            {
                bramkaStartu.SignalAndWait();
                return await drugaUsluga.ZarezerwujAsync("klient-2", 1, 1, termin, null);
            }));

        Assert.Single(wyniki, wynik => wynik.Powodzenie);
        Assert.Single(wyniki, wynik => !wynik.Powodzenie);

        await using var kontekstWeryfikujacy = UtworzKontekst();
        Assert.Equal(1, await kontekstWeryfikujacy.Wizyty.CountAsync());
    }

    public async Task InitializeAsync()
    {
        await using var kontekst = UtworzKontekst();
        await kontekst.Database.EnsureCreatedAsync();

        kontekst.Users.AddRange(
            new UzytkownikAplikacji
            {
                Id = "klient-1",
                UserName = "klient1@example.com",
                Email = "klient1@example.com",
                ImieINazwisko = "Klient Pierwszy"
            },
            new UzytkownikAplikacji
            {
                Id = "klient-2",
                UserName = "klient2@example.com",
                Email = "klient2@example.com",
                ImieINazwisko = "Klient Drugi"
            });

        kontekst.Fryzjerzy.Add(new Fryzjer
        {
            ImieINazwisko = "Adam Nowak",
            Opis = "Testowy fryzjer"
        });

        kontekst.UslugiFryzjerskie.Add(new UslugaFryzjerska
        {
            Nazwa = "Strzyzenie testowe",
            Opis = "Usługa używana w testach",
            CzasTrwaniaMinuty = 30,
            Cena = 80m
        });

        await kontekst.SaveChangesAsync();
    }

    public Task DisposeAsync()
    {
        File.Delete(sciezkaBazy);
        File.Delete($"{sciezkaBazy}-shm");
        File.Delete($"{sciezkaBazy}-wal");
        return Task.CompletedTask;
    }

    private KontekstAplikacji UtworzKontekst()
    {
        var options = new DbContextOptionsBuilder<KontekstAplikacji>()
            .UseSqlite($"Data Source={sciezkaBazy};Default Timeout=10")
            .Options;

        return new KontekstAplikacji(options);
    }

    private static DateTimeOffset PobierzPrzyszlyTermin()
    {
        var data = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        while (data.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            data = data.AddDays(1);
        }

        return new DateTimeOffset(data.ToDateTime(new TimeOnly(10, 0), DateTimeKind.Local));
    }
}
