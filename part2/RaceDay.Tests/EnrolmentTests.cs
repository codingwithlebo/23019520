namespace RaceDay.Tests;

public class EnrolmentTests(RaceDayFactory factory) : IClassFixture<RaceDayFactory>
{
    [Fact]
    public async Task Participant_CanEnrol_AndTheEnrolmentIsRecorded()
    {
        var (organiser, _, _) = await TestApi.RegisterAndLogin(factory, "Organiser");
        var (participant, participantId, _) = await TestApi.RegisterAndLogin(factory, "Participant");
        var ev = await TestApi.CreateEvent(organiser);
        var category = await TestApi.CreateCategory(organiser, ev.Id, "21km");

        var response = await participant.PostAsJsonAsync("/api/enrolments",
            new EnrolmentRequest { EventId = ev.Id, CategoryId = category.Id });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<EnrolmentResponse>();

        // The participant, the event and the selected category are all linked in the database.
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        var stored = await db.Enrolments.SingleAsync(e => e.Id == created!.Id);
        Assert.Equal(participantId, stored.ParticipantId);
        Assert.Equal(ev.Id, stored.EventId);
        Assert.Equal(category.Id, stored.CategoryId);
    }

    [Fact]
    public async Task Participant_SeesOwnEnrolment_InMyEnrolments()
    {
        var (organiser, _, _) = await TestApi.RegisterAndLogin(factory, "Organiser");
        var (participant, _, _) = await TestApi.RegisterAndLogin(factory, "Participant");
        var ev = await TestApi.CreateEvent(organiser, "My Event");
        var category = await TestApi.CreateCategory(organiser, ev.Id, "Senior");
        var enrolment = await TestApi.Enrol(participant, ev.Id, category.Id);

        var response = await participant.GetAsync("/api/enrolments/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var mine = await response.Content.ReadFromJsonAsync<List<EnrolmentResponse>>();
        var found = Assert.Single(mine!, e => e.Id == enrolment.Id);
        Assert.Equal("My Event", found.EventName);
        Assert.Equal("Senior", found.CategoryName);
    }

    [Fact]
    public async Task Participant_DoesNotSeeAnotherParticipantsEnrolments()
    {
        var (organiser, _, _) = await TestApi.RegisterAndLogin(factory, "Organiser");
        var (first, _, _) = await TestApi.RegisterAndLogin(factory, "Participant");
        var (second, _, _) = await TestApi.RegisterAndLogin(factory, "Participant");
        var ev = await TestApi.CreateEvent(organiser);
        var category = await TestApi.CreateCategory(organiser, ev.Id);
        await TestApi.Enrol(first, ev.Id, category.Id);

        var response = await second.GetAsync("/api/enrolments/me");

        var mine = await response.Content.ReadFromJsonAsync<List<EnrolmentResponse>>();
        Assert.Empty(mine!);
    }

    [Fact]
    public async Task Enrol_TwiceInTheSameEvent_ReturnsConflict()
    {
        var (organiser, _, _) = await TestApi.RegisterAndLogin(factory, "Organiser");
        var (participant, _, _) = await TestApi.RegisterAndLogin(factory, "Participant");
        var ev = await TestApi.CreateEvent(organiser);
        var category = await TestApi.CreateCategory(organiser, ev.Id);
        await TestApi.Enrol(participant, ev.Id, category.Id);

        var response = await participant.PostAsJsonAsync("/api/enrolments",
            new EnrolmentRequest { EventId = ev.Id, CategoryId = category.Id });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_WithCategoryFromAnotherEvent_ReturnsBadRequest()
    {
        var (organiser, _, _) = await TestApi.RegisterAndLogin(factory, "Organiser");
        var (participant, _, _) = await TestApi.RegisterAndLogin(factory, "Participant");
        var eventOne = await TestApi.CreateEvent(organiser, "Event One");
        var eventTwo = await TestApi.CreateEvent(organiser, "Event Two");
        var categoryOfTwo = await TestApi.CreateCategory(organiser, eventTwo.Id);

        var response = await participant.PostAsJsonAsync("/api/enrolments",
            new EnrolmentRequest { EventId = eventOne.Id, CategoryId = categoryOfTwo.Id });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_InUnknownEvent_ReturnsNotFound()
    {
        var (participant, _, _) = await TestApi.RegisterAndLogin(factory, "Participant");

        var response = await participant.PostAsJsonAsync("/api/enrolments",
            new EnrolmentRequest { EventId = 999999, CategoryId = 1 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_WithoutLogin_ReturnsUnauthorized()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/enrolments",
            new EnrolmentRequest { EventId = 1, CategoryId = 1 });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanViewAllEnrolmentsForOwnEvent()
    {
        var (organiser, _, _) = await TestApi.RegisterAndLogin(factory, "Organiser");
        var (first, _, _) = await TestApi.RegisterAndLogin(factory, "Participant");
        var (second, _, _) = await TestApi.RegisterAndLogin(factory, "Participant");
        var ev = await TestApi.CreateEvent(organiser);
        var category = await TestApi.CreateCategory(organiser, ev.Id);
        await TestApi.Enrol(first, ev.Id, category.Id);
        await TestApi.Enrol(second, ev.Id, category.Id);

        var response = await organiser.GetAsync($"/api/events/{ev.Id}/enrolments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var all = await response.Content.ReadFromJsonAsync<List<EnrolmentResponse>>();
        Assert.Equal(2, all!.Count);
    }

    [Fact]
    public async Task Organiser_CannotViewEnrolmentsOfAnotherOrganisersEvent()
    {
        var (owner, _, _) = await TestApi.RegisterAndLogin(factory, "Organiser");
        var (otherOrganiser, _, _) = await TestApi.RegisterAndLogin(factory, "Organiser");
        var ev = await TestApi.CreateEvent(owner);

        var response = await otherOrganiser.GetAsync($"/api/events/{ev.Id}/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CanCancelOwnEnrolment()
    {
        var (organiser, _, _) = await TestApi.RegisterAndLogin(factory, "Organiser");
        var (participant, _, _) = await TestApi.RegisterAndLogin(factory, "Participant");
        var ev = await TestApi.CreateEvent(organiser);
        var category = await TestApi.CreateCategory(organiser, ev.Id);
        var enrolment = await TestApi.Enrol(participant, ev.Id, category.Id);

        var response = await participant.DeleteAsync($"/api/enrolments/{enrolment.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotCancelAnotherParticipantsEnrolment()
    {
        var (organiser, _, _) = await TestApi.RegisterAndLogin(factory, "Organiser");
        var (owner, _, _) = await TestApi.RegisterAndLogin(factory, "Participant");
        var (other, _, _) = await TestApi.RegisterAndLogin(factory, "Participant");
        var ev = await TestApi.CreateEvent(organiser);
        var category = await TestApi.CreateCategory(organiser, ev.Id);
        var enrolment = await TestApi.Enrol(owner, ev.Id, category.Id);

        var response = await other.DeleteAsync($"/api/enrolments/{enrolment.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
