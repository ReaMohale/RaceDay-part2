using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;

namespace RaceDay.Tests;

public class AuthTests : ApiTestBase, IClassFixture<RaceDayFactory>
{
    public AuthTests(RaceDayFactory factory) : base(factory) { }

    [Theory]
    [InlineData("Organiser")]
    [InlineData("Participant")]
    public async Task Register_ValidDetails_Returns201(string accountType)
    {
        var client = this.Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register",
            new { fullName = "Thandi Nkosi", email = NewEmail(), password = Password, phoneNumber = "0821234567", accountType });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<UserProfileResponse>();
        Assert.Equal(accountType, profile!.Role);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409_EvenAcrossRoles()
    {
        var client = this.Factory.CreateClient();
        var email = NewEmail();
        var first = await client.PostAsJsonAsync("/api/auth/register",
            new { fullName = "A", email, password = Password, accountType = "Organiser" });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await client.PostAsJsonAsync("/api/auth/register",
            new { fullName = "B", email, password = Password, accountType = "Participant" });
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidAccountType_Returns400()
    {
        var client = this.Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register",
            new { fullName = "A", email = NewEmail(), password = Password, accountType = "Admin" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_ShortPassword_Returns400()
    {
        var client = this.Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register",
            new { fullName = "A", email = NewEmail(), password = "short", accountType = "Participant" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Password_IsStoredHashed_NotInPlainText()
    {
        var (_, userId, _) = await this.SignUp("Participant");

        using var scope = this.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        var participant = await db.Participants.FindAsync(userId);

        Assert.NotNull(participant);
        Assert.NotEqual(Password, participant!.PasswordHash);
        Assert.DoesNotContain(Password, participant.PasswordHash);
    }

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithRole()
    {
        var client = this.Factory.CreateClient();
        var email = NewEmail();
        await client.PostAsJsonAsync("/api/auth/register",
            new { fullName = "Org", email, password = Password, accountType = "Organiser" });

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<UserProfileResponse>();
        Assert.Equal("Organiser", profile!.Role);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var (client, _, email) = await this.SignUp("Participant");
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "WrongPassword1" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401()
    {
        var client = this.Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email = NewEmail(), password = Password });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutLogin_Returns401()
    {
        var client = this.Factory.CreateClient();
        var response = await client.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Session_KeepsUserLoggedInAcrossRequests()
    {
        var (client, userId, _) = await this.SignUp("Participant");

        var response = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<UserProfileResponse>();
        Assert.Equal(userId, profile!.UserId);
        Assert.Equal("Participant", profile.Role);
    }

    [Fact]
    public async Task Logout_EndsTheSession()
    {
        var (client, _, _) = await this.SignUp("Organiser");

        var logout = await client.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        var after = await client.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.Unauthorized, after.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_ChangesOwnDetails()
    {
        var (client, _, _) = await this.SignUp("Participant");

        var response = await client.PutAsJsonAsync("/api/users/me", new { fullName = "New Name", phoneNumber = "0731112222" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<UserProfileResponse>();
        Assert.Equal("New Name", profile!.FullName);
    }
}
