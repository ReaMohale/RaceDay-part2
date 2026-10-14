using System.Net;
using System.Net.Http.Json;

namespace RaceDay.Tests;

/// <summary>Organiser-only features must reject Participants (and the reverse), and anonymous users get 401.</summary>
public class RoleEnforcementTests : ApiTestBase, IClassFixture<RaceDayFactory>
{
    public RoleEnforcementTests(RaceDayFactory factory) : base(factory) { }

    private static object NewEventBody() => new
    {
        eventName = "X", eventDate = "2026-12-01", location = "Y", eventType = "Run", distanceKm = 5
    };

    [Fact]
    public async Task Participant_CannotCreateEvent_Returns403()
    {
        var (participant, _, _) = await this.SignUp("Participant");
        var response = await participant.PostAsJsonAsync("/api/events", NewEventBody());
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_CannotCreateEvent_Returns401()
    {
        var response = await this.Factory.CreateClient().PostAsJsonAsync("/api/events", NewEventBody());
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotCreateCategory_Returns403()
    {
        var (organiser, _, _) = await this.SignUp("Organiser");
        var (participant, _, _) = await this.SignUp("Participant");
        var ev = await CreateEvent(organiser);

        var response = await participant.PostAsJsonAsync($"/api/events/{ev.EventId}/categories",
            new { categoryName = "Hack", distanceKm = 5, entryFee = 0, maxParticipants = 10 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotEnrol_Returns403()
    {
        var (organiser, _, _) = await this.SignUp("Organiser");
        var ev = await CreateEvent(organiser);
        var category = await CreateCategory(organiser, ev.EventId);

        var response = await organiser.PostAsync($"/api/categories/{category.CategoryId}/enrolments", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotCaptureResult_Returns403()
    {
        var (organiser, _, _) = await this.SignUp("Organiser");
        var (participant, _, _) = await this.SignUp("Participant");
        var ev = await CreateEvent(organiser);
        var category = await CreateCategory(organiser, ev.EventId);
        var enrolment = await Enrol(participant, category.CategoryId);

        var response = await participant.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/results",
            new { finishTime = "00:45:00", position = 1 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotViewMyEnrolments_Returns403()
    {
        var (organiser, _, _) = await this.SignUp("Organiser");
        var response = await organiser.GetAsync("/api/enrolments/me");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/enrolments/me")]
    [InlineData("/api/participants/me/results")]
    [InlineData("/api/users/me")]
    public async Task ProtectedGetEndpoints_RejectAnonymous_Returns401(string url)
    {
        var response = await this.Factory.CreateClient().GetAsync(url);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
