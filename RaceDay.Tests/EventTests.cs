using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.Dtos;

namespace RaceDay.Tests;

public class EventTests : ApiTestBase, IClassFixture<RaceDayFactory>
{
    public EventTests(RaceDayFactory factory) : base(factory) { }

    [Fact]
    public async Task GetEvents_IsPublic_Returns200WithoutLogin()
    {
        var response = await this.Factory.CreateClient().GetAsync("/api/events");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanCreateEvent_Returns201()
    {
        var (organiser, userId, _) = await this.SignUp("Organiser");

        var response = await organiser.PostAsJsonAsync("/api/events", new
        {
            eventName = "River Run",
            description = "Scenic run",
            eventDate = "2026-11-08",
            location = "Standerton",
            eventType = "Run",
            distanceKm = 21.1
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<EventResponse>();
        Assert.Equal(userId, created!.OrganiserId);
        Assert.Equal("Run", created.EventType);
    }

    [Fact]
    public async Task CreatedEvent_AppearsInPublicList()
    {
        var (organiser, _, _) = await this.SignUp("Organiser");
        var created = await CreateEvent(organiser);

        var list = await this.Factory.CreateClient().GetFromJsonAsync<List<EventResponse>>("/api/events");

        Assert.Contains(list!, e => e.EventId == created.EventId);
    }

    [Fact]
    public async Task CreateEvent_InvalidEventType_Returns400()
    {
        var (organiser, _, _) = await this.SignUp("Organiser");

        var response = await organiser.PostAsJsonAsync("/api/events", new
        {
            eventName = "Bad", eventDate = "2026-11-08", location = "X", eventType = "Swim", distanceKm = 5
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanUpdateOwnEvent()
    {
        var (organiser, _, _) = await this.SignUp("Organiser");
        var created = await CreateEvent(organiser);

        var response = await organiser.PutAsJsonAsync($"/api/events/{created.EventId}", new
        {
            eventName = "Renamed Event", description = "Updated", eventDate = "2026-12-24",
            location = "Johannesburg", eventType = "Cycle", distanceKm = 40
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<EventResponse>();
        Assert.Equal("Renamed Event", updated!.EventName);
        Assert.Equal("Cycle", updated.EventType);
    }

    [Fact]
    public async Task Organiser_CanDeleteOwnEvent()
    {
        var (organiser, _, _) = await this.SignUp("Organiser");
        var created = await CreateEvent(organiser);

        var delete = await organiser.DeleteAsync($"/api/events/{created.EventId}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var get = await organiser.GetAsync($"/api/events/{created.EventId}");
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotEditAnotherOrganisersEvent_Returns403()
    {
        var (owner, _, _) = await this.SignUp("Organiser");
        var (other, _, _) = await this.SignUp("Organiser");
        var created = await CreateEvent(owner);

        var response = await other.PutAsJsonAsync($"/api/events/{created.EventId}", new
        {
            eventName = "Hijack", eventDate = "2026-12-01", location = "X", eventType = "Run", distanceKm = 5
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotDeleteEventWithEnrolments_Returns409()
    {
        var (organiser, _, _) = await this.SignUp("Organiser");
        var (participant, _, _) = await this.SignUp("Participant");
        var ev = await CreateEvent(organiser);
        var category = await CreateCategory(organiser, ev.EventId);
        await Enrol(participant, category.CategoryId);

        var response = await organiser.DeleteAsync($"/api/events/{ev.EventId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task GetEvent_Unknown_Returns404()
    {
        var response = await this.Factory.CreateClient().GetAsync("/api/events/999999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanManageCategories_AndPublicCanView()
    {
        var (organiser, _, _) = await this.SignUp("Organiser");
        var ev = await CreateEvent(organiser);
        var category = await CreateCategory(organiser, ev.EventId, maxParticipants: 50);

        var list = await this.Factory.CreateClient().GetFromJsonAsync<List<CategoryResponse>>($"/api/events/{ev.EventId}/categories");
        Assert.Contains(list!, c => c.CategoryId == category.CategoryId && c.MaxParticipants == 50);

        var update = await organiser.PutAsJsonAsync($"/api/categories/{category.CategoryId}",
            new { categoryName = "Under 20", distanceKm = 5, entryFee = 80, maxParticipants = 75 });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var delete = await organiser.DeleteAsync($"/api/categories/{category.CategoryId}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    }
}
