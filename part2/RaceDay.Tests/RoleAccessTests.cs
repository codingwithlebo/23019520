namespace RaceDay.Tests;

/// <summary>Role-based access: each role is rejected from the other role's functionality.</summary>
public class RoleAccessTests(RaceDayFactory factory) : IClassFixture<RaceDayFactory>
{
    [Fact]
    public async Task Anonymous_CannotCreateEvent_ReturnsUnauthorized()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/events", TestApi.NewEventRequest());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotCreateEvent_ReturnsForbidden()
    {
        var (participant, _, _) = await TestApi.RegisterAndLogin(factory, "Participant");

        var response = await participant.PostAsJsonAsync("/api/events", TestApi.NewEventRequest());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotUpdateOrDeleteEvents()
    {
        var (organiser, _, _) = await TestApi.RegisterAndLogin(factory, "Organiser");
        var created = await TestApi.CreateEvent(organiser);
        var (participant, _, _) = await TestApi.RegisterAndLogin(factory, "Participant");

        var update = await participant.PutAsJsonAsync($"/api/events/{created.Id}", TestApi.NewEventRequest());
        var delete = await participant.DeleteAsync($"/api/events/{created.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotManageCategories_ReturnsForbidden()
    {
        var (organiser, _, _) = await TestApi.RegisterAndLogin(factory, "Organiser");
        var created = await TestApi.CreateEvent(organiser);
        var category = await TestApi.CreateCategory(organiser, created.Id);
        var (participant, _, _) = await TestApi.RegisterAndLogin(factory, "Participant");

        var add = await participant.PostAsJsonAsync($"/api/events/{created.Id}/categories", new CategoryRequest { Name = "X" });
        var delete = await participant.DeleteAsync($"/api/categories/{category.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, add.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotEnrol_ReturnsForbidden()
    {
        var (organiser, _, _) = await TestApi.RegisterAndLogin(factory, "Organiser");
        var created = await TestApi.CreateEvent(organiser);
        var category = await TestApi.CreateCategory(organiser, created.Id);

        var response = await organiser.PostAsJsonAsync("/api/enrolments",
            new EnrolmentRequest { EventId = created.Id, CategoryId = category.Id });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotViewParticipantOnlyEndpoints()
    {
        var (organiser, _, _) = await TestApi.RegisterAndLogin(factory, "Organiser");

        var enrolments = await organiser.GetAsync("/api/enrolments/me");
        var results = await organiser.GetAsync("/api/results/me");

        Assert.Equal(HttpStatusCode.Forbidden, enrolments.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, results.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotViewAllEnrolmentsForAnEvent_ReturnsForbidden()
    {
        var (organiser, _, _) = await TestApi.RegisterAndLogin(factory, "Organiser");
        var created = await TestApi.CreateEvent(organiser);
        var (participant, _, _) = await TestApi.RegisterAndLogin(factory, "Participant");

        var response = await participant.GetAsync($"/api/events/{created.Id}/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotCaptureResults_ReturnsForbidden()
    {
        var (participant, _, _) = await TestApi.RegisterAndLogin(factory, "Participant");

        var response = await participant.PostAsJsonAsync("/api/results",
            new ResultRequest { EnrolmentId = 1, FinishTime = "00:40:00", Position = 1 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_CannotViewEventResults_ReturnsUnauthorized()
    {
        var response = await factory.CreateClient().GetAsync("/api/events/1/results");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
