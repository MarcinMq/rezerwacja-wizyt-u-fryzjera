# Fryzjer Booking

Nowoczesna aplikacja webowa do rezerwacji wizyt u fryzjera. Projekt łączy backend ASP.NET Core, interfejs Blazor Server, logowanie przez ASP.NET Core Identity oraz lokalną bazę SQLite.

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![Blazor](https://img.shields.io/badge/Blazor-Server-6D429C?style=for-the-badge&logo=blazor&logoColor=white)
![SQLite](https://img.shields.io/badge/SQLite-database-003B57?style=for-the-badge&logo=sqlite&logoColor=white)

## Opis

Fryzjer Booking pozwala klientowi założyć konto, zalogować się, wybrać usługę, fryzjera oraz wolny termin wizyty. Aplikacja pilnuje kolizji terminów, pokazuje listę wizyt zalogowanego klienta i pozwala odwołać przyszłą wizytę.

Kod aplikacji został częściowo nazwany po polsku, między innymi modele, serwisy i endpointy API. Tam, gdzie wymaga tego .NET lub Identity, pozostają nazwy frameworkowe po angielsku.

## Funkcje

- rejestracja i logowanie klientów,
- uwierzytelnianie przez ASP.NET Core Identity,
- lista usług fryzjerskich,
- lista aktywnych fryzjerów,
- wybór dnia i dostępnej godziny wizyty,
- blokowanie zajętych terminów,
- widok „Moje wizyty” dla zalogowanego użytkownika,
- odwoływanie przyszłych wizyt,
- polskie endpointy API pod `/api`,
- lokalna baza SQLite tworzona automatycznie przy starcie.

## Technologie

- .NET 10
- ASP.NET Core
- Blazor Server
- ASP.NET Core Identity
- Entity Framework Core
- SQLite
- Razor Pages
- Minimal API

## Wymagania

- .NET SDK 10.0 lub nowszy

Sprawdzenie wersji:

```bash
dotnet --version
```

## Uruchomienie

Przejdź do folderu projektu:

```bash
cd "/Users/marcinmq/Documents/Rezerwacja wizyt u fryzjera"
```

Przywróć paczki i uruchom aplikację:

```bash
dotnet restore
dotnet run
```

Domyślne adresy z profilu startowego:

- `https://localhost:7246`
- `http://localhost:5246`

Możesz też wymusić lokalny adres HTTP:

```bash
dotnet run --no-launch-profile --urls http://127.0.0.1:5107
```

Po pierwszym starcie aplikacja utworzy plik bazy:

```text
barber-booking.db
```

Baza dostaje też przykładowe usługi i fryzjerów.

## Konto użytkownika

Aplikacja nie ma domyślnego konta administratora ani klienta. Konto należy założyć przez stronę:

```text
/konto/rejestracja
```

Hasło musi mieć co najmniej 8 znaków, małą literę i cyfrę.

## Endpointy API

### Konto

| Metoda | Endpoint | Opis |
| --- | --- | --- |
| `GET` | `/api/konto/ja` | Dane aktualnie zalogowanego użytkownika |
| `POST` | `/api/konto/rejestracja` | Rejestracja konta |
| `POST` | `/api/konto/logowanie` | Logowanie |
| `POST` | `/api/konto/wylogowanie` | Wylogowanie |

### Rezerwacje

| Metoda | Endpoint | Opis |
| --- | --- | --- |
| `GET` | `/api/uslugi` | Lista aktywnych usług |
| `GET` | `/api/fryzjerzy` | Lista aktywnych fryzjerów |
| `GET` | `/api/dostepnosc` | Dostępne terminy dla fryzjera, usługi i dnia |
| `GET` | `/api/wizyty/moje` | Wizyty zalogowanego użytkownika |
| `POST` | `/api/wizyty` | Utworzenie rezerwacji |
| `PATCH` | `/api/wizyty/{wizytaId}/odwolaj` | Odwołanie wizyty |

Przykład sprawdzenia dostępności:

```bash
curl "http://127.0.0.1:5107/api/dostepnosc?fryzjerId=1&uslugaId=1&data=2026-06-30"
```

Przykład rejestracji przez API:

```bash
curl -X POST "http://127.0.0.1:5107/api/konto/rejestracja" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "jan@example.com",
    "haslo": "Test1234",
    "imieINazwisko": "Jan Kowalski",
    "telefon": "500600700"
  }'
```

Przykład utworzenia wizyty:

```bash
curl -X POST "http://127.0.0.1:5107/api/wizyty" \
  -H "Content-Type: application/json" \
  -d '{
    "fryzjerId": 1,
    "uslugaId": 1,
    "rozpoczynaSie": "2026-06-30T10:00:00+02:00",
    "notatka": "Proszę krótkie boki"
  }'
```

## Struktura projektu

```text
.
├── Components/          # Widoki Blazor
├── Data/                # Kontekst EF Core i dane startowe
├── Models/              # Modele domenowe
├── Pages/Account/       # Logowanie, rejestracja i wylogowanie
├── PunktyKoncowe/       # Minimal API
├── Services/            # Logika rezerwacji
├── wwwroot/css/         # Style aplikacji
├── Program.cs           # Konfiguracja aplikacji
└── BarberBooking.csproj # Plik projektu .NET
```

## Ważne pliki

- `Data/KontekstAplikacji.cs` - konfiguracja Entity Framework Core i mapowanie bazy,
- `Services/SerwisRezerwacji.cs` - logika rezerwacji, dostępności i odwoływania wizyt,
- `PunktyKoncowe/PunktyKoncoweRezerwacji.cs` - endpointy API dla usług, fryzjerów i wizyt,
- `PunktyKoncowe/PunktyKoncoweKonta.cs` - endpointy API dla konta,
- `Components/Pages/BookAppointment.razor` - formularz rezerwacji,
- `Components/Pages/MyAppointments.razor` - widok wizyt użytkownika.

## Uwagi developerskie

Projekt używa Minimal API zamiast klasycznych kontrolerów MVC. Dlatego folder `Controllers` nie jest potrzebny. Endpointy znajdują się w folderze `PunktyKoncowe`.

Modele i metody aplikacyjne mają polskie nazwy bez polskich znaków, np. `UslugaFryzjerska`, `SerwisRezerwacji`, `PobierzDostepneTerminyAsync`. To ułatwia pracę z narzędziami i zachowuje kompatybilność z C#.

## Znane ostrzeżenie

Podczas `restore` lub `build` może pojawić się ostrzeżenie NuGet dotyczące transytywnej paczki SQLite:

```text
NU1903: SQLitePCLRaw.lib.e_sqlite3 2.1.11
```

Projekt mimo tego buduje się poprawnie.

## Build

```bash
dotnet build
```

## Status

Projekt jest gotowy jako lokalna aplikacja demonstracyjna do rezerwacji wizyt u fryzjera.
