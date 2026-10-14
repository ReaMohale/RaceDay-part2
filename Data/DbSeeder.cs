using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Data;

/// <summary>
/// Seeds demo data through Entity Framework (development only, and only into an empty database).
/// Unlike the placeholder hashes in docs/raceday_schema.sql, these accounts can really log in.
/// Demo password for every account: Passw0rd!123
/// </summary>
public static class DbSeeder
{
    public const string DemoPassword = "Passw0rd!123";

    public static void Seed(RaceDayDbContext db, IPasswordService passwords)
    {
        if (db.Organisers.Any() || db.Participants.Any())
            return;

        var hash = passwords.Hash(DemoPassword);

        var thandiwe = new Organiser { FullName = "Thandiwe Nkosi", Email = "thandiwe.nkosi@raceday.co.za", PasswordHash = hash, PhoneNumber = "0821234567" };
        var johan = new Organiser { FullName = "Johan van der Merwe", Email = "johan.vdm@raceday.co.za", PasswordHash = hash, PhoneNumber = "0837654321" };

        var lerato = new Participant { FullName = "Lerato Mokoena", Email = "lerato.mokoena@gmail.com", PasswordHash = hash, PhoneNumber = "0731112222" };
        var sipho = new Participant { FullName = "Sipho Dlamini", Email = "sipho.dlamini@gmail.com", PasswordHash = hash, PhoneNumber = "0743334444" };

        var riverRun = new Event { Organiser = thandiwe, EventName = "Standerton River Run", Description = "A scenic riverside road running event for all levels.", EventDate = new DateOnly(2026, 11, 8), Location = "Standerton, Mpumalanga", EventType = "Run", DistanceKm = 21.10m };
        var charityCycle = new Event { Organiser = thandiwe, EventName = "Highveld Charity Cycle", Description = "Community cycling event raising funds for local schools.", EventDate = new DateOnly(2026, 8, 16), Location = "Standerton, Mpumalanga", EventType = "Cycle", DistanceKm = 80.00m };
        var parkRun = new Event { Organiser = johan, EventName = "Johannesburg City Park Run", Description = "A family-friendly park run through the city centre.", EventDate = new DateOnly(2026, 9, 27), Location = "Johannesburg, Gauteng", EventType = "Run", DistanceKm = 10.00m };

        var run10 = new Category { Event = riverRun, CategoryName = "10km Run", DistanceKm = 10.00m, EntryFee = 150m, MaxParticipants = 300 };
        var half = new Category { Event = riverRun, CategoryName = "21km Half Marathon", DistanceKm = 21.10m, EntryFee = 250m, MaxParticipants = 200 };
        var cycle40 = new Category { Event = charityCycle, CategoryName = "40km Road Cycle", DistanceKm = 40.00m, EntryFee = 200m, MaxParticipants = 150 };
        var cycle80 = new Category { Event = charityCycle, CategoryName = "80km Road Cycle", DistanceKm = 80.00m, EntryFee = 300m, MaxParticipants = 100 };
        var fun5 = new Category { Event = parkRun, CategoryName = "5km Fun Run", DistanceKm = 5.00m, EntryFee = 50m, MaxParticipants = 500 };
        var park10 = new Category { Event = parkRun, CategoryName = "10km Run", DistanceKm = 10.00m, EntryFee = 100m, MaxParticipants = 300 };

        var leratoRiver = new Enrolment { Participant = lerato, Event = riverRun, Category = run10 };
        var leratoPark = new Enrolment { Participant = lerato, Event = parkRun, Category = fun5 };
        var siphoRiver = new Enrolment { Participant = sipho, Event = riverRun, Category = half };
        var siphoCycle = new Enrolment { Participant = sipho, Event = charityCycle, Category = cycle40 };

        // Results only for events that have already taken place.
        var leratoResult = new Result { Enrolment = leratoPark, FinishTime = new TimeSpan(0, 24, 18), Position = 3 };
        var siphoResult = new Result { Enrolment = siphoCycle, FinishTime = new TimeSpan(1, 12, 7), Position = 12 };

        db.AddRange(thandiwe, johan, lerato, sipho,
                    riverRun, charityCycle, parkRun,
                    run10, half, cycle40, cycle80, fun5, park10,
                    leratoRiver, leratoPark, siphoRiver, siphoCycle,
                    leratoResult, siphoResult);
        db.SaveChanges();
    }
}
