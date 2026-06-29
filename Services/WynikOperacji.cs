namespace FryzjerBooking.Services;

public sealed record WynikOperacji(bool Powodzenie, string? Blad = null)
{
    public static WynikOperacji Sukces() => new(true);

    public static WynikOperacji Niepowodzenie(string blad) => new(false, blad);
}
