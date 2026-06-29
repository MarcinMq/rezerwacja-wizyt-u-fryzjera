namespace FryzjerBooking.Models;

public sealed class UslugaFryzjerska
{
    public int Id { get; set; }

    public string Nazwa { get; set; } = string.Empty;

    public string Opis { get; set; } = string.Empty;

    public int CzasTrwaniaMinuty { get; set; }

    public decimal Cena { get; set; }

    public bool CzyAktywny { get; set; } = true;

    public ICollection<Wizyta> Wizyty { get; set; } = new List<Wizyta>();
}
