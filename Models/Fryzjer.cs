namespace FryzjerBooking.Models;

public sealed class Fryzjer
{
    public int Id { get; set; }

    public string ImieINazwisko { get; set; } = string.Empty;

    public string Opis { get; set; } = string.Empty;

    public bool CzyAktywny { get; set; } = true;

    public ICollection<Wizyta> Wizyty { get; set; } = new List<Wizyta>();
}
