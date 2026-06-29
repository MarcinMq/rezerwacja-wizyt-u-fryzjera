using FryzjerBooking.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FryzjerBooking.Data;

public sealed class KontekstAplikacji(DbContextOptions<KontekstAplikacji> options)
    : IdentityDbContext<UzytkownikAplikacji>(options)
{
    public DbSet<Fryzjer> Fryzjerzy => Set<Fryzjer>();

    public DbSet<UslugaFryzjerska> UslugiFryzjerskie => Set<UslugaFryzjerska>();

    public DbSet<Wizyta> Wizyty => Set<Wizyta>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<UzytkownikAplikacji>(uzytkownik =>
        {
            uzytkownik.Property(user => user.ImieINazwisko)
                .HasColumnName("FullName")
                .HasMaxLength(160);

            uzytkownik.Property(user => user.Utworzono)
                .HasColumnName("CreatedAt");
        });

        builder.Entity<Fryzjer>(fryzjer =>
        {
            fryzjer.ToTable("Barbers");

            fryzjer.Property(item => item.ImieINazwisko)
                .HasColumnName("FullName")
                .HasMaxLength(160);

            fryzjer.Property(item => item.Opis)
                .HasColumnName("Bio")
                .HasMaxLength(500);

            fryzjer.Property(item => item.CzyAktywny)
                .HasColumnName("IsActive");
        });

        builder.Entity<UslugaFryzjerska>(usluga =>
        {
            usluga.ToTable("BarberServices");

            usluga.Property(item => item.Nazwa)
                .HasColumnName("Name")
                .HasMaxLength(120);

            usluga.Property(item => item.Opis)
                .HasColumnName("Description")
                .HasMaxLength(500);

            usluga.Property(item => item.CzasTrwaniaMinuty)
                .HasColumnName("DurationMinutes");

            usluga.Property(item => item.Cena)
                .HasColumnName("Price")
                .HasConversion<double>();

            usluga.Property(item => item.CzyAktywny)
                .HasColumnName("IsActive");
        });

        builder.Entity<Wizyta>(wizyta =>
        {
            wizyta.ToTable("Appointments");

            wizyta.Property(item => item.KlientId)
                .HasColumnName("CustomerId");

            wizyta.Property(item => item.FryzjerId)
                .HasColumnName("BarberId");

            wizyta.Property(item => item.UslugaId)
                .HasColumnName("ServiceId");

            wizyta.Property(item => item.RozpoczynaSie)
                .HasColumnName("StartsAt");

            wizyta.Property(item => item.KonczySie)
                .HasColumnName("EndsAt");

            wizyta.Property(item => item.Notatka)
                .HasColumnName("Notes")
                .HasMaxLength(500);

            wizyta.Property(item => item.Utworzono)
                .HasColumnName("CreatedAt");
        });

        builder.Entity<Wizyta>()
            .HasOne(wizyta => wizyta.Klient)
            .WithMany(user => user.Wizyty)
            .HasForeignKey(wizyta => wizyta.KlientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Wizyta>()
            .HasOne(wizyta => wizyta.Fryzjer)
            .WithMany(fryzjer => fryzjer.Wizyty)
            .HasForeignKey(wizyta => wizyta.FryzjerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Wizyta>()
            .HasOne(wizyta => wizyta.Usluga)
            .WithMany(usluga => usluga.Wizyty)
            .HasForeignKey(wizyta => wizyta.UslugaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Wizyta>()
            .HasIndex(wizyta => new { wizyta.FryzjerId, wizyta.RozpoczynaSie });
    }
}
