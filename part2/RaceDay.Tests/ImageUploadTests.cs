using System.Net.Http.Headers;

namespace RaceDay.Tests;

public class ImageUploadTests(RaceDayFactory factory) : IClassFixture<RaceDayFactory>
{
    private static readonly byte[] PngBytes = { 137, 80, 78, 71, 13, 10, 26, 10, 1, 2, 3 };

    private static MultipartFormDataContent Image(string contentType = "image/png", byte[]? bytes = null)
    {
        var file = new ByteArrayContent(bytes ?? PngBytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        var form = new MultipartFormDataContent();
        form.Add(file, "file", "picture.png");
        return form;
    }

    [Fact]
    public async Task Organiser_CanUploadBanner_AndAnyoneCanViewIt()
    {
        var (organiser, _, _) = await TestApi.RegisterAndLogin(factory, "Organiser");
        var ev = await TestApi.CreateEvent(organiser);

        var upload = await organiser.PostAsync($"/api/events/{ev.Id}/banner", Image());

        Assert.Equal(HttpStatusCode.OK, upload.StatusCode);
        var updated = await upload.Content.ReadFromJsonAsync<EventResponse>();
        Assert.Equal($"/api/events/{ev.Id}/banner", updated!.BannerUrl);

        var download = await factory.CreateClient().GetAsync(updated.BannerUrl);
        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        Assert.Equal("image/png", download.Content.Headers.ContentType!.MediaType);
        Assert.Equal(PngBytes, await download.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task Event_WithoutBanner_HasNoBannerUrl_AndBannerReturnsNotFound()
    {
        var (organiser, _, _) = await TestApi.RegisterAndLogin(factory, "Organiser");
        var ev = await TestApi.CreateEvent(organiser);

        Assert.Null(ev.BannerUrl);
        var response = await factory.CreateClient().GetAsync($"/api/events/{ev.Id}/banner");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_NotAnImage_ReturnsBadRequest()
    {
        var (organiser, _, _) = await TestApi.RegisterAndLogin(factory, "Organiser");
        var ev = await TestApi.CreateEvent(organiser);

        var response = await organiser.PostAsync($"/api/events/{ev.Id}/banner", Image("text/plain"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_TooLarge_ReturnsBadRequest()
    {
        var (organiser, _, _) = await TestApi.RegisterAndLogin(factory, "Organiser");
        var ev = await TestApi.CreateEvent(organiser);
        var tooBig = new byte[(5 * 1024 * 1024) + 1];

        var response = await organiser.PostAsync($"/api/events/{ev.Id}/banner", Image(bytes: tooBig));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_ReplacesThePreviousImage()
    {
        var (organiser, _, _) = await TestApi.RegisterAndLogin(factory, "Organiser");
        var ev = await TestApi.CreateEvent(organiser);
        await organiser.PostAsync($"/api/events/{ev.Id}/banner", Image());
        var secondBytes = new byte[] { 255, 216, 255, 224, 9, 9 };

        var second = await organiser.PostAsync($"/api/events/{ev.Id}/banner", Image("image/jpeg", secondBytes));

        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var download = await factory.CreateClient().GetAsync($"/api/events/{ev.Id}/banner");
        Assert.Equal("image/jpeg", download.Content.Headers.ContentType!.MediaType);
        Assert.Equal(secondBytes, await download.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task Participant_CannotUploadBanner_ReturnsForbidden()
    {
        var (organiser, _, _) = await TestApi.RegisterAndLogin(factory, "Organiser");
        var (participant, _, _) = await TestApi.RegisterAndLogin(factory, "Participant");
        var ev = await TestApi.CreateEvent(organiser);

        var response = await participant.PostAsync($"/api/events/{ev.Id}/banner", Image());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotUploadBanner_ForAnotherOrganisersEvent()
    {
        var (owner, _, _) = await TestApi.RegisterAndLogin(factory, "Organiser");
        var (otherOrganiser, _, _) = await TestApi.RegisterAndLogin(factory, "Organiser");
        var ev = await TestApi.CreateEvent(owner);

        var response = await otherOrganiser.PostAsync($"/api/events/{ev.Id}/banner", Image());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_WithoutLogin_ReturnsUnauthorized()
    {
        var response = await factory.CreateClient().PostAsync("/api/events/1/banner", Image());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("Participant")]
    [InlineData("Organiser")]
    public async Task BothRoles_CanUploadAndViewOwnProfilePicture(string role)
    {
        var (client, userId, _) = await TestApi.RegisterAndLogin(factory, role);

        var upload = await client.PostAsync($"/api/users/{userId}/profile/picture", Image());

        Assert.Equal(HttpStatusCode.OK, upload.StatusCode);
        var profile = await upload.Content.ReadFromJsonAsync<ProfileResponse>();
        Assert.Equal($"/api/users/{userId}/profile/picture", profile!.ProfilePictureUrl);

        var download = await client.GetAsync(profile.ProfilePictureUrl);
        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        Assert.Equal(PngBytes, await download.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task User_CannotUploadPicture_ForAnotherUser()
    {
        var (_, otherId, _) = await TestApi.RegisterAndLogin(factory, "Participant");
        var (client, _, _) = await TestApi.RegisterAndLogin(factory, "Participant");

        var response = await client.PostAsync($"/api/users/{otherId}/profile/picture", Image());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ProfilePicture_NotAnImage_ReturnsBadRequest()
    {
        var (client, userId, _) = await TestApi.RegisterAndLogin(factory, "Participant");

        var response = await client.PostAsync($"/api/users/{userId}/profile/picture", Image("application/pdf"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ProfilePicture_WithoutLogin_ReturnsUnauthorized()
    {
        var response = await factory.CreateClient().GetAsync("/api/users/1/profile/picture");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
