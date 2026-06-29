using Microsoft.AspNetCore.Identity;

namespace FryzjerBooking.Models;

public sealed class UzytkownikAplikacji : IdentityUser
{
    public string ImieINazwisko { get; set; } = string.Empty;

    public DateTimeOffset Utworzono { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<Wizyta> Wizyty { get; set; } = new List<Wizyta>();
}
