using System.Net.Http.Json;
using RaceDay.Api.Dtos;

namespace RaceDay.Tests;

public abstract class ApiTestBase
{
    protected const string Password = "Passw0rd!123";
    protected readonly RaceDayFactory Factory;

    protected ApiTestBase(RaceDayFactory factory) => this.Factory = factory;

    protected static string NewEmail() => $"{Guid.NewGuid():N}@raceday.test";

    /// <summary>Registers a new user, logs them in, and returns a client holding their session cookie.</summary>
    protected async Task<(HttpClient Client, int UserId, string Email)> SignUp(string accountType)
    {
        var client = this.Factory.CreateClient();
        var email = NewEmail();

        var register = await client.PostAsJsonAsync("/api/auth/register",
            new { fullName = "Test User", email, password = Password, phoneNumber = "0821234567", accountType });
        register.EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });
        login.EnsureSuccessStatusCode();
        var profile = (await login.Content.ReadFromJsonAsync<UserProfileResponse>())!;

        return (client, profile.UserId, email);
    }

    protected static async Task<EventResponse> CreateEvent(HttpClient organiser, string eventType = "Run")
    {
        var response = await organiser.PostAsJsonAsync("/api/events", new
        {
            eventName = "Test Event",
            description = "A test event",
            eventDate = "2026-12-01",
            location = "Standerton",
            eventType,
            distanceKm = 10
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EventResponse>())!;
    }

    protected static async Task<CategoryResponse> CreateCategory(HttpClient organiser, int eventId, int maxParticipants = 100)
    {
        var response = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories", new
        {
            categoryName = "10km Run",
            distanceKm = 10,
            entryFee = 150,
            maxParticipants
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CategoryResponse>())!;
    }

    protected static async Task<EnrolmentResponse> Enrol(HttpClient participant, int categoryId)
    {
        var response = await participant.PostAsync($"/api/categories/{categoryId}/enrolments", null);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EnrolmentResponse>())!;
    }
}
