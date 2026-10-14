using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.Dtos;

namespace RaceDay.Tests;

public class EnrolmentAndResultTests : ApiTestBase, IClassFixture<RaceDayFactory>
{
    public EnrolmentAndResultTests(RaceDayFactory factory) : base(factory) { }

    [Fact]
    public async Task Participant_CanEnrol_AndEnrolmentLinksParticipantEventAndCategory()
    {
        var (organiser, _, _) = await this.SignUp("Organiser");
        var (participant, participantId, _) = await this.SignUp("Participant");
        var ev = await CreateEvent(organiser);
        var category = await CreateCategory(organiser, ev.EventId);

        var response = await participant.PostAsync($"/api/categories/{category.CategoryId}/enrolments", null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var enrolment = await response.Content.ReadFromJsonAsync<EnrolmentResponse>();
        Assert.Equal(participantId, enrolment!.ParticipantId);
        Assert.Equal(ev.EventId, enrolment.EventId);
        Assert.Equal(category.CategoryId, enrolment.CategoryId);

        var mine = await participant.GetFromJsonAsync<List<EnrolmentResponse>>("/api/enrolments/me");
        Assert.Contains(mine!, e => e.EnrolmentId == enrolment.EnrolmentId);
    }

    [Fact]
    public async Task Enrol_SameCategoryTwice_Returns409()
    {
        var (organiser, _, _) = await this.SignUp("Organiser");
        var (participant, _, _) = await this.SignUp("Participant");
        var ev = await CreateEvent(organiser);
        var category = await CreateCategory(organiser, ev.EventId);
        await Enrol(participant, category.CategoryId);

        var second = await participant.PostAsync($"/api/categories/{category.CategoryId}/enrolments", null);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Enrol_FullCategory_Returns400()
    {
        var (organiser, _, _) = await this.SignUp("Organiser");
        var (first, _, _) = await this.SignUp("Participant");
        var (second, _, _) = await this.SignUp("Participant");
        var ev = await CreateEvent(organiser);
        var category = await CreateCategory(organiser, ev.EventId, maxParticipants: 1);
        await Enrol(first, category.CategoryId);

        var response = await second.PostAsync($"/api/categories/{category.CategoryId}/enrolments", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_UnknownCategory_Returns404()
    {
        var (participant, _, _) = await this.SignUp("Participant");
        var response = await participant.PostAsync("/api/categories/999999/enrolments", null);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanSeeAllEnrolmentsForOwnEvent()
    {
        var (organiser, _, _) = await this.SignUp("Organiser");
        var (p1, _, _) = await this.SignUp("Participant");
        var (p2, _, _) = await this.SignUp("Participant");
        var ev = await CreateEvent(organiser);
        var category = await CreateCategory(organiser, ev.EventId);
        await Enrol(p1, category.CategoryId);
        await Enrol(p2, category.CategoryId);

        var list = await organiser.GetFromJsonAsync<List<EnrolmentResponse>>($"/api/events/{ev.EventId}/enrolments");

        Assert.Equal(2, list!.Count);
    }

    [Fact]
    public async Task Organiser_CannotSeeEnrolmentsForAnotherOrganisersEvent_Returns403()
    {
        var (owner, _, _) = await this.SignUp("Organiser");
        var (other, _, _) = await this.SignUp("Organiser");
        var ev = await CreateEvent(owner);

        var response = await other.GetAsync($"/api/events/{ev.EventId}/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CanCancelOwnEnrolment()
    {
        var (organiser, _, _) = await this.SignUp("Organiser");
        var (participant, _, _) = await this.SignUp("Participant");
        var ev = await CreateEvent(organiser);
        var category = await CreateCategory(organiser, ev.EventId);
        var enrolment = await Enrol(participant, category.CategoryId);

        var response = await participant.DeleteAsync($"/api/enrolments/{enrolment.EnrolmentId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotCancelSomeoneElsesEnrolment_Returns403()
    {
        var (organiser, _, _) = await this.SignUp("Organiser");
        var (owner, _, _) = await this.SignUp("Participant");
        var (other, _, _) = await this.SignUp("Participant");
        var ev = await CreateEvent(organiser);
        var category = await CreateCategory(organiser, ev.EventId);
        var enrolment = await Enrol(owner, category.CategoryId);

        var response = await other.DeleteAsync($"/api/enrolments/{enrolment.EnrolmentId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanCaptureResult_AndParticipantSeesItInHistory()
    {
        var (organiser, _, _) = await this.SignUp("Organiser");
        var (participant, _, _) = await this.SignUp("Participant");
        var ev = await CreateEvent(organiser);
        var category = await CreateCategory(organiser, ev.EventId);
        var enrolment = await Enrol(participant, category.CategoryId);

        var capture = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/results",
            new { finishTime = "00:47:31", position = 4 });

        Assert.Equal(HttpStatusCode.Created, capture.StatusCode);

        var history = await participant.GetFromJsonAsync<List<ResultResponse>>("/api/participants/me/results");
        var result = Assert.Single(history!);
        Assert.Equal(TimeSpan.Parse("00:47:31"), result.FinishTime);
        Assert.Equal(4, result.Position);
    }

    [Fact]
    public async Task CaptureResult_Twice_Returns409()
    {
        var (organiser, _, _) = await this.SignUp("Organiser");
        var (participant, _, _) = await this.SignUp("Participant");
        var ev = await CreateEvent(organiser);
        var category = await CreateCategory(organiser, ev.EventId);
        var enrolment = await Enrol(participant, category.CategoryId);
        var url = $"/api/enrolments/{enrolment.EnrolmentId}/results";
        await organiser.PostAsJsonAsync(url, new { finishTime = "00:50:00", position = 1 });

        var second = await organiser.PostAsJsonAsync(url, new { finishTime = "00:51:00", position = 2 });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotCaptureResultForAnotherOrganisersEvent_Returns403()
    {
        var (owner, _, _) = await this.SignUp("Organiser");
        var (other, _, _) = await this.SignUp("Organiser");
        var (participant, _, _) = await this.SignUp("Participant");
        var ev = await CreateEvent(owner);
        var category = await CreateCategory(owner, ev.EventId);
        var enrolment = await Enrol(participant, category.CategoryId);

        var response = await other.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/results",
            new { finishTime = "00:50:00", position = 1 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotViewAnotherParticipantsResult_Returns403()
    {
        var (organiser, _, _) = await this.SignUp("Organiser");
        var (owner, _, _) = await this.SignUp("Participant");
        var (other, _, _) = await this.SignUp("Participant");
        var ev = await CreateEvent(organiser);
        var category = await CreateCategory(organiser, ev.EventId);
        var enrolment = await Enrol(owner, category.CategoryId);
        await organiser.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/results",
            new { finishTime = "00:50:00", position = 1 });

        var response = await other.GetAsync($"/api/enrolments/{enrolment.EnrolmentId}/results");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanViewParticipantProfile_OnlyIfEnrolledInTheirEvent()
    {
        var (organiser, _, _) = await this.SignUp("Organiser");
        var (participant, participantId, _) = await this.SignUp("Participant");
        var (stranger, strangerId, _) = await this.SignUp("Participant");
        var ev = await CreateEvent(organiser);
        var category = await CreateCategory(organiser, ev.EventId);
        await Enrol(participant, category.CategoryId);

        var allowed = await organiser.GetAsync($"/api/participants/{participantId}");
        var denied = await organiser.GetAsync($"/api/participants/{strangerId}");

        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
    }
}
