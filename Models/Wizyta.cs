namespace FryzjerBooking.Models;

public sealed class Wizyta
{
    public int Id { get; set; }

    public string KlientId { get; set; } = string.Empty;

    public UzytkownikAplikacji Klient { get; set; } = null!;

    public int FryzjerId { get; set; }

    public Fryzjer Fryzjer { get; set; } = null!;

    public int UslugaId { get; set; }

    public UslugaFryzjerska Usluga { get; set; } = null!;

    public DateTimeOffset RozpoczynaSie { get; set; }

    public DateTimeOffset KonczySie { get; set; }

    public StatusWizyty Status { get; set; } = StatusWizyty.Oczekujaca;

    public string? Notatka { get; set; }

    public DateTimeOffset Utworzono { get; set; } = DateTimeOffset.UtcNow;
}
