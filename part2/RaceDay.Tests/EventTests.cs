namespace RaceDay.Tests;

public class EventTests(RaceDayFactory factory) : IClassFixture<RaceDayFactory>
{
    [Fact]
    public async Task Organiser_CanCreateEvent()
    {
        var (organiser, organiserId, _) = await TestApi.RegisterAndLogin(factory, "Organiser");

        var response = await organiser.PostAsJsonAsync("/api/events", TestApi.NewEventRequest("Soweto 10km"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<EventResponse>();
        Assert.Equal("Soweto 10km", created!.Name);
        Assert.Equal(10m, created.DistanceKm);
        Assert.Equal("Run", created.EventType);
        Assert.Equal(organiserId, created.OrganiserId);
    }

    [Theory]
    [InlineData("Run")]
    [InlineData("Walk")]
    [InlineData("Cycle")]
    public async Task Organiser_CanCreateEvent_OfEachType(string type)
    {
        var (organiser, _, _) = await TestApi.RegisterAndLogin(factory, "Organiser");

        var created = await TestApi.CreateEvent(organiser, $"{type} event", type);

        Assert.Equal(type, created.EventType);
    }

    [Fact]
    public async Task CreateEvent_InvalidEventType_ReturnsBadRequest()
    {
        var (organiser, _, _) = await TestApi.RegisterAndLogin(factory, "Organiser");

        var response = await organiser.PostAsJsonAsync("/api/events", TestApi.NewEventRequest(type: "Swim"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_ZeroDistance_ReturnsBadRequest()
    {
        var (organiser, _, _) = await TestApi.RegisterAndLogin(factory, "Organiser");
        var request = TestApi.NewEventRequest();
        request.DistanceKm = 0;

        var response = await organiser.PostAsJsonAsync("/api/events", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_MissingName_ReturnsBadRequest()
    {
        var (organiser, _, _) = await TestApi.RegisterAndLogin(factory, "Organiser");
        var request = TestApi.NewEventRequest();
        request.Name = "";

        var response = await organiser.PostAsJsonAsync("/api/events", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanUpdateOwnEvent()
    {
        var (organiser, _, _) = await TestApi.RegisterAndLogin(factory, "Organiser");
        var created = await TestApi.CreateEvent(organiser);
        var update = TestApi.NewEventRequest("Renamed Ride", "Cycle");
        update.DistanceKm = 60m;

        var response = await organiser.PutAsJsonAsync($"/api/events/{created.Id}", update);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<EventResponse>();
        Assert.Equal("Renamed Ride", updated!.Name);
        Assert.Equal("Cycle", updated.EventType);
        Assert.Equal(60m, updated.DistanceKm);
    }

    [Fact]
    public async Task Organiser_CanDeleteOwnEvent()
    {
        var (organiser, _, _) = await TestApi.RegisterAndLogin(factory, "Organiser");
        var created = await TestApi.CreateEvent(organiser);

        var delete = await organiser.DeleteAsync($"/api/events/{created.Id}");
        var afterDelete = await organiser.GetAsync($"/api/events/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, afterDelete.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotUpdateAnotherOrganisersEvent()
    {
        var (owner, _, _) = await TestApi.RegisterAndLogin(factory, "Organiser");
        var (otherOrganiser, _, _) = await TestApi.RegisterAndLogin(factory, "Organiser");
        var created = await TestApi.CreateEvent(owner);

        var response = await otherOrganiser.PutAsJsonAsync($"/api/events/{created.Id}", TestApi.NewEventRequest("Stolen"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotDeleteAnotherOrganisersEvent()
    {
        var (owner, _, _) = await TestApi.RegisterAndLogin(factory, "Organiser");
        var (otherOrganiser, _, _) = await TestApi.RegisterAndLogin(factory, "Organiser");
        var created = await TestApi.CreateEvent(owner);

        var response = await otherOrganiser.DeleteAsync($"/api/events/{created.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEvent_Unknown_ReturnsNotFound()
    {
        var (organiser, _, _) = await TestApi.RegisterAndLogin(factory, "Organiser");

        var response = await organiser.PutAsJsonAsync("/api/events/999999", TestApi.NewEventRequest());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Anyone_CanViewEvents_WithoutLogin()
    {
        var (organiser, _, _) = await TestApi.RegisterAndLogin(factory, "Organiser");
        var created = await TestApi.CreateEvent(organiser, "Public Event");

        var response = await factory.CreateClient().GetAsync("/api/events");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var events = await response.Content.ReadFromJsonAsync<List<EventResponse>>();
        Assert.Contains(events!, e => e.Id == created.Id);
    }

    [Fact]
    public async Task Events_CanBeFilteredByType()
    {
        var (organiser, _, _) = await TestApi.RegisterAndLogin(factory, "Organiser");
        var cycle = await TestApi.CreateEvent(organiser, "Filter Cycle", "Cycle");
        await TestApi.CreateEvent(organiser, "Filter Run", "Run");

        var response = await factory.CreateClient().GetAsync("/api/events?type=Cycle");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var events = await response.Content.ReadFromJsonAsync<List<EventResponse>>();
        Assert.Contains(events!, e => e.Id == cycle.Id);
        Assert.All(events!, e => Assert.Equal("Cycle", e.EventType));
    }

    [Fact]
    public async Task Participant_CanViewEvents()
    {
        var (organiser, _, _) = await TestApi.RegisterAndLogin(factory, "Organiser");
        var created = await TestApi.CreateEvent(organiser);
        var (participant, _, _) = await TestApi.RegisterAndLogin(factory, "Participant");

        var response = await participant.GetAsync($"/api/events/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanAddAndViewCategories()
    {
        var (organiser, _, _) = await TestApi.RegisterAndLogin(factory, "Organiser");
        var created = await TestApi.CreateEvent(organiser);

        var category = await TestApi.CreateCategory(organiser, created.Id, "Under 20");
        var list = await factory.CreateClient().GetAsync($"/api/events/{created.Id}/categories");

        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        var categories = await list.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        Assert.Contains(categories!, c => c.Id == category.Id && c.Name == "Under 20");
    }

    [Fact]
    public async Task Organiser_CannotAddCategory_ToAnotherOrganisersEvent()
    {
        var (owner, _, _) = await TestApi.RegisterAndLogin(factory, "Organiser");
        var (otherOrganiser, _, _) = await TestApi.RegisterAndLogin(factory, "Organiser");
        var created = await TestApi.CreateEvent(owner);

        var response = await otherOrganiser.PostAsJsonAsync($"/api/events/{created.Id}/categories",
            new CategoryRequest { Name = "Sneaky" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotDeleteCategory_WithEnrolments()
    {
        var (organiser, _, _) = await TestApi.RegisterAndLogin(factory, "Organiser");
        var (participant, _, _) = await TestApi.RegisterAndLogin(factory, "Participant");
        var created = await TestApi.CreateEvent(organiser);
        var category = await TestApi.CreateCategory(organiser, created.Id);
        await TestApi.Enrol(participant, created.Id, category.Id);

        var response = await organiser.DeleteAsync($"/api/categories/{category.Id}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
