using FryzjerBooking.Data;
using FryzjerBooking.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FryzjerBooking.Services;

public sealed class SerwisRezerwacji(KontekstAplikacji db)
{
    public async Task<IReadOnlyList<UslugaFryzjerska>> PobierzAktywneUslugiAsync()
    {
        return await db.UslugiFryzjerskie
            .AsNoTracking()
            .Where(usluga => usluga.CzyAktywny)
            .OrderBy(usluga => usluga.Cena)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Fryzjer>> PobierzAktywnychFryzjerowAsync()
    {
        return await db.Fryzjerzy
            .AsNoTracking()
            .Where(fryzjer => fryzjer.CzyAktywny)
            .OrderBy(fryzjer => fryzjer.ImieINazwisko)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Wizyta>> PobierzWizytyUzytkownikaAsync(string uzytkownikId)
    {
        var wizyty = await db.Wizyty
            .AsNoTracking()
            .Include(wizyta => wizyta.Fryzjer)
            .Include(wizyta => wizyta.Usluga)
            .Where(wizyta => wizyta.KlientId == uzytkownikId)
            .ToListAsync();

        return wizyty
            .OrderByDescending(wizyta => wizyta.RozpoczynaSie)
            .ToList();
    }

    public async Task<IReadOnlyList<DateTimeOffset>> PobierzDostepneTerminyAsync(
        int fryzjerId,
        int uslugaId,
        DateOnly data)
    {
        var usluga = await db.UslugiFryzjerskie
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == uslugaId && item.CzyAktywny);

        var fryzjerIstnieje = await db.Fryzjerzy
            .AsNoTracking()
            .AnyAsync(item => item.Id == fryzjerId && item.CzyAktywny);

        if (usluga is null || !fryzjerIstnieje || data < DateOnly.FromDateTime(DateTime.Today))
        {
            return [];
        }

        var (otwarcie, zamkniecie) = PobierzGodzinyPracy(data);
        if (otwarcie is null || zamkniecie is null)
        {
            return [];
        }

        var poczatekDnia = data.ToDateTime(otwarcie.Value, DateTimeKind.Local);
        var koniecDnia = data.ToDateTime(zamkniecie.Value, DateTimeKind.Local);
        var poczatekZakresu = new DateTimeOffset(poczatekDnia).ToUniversalTime();
        var koniecZakresu = new DateTimeOffset(koniecDnia).ToUniversalTime();

        var istniejaceWizyty = await db.Wizyty
            .AsNoTracking()
            .Where(wizyta =>
                wizyta.FryzjerId == fryzjerId
                && wizyta.Status != StatusWizyty.Odwolana)
            .Select(wizyta => new { wizyta.RozpoczynaSie, wizyta.KonczySie })
            .ToListAsync();

        istniejaceWizyty = istniejaceWizyty
            .Where(wizyta =>
                wizyta.RozpoczynaSie < koniecZakresu
                && wizyta.KonczySie > poczatekZakresu)
            .ToList();

        var terminy = new List<DateTimeOffset>();
        var aktualnyTermin = new DateTimeOffset(poczatekDnia);
        var ostatniStart = new DateTimeOffset(koniecDnia.AddMinutes(-usluga.CzasTrwaniaMinuty));

        while (aktualnyTermin <= ostatniStart)
        {
            var koniecTerminu = aktualnyTermin.AddMinutes(usluga.CzasTrwaniaMinuty);
            var nachodziNaInnaWizyte = istniejaceWizyty.Any(wizyta =>
                wizyta.RozpoczynaSie < koniecTerminu.ToUniversalTime()
                && wizyta.KonczySie > aktualnyTermin.ToUniversalTime());

            if (!nachodziNaInnaWizyte && aktualnyTermin > DateTimeOffset.Now.AddMinutes(30))
            {
                terminy.Add(aktualnyTermin);
            }

            aktualnyTermin = aktualnyTermin.AddMinutes(30);
        }

        return terminy;
    }

    public async Task<WynikOperacji> ZarezerwujAsync(
        string uzytkownikId,
        int fryzjerId,
        int uslugaId,
        DateTimeOffset rozpoczynaSie,
        string? notatka)
    {
        await db.Database.OpenConnectionAsync();
        SqliteTransaction? transakcja = null;

        try
        {
            var polaczenie = db.Database.GetDbConnection() as SqliteConnection
                ?? throw new InvalidOperationException("Rezerwacje wymagają połączenia SQLite.");

            // BEGIN IMMEDIATE serializuje zapisy dla danego pliku SQLite przed sprawdzeniem dostępności.
            transakcja = polaczenie.BeginTransaction(deferred: false);
            db.Database.UseTransaction(transakcja);

            var usluga = await db.UslugiFryzjerskie
                .FirstOrDefaultAsync(item => item.Id == uslugaId && item.CzyAktywny);

            var fryzjerIstnieje = await db.Fryzjerzy
                .AnyAsync(item => item.Id == fryzjerId && item.CzyAktywny);

            if (usluga is null)
            {
                return WynikOperacji.Niepowodzenie("Wybrana usługa nie istnieje.");
            }

            if (!fryzjerIstnieje)
            {
                return WynikOperacji.Niepowodzenie("Wybrany fryzjer nie istnieje.");
            }

            if (rozpoczynaSie <= DateTimeOffset.Now.AddMinutes(30))
            {
                return WynikOperacji.Niepowodzenie("Termin musi być zaplanowany z co najmniej 30-minutowym wyprzedzeniem.");
            }

            var data = DateOnly.FromDateTime(rozpoczynaSie.LocalDateTime);
            var dostepneTerminy = await PobierzDostepneTerminyAsync(fryzjerId, uslugaId, data);
            if (!dostepneTerminy.Any(termin => termin == rozpoczynaSie))
            {
                return WynikOperacji.Niepowodzenie("Ten termin nie jest już dostępny.");
            }

            var wizyta = new Wizyta
            {
                KlientId = uzytkownikId,
                FryzjerId = fryzjerId,
                UslugaId = uslugaId,
                RozpoczynaSie = rozpoczynaSie.ToUniversalTime(),
                KonczySie = rozpoczynaSie.AddMinutes(usluga.CzasTrwaniaMinuty).ToUniversalTime(),
                Notatka = notatka?.Trim()
            };

            db.Wizyty.Add(wizyta);
            await db.SaveChangesAsync();
            await transakcja.CommitAsync();

            return WynikOperacji.Sukces();
        }
        catch (SqliteException exception) when (exception.SqliteErrorCode is 5 or 6)
        {
            db.ChangeTracker.Clear();
            return WynikOperacji.Niepowodzenie("Termin jest właśnie rezerwowany przez innego klienta. Wybierz inną godzinę.");
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            return WynikOperacji.Niepowodzenie("Ten termin nie jest już dostępny.");
        }
        finally
        {
            db.Database.UseTransaction(null);
            if (transakcja is not null)
            {
                await transakcja.DisposeAsync();
            }

            await db.Database.CloseConnectionAsync();
        }
    }

    public async Task<WynikOperacji> OdwolajAsync(string uzytkownikId, int wizytaId)
    {
        var wizyta = await db.Wizyty
            .FirstOrDefaultAsync(item => item.Id == wizytaId && item.KlientId == uzytkownikId);

        if (wizyta is null)
        {
            return WynikOperacji.Niepowodzenie("Nie znaleziono wizyty.");
        }

        if (wizyta.RozpoczynaSie <= DateTimeOffset.UtcNow)
        {
            return WynikOperacji.Niepowodzenie("Nie można odwołać wizyty, która już się rozpoczęła.");
        }

        wizyta.Status = StatusWizyty.Odwolana;
        await db.SaveChangesAsync();

        return WynikOperacji.Sukces();
    }

    private static (TimeOnly? Otwarcie, TimeOnly? Zamkniecie) PobierzGodzinyPracy(DateOnly data)
    {
        return data.DayOfWeek switch
        {
            DayOfWeek.Sunday => (null, null),
            DayOfWeek.Saturday => (new TimeOnly(10, 0), new TimeOnly(14, 0)),
            _ => (new TimeOnly(9, 0), new TimeOnly(17, 0))
        };
    }
}
