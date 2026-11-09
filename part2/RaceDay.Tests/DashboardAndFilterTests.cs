namespace RaceDay.Tests;

public class DashboardAndFilterTests(RaceDayFactory factory) : IClassFixture<RaceDayFactory>
{
    [Fact]
    public async Task Organiser_Dashboard_ListsOwnEvents_WithEnrolmentCounts()
    {
        var (organiser, _, _) = await TestApi.RegisterAndLogin(factory, "Organiser");
        var (first, _, _) = await TestApi.RegisterAndLogin(factory, "Participant");
        var (second, _, _) = await TestApi.RegisterAndLogin(factory, "Participant");
        var busy = await TestApi.CreateEvent(organiser, "Busy Event");
        var quiet = await TestApi.CreateEvent(organiser, "Quiet Event");
        var category = await TestApi.CreateCategory(organiser, busy.Id);
        await TestApi.Enrol(first, busy.Id, category.Id);
        await TestApi.Enrol(second, busy.Id, category.Id);

        var mine = await organiser.GetFromJsonAsync<List<OrganiserEventResponse>>("/api/events/mine");

        Assert.Equal(2, mine!.Count);
        Assert.Equal(2, mine.Single(e => e.Id == busy.Id).EnrolmentCount);
        Assert.Equal(0, mine.Single(e => e.Id == quiet.Id).EnrolmentCount);
    }

    [Fact]
    public async Task Organiser_Dashboard_DoesNotShowAnotherOrganisersEvents()
    {
        var (organiser, _, _) = await TestApi.RegisterAndLogin(factory, "Organiser");
        var (other, _, _) = await TestApi.RegisterAndLogin(factory, "Organiser");
        await TestApi.CreateEvent(other, "Someone Else's Event");

        var mine = await organiser.GetFromJsonAsync<List<OrganiserEventResponse>>("/api/events/mine");

        Assert.Empty(mine!);
    }

    [Fact]
    public async Task Participant_CannotViewOrganiserDashboard_ReturnsForbidden()
    {
        var (participant, _, _) = await TestApi.RegisterAndLogin(factory, "Participant");

        var response = await participant.GetAsync("/api/events/mine");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Dashboard_WithoutLogin_ReturnsUnauthorized()
    {
        var response = await factory.CreateClient().GetAsync("/api/events/mine");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Events_CanBeSearched_ByName()
    {
        var (organiser, _, _) = await TestApi.RegisterAndLogin(factory, "Organiser");
        var unique = $"Zulu{Guid.NewGuid():N}";
        var target = await TestApi.CreateEvent(organiser, unique);
        await TestApi.CreateEvent(organiser, "Something Else");

        var results = await factory.CreateClient().GetFromJsonAsync<List<EventResponse>>($"/api/events?search={unique}");

        Assert.Equal(target.Id, Assert.Single(results!).Id);
    }

    [Fact]
    public async Task Events_CanBeFiltered_ByDistance()
    {
        var (organiser, _, _) = await TestApi.RegisterAndLogin(factory, "Organiser");
        var unique = $"Dist{Guid.NewGuid():N}";
        var shortRequest = TestApi.NewEventRequest($"{unique}-short");
        shortRequest.DistanceKm = 5m;
        var longRequest = TestApi.NewEventRequest($"{unique}-long");
        longRequest.DistanceKm = 42m;
        var shortEvent = await (await organiser.PostAsJsonAsync("/api/events", shortRequest)).Content.ReadFromJsonAsync<EventResponse>();
        var longEvent = await (await organiser.PostAsJsonAsync("/api/events", longRequest)).Content.ReadFromJsonAsync<EventResponse>();

        var results = await factory.CreateClient().GetFromJsonAsync<List<EventResponse>>(
            $"/api/events?search={unique}&minDistanceKm=10");

        Assert.Equal(longEvent!.Id, Assert.Single(results!).Id);
        Assert.DoesNotContain(results!, e => e.Id == shortEvent!.Id);
    }

    [Fact]
    public async Task Events_UpcomingOnly_HidesPastEvents()
    {
        var (organiser, _, _) = await TestApi.RegisterAndLogin(factory, "Organiser");
        var unique = $"Time{Guid.NewGuid():N}";
        var past = TestApi.NewEventRequest($"{unique}-past");
        past.Date = DateTime.UtcNow.AddDays(-10);
        var future = TestApi.NewEventRequest($"{unique}-future");
        future.Date = DateTime.UtcNow.AddDays(10);
        await organiser.PostAsJsonAsync("/api/events", past);
        var futureEvent = await (await organiser.PostAsJsonAsync("/api/events", future)).Content.ReadFromJsonAsync<EventResponse>();

        var results = await factory.CreateClient().GetFromJsonAsync<List<EventResponse>>(
            $"/api/events?search={unique}&upcomingOnly=true");

        Assert.Equal(futureEvent!.Id, Assert.Single(results!).Id);
    }

    [Fact]
    public async Task Events_FilterByUnknownType_ReturnsBadRequest()
    {
        var response = await factory.CreateClient().GetAsync("/api/events?type=Swim");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task MyResults_IncludeEventDateCategoryAndTotalFinishers()
    {
        var (organiser, _, _) = await TestApi.RegisterAndLogin(factory, "Organiser");
        var (first, _, _) = await TestApi.RegisterAndLogin(factory, "Participant");
        var (second, _, _) = await TestApi.RegisterAndLogin(factory, "Participant");
        var ev = await TestApi.CreateEvent(organiser, "Finishers Event");
        var category = await TestApi.CreateCategory(organiser, ev.Id, "21km");
        var firstEnrolment = await TestApi.Enrol(first, ev.Id, category.Id);
        var secondEnrolment = await TestApi.Enrol(second, ev.Id, category.Id);
        await organiser.PostAsJsonAsync("/api/results",
            new ResultRequest { EnrolmentId = firstEnrolment.Id, FinishTime = "01:30:00", Position = 2 });
        await organiser.PostAsJsonAsync("/api/results",
            new ResultRequest { EnrolmentId = secondEnrolment.Id, FinishTime = "01:20:00", Position = 1 });

        var mine = await first.GetFromJsonAsync<List<ResultResponse>>("/api/results/me");

        var result = Assert.Single(mine!);
        Assert.Equal("Finishers Event", result.EventName);
        Assert.Equal("21km", result.CategoryName);
        Assert.Equal(2, result.Position);
        Assert.Equal(2, result.TotalFinishers);
        Assert.Equal(ev.Date, result.EventDate);
    }
}
