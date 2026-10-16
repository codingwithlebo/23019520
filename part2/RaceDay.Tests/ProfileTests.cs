namespace RaceDay.Tests;

public class ProfileTests(RaceDayFactory factory) : IClassFixture<RaceDayFactory>
{
    [Theory]
    [InlineData("Participant")]
    [InlineData("Organiser")]
    public async Task BothRoles_CanViewOwnProfile(string role)
    {
        var (client, userId, email) = await TestApi.RegisterAndLogin(factory, role);

        var response = await client.GetAsync($"/api/users/{userId}/profile");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<ProfileResponse>();
        Assert.Equal(email, profile!.Email);
        Assert.Equal(role, profile.Role);
    }

    [Theory]
    [InlineData("Participant")]
    [InlineData("Organiser")]
    public async Task BothRoles_CanUpdateOwnProfile(string role)
    {
        var (client, userId, _) = await TestApi.RegisterAndLogin(factory, role);

        var update = await client.PutAsJsonAsync($"/api/users/{userId}/profile",
            new UpdateProfileRequest { FullName = "Updated Name", Phone = "0821112222", Bio = "New bio" });

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var profile = await update.Content.ReadFromJsonAsync<ProfileResponse>();
        Assert.Equal("Updated Name", profile!.FullName);
        Assert.Equal("New bio", profile.Bio);
    }

    [Fact]
    public async Task User_CannotViewAnotherUsersProfile()
    {
        var (_, otherId, _) = await TestApi.RegisterAndLogin(factory, "Participant");
        var (client, _, _) = await TestApi.RegisterAndLogin(factory, "Participant");

        var response = await client.GetAsync($"/api/users/{otherId}/profile");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task User_CannotUpdateAnotherUsersProfile()
    {
        var (_, otherId, _) = await TestApi.RegisterAndLogin(factory, "Participant");
        var (client, _, _) = await TestApi.RegisterAndLogin(factory, "Participant");

        var response = await client.PutAsJsonAsync($"/api/users/{otherId}/profile",
            new UpdateProfileRequest { FullName = "Hacked" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Profile_WithoutLogin_ReturnsUnauthorized()
    {
        var response = await factory.CreateClient().GetAsync("/api/users/1/profile");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
