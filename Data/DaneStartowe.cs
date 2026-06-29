using FryzjerBooking.Models;
using Microsoft.EntityFrameworkCore;

namespace FryzjerBooking.Data;

public static class DaneStartowe
{
    public static async Task SeedAsync(KontekstAplikacji db)
    {
        if (!await db.UslugiFryzjerskie.AnyAsync())
        {
            db.UslugiFryzjerskie.AddRange(
                new UslugaFryzjerska
                {
                    Nazwa = "Strzyżenie męskie",
                    Opis = "Klasyczne lub nowoczesne cięcie z modelowaniem.",
                    CzasTrwaniaMinuty = 45,
                    Cena = 80
                },
                new UslugaFryzjerska
                {
                    Nazwa = "Broda",
                    Opis = "Trymowanie, kontur i pielęgnacja brody.",
                    CzasTrwaniaMinuty = 30,
                    Cena = 55
                },
                new UslugaFryzjerska
                {
                    Nazwa = "Combo",
                    Opis = "Strzyżenie włosów i pełna pielęgnacja brody.",
                    CzasTrwaniaMinuty = 75,
                    Cena = 125
                });
        }

        if (!await db.Fryzjerzy.AnyAsync())
        {
            db.Fryzjerzy.AddRange(
                new Fryzjer
                {
                    ImieINazwisko = "Adam Nowak",
                    Opis = "Specjalista od klasycznych cięć i krótkich fryzur."
                },
                new Fryzjer
                {
                    ImieINazwisko = "Michał Zieliński",
                    Opis = "Pracuje z fade, teksturą i dłuższymi formami."
                },
                new Fryzjer
                {
                    ImieINazwisko = "Karolina Wójcik",
                    Opis = "Łączy precyzyjne cięcie z pielęgnacją brody."
                });
        }

        await db.SaveChangesAsync();
    }
}
