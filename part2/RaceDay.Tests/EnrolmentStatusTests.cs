namespace RaceDay.Tests;

public class EnrolmentStatusTests(RaceDayFactory factory) : IClassFixture<RaceDayFactory>
{
    private async Task<(HttpClient Organiser, HttpClient Participant, EnrolmentResponse Enrolment)> SetUp()
    {
        var (organiser, _, _) = await TestApi.RegisterAndLogin(factory, "Organiser");
        var (participant, _, _) = await TestApi.RegisterAndLogin(factory, "Participant");
        var ev = await TestApi.CreateEvent(organiser);
        var category = await TestApi.CreateCategory(organiser, ev.Id);
        var enrolment = await TestApi.Enrol(participant, ev.Id, category.Id);
        return (organiser, participant, enrolment);
    }

    [Fact]
    public async Task NewEnrolment_StartsAsPending()
    {
        var (_, _, enrolment) = await SetUp();

        Assert.Equal("Pending", enrolment.Status);
    }

    [Fact]
    public async Task Organiser_CanConfirmEnrolment_AndParticipantSeesIt()
    {
        var (organiser, participant, enrolment) = await SetUp();

        var response = await organiser.PutAsJsonAsync($"/api/enrolments/{enrolment.Id}/status",
            new EnrolmentStatusRequest { Status = "Confirmed" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var mine = await participant.GetFromJsonAsync<List<EnrolmentResponse>>("/api/enrolments/me");
        Assert.Equal("Confirmed", Assert.Single(mine!).Status);
    }

    [Fact]
    public async Task Organiser_CanSetEnrolmentBackToPending()
    {
        var (organiser, _, enrolment) = await SetUp();
        await organiser.PutAsJsonAsync($"/api/enrolments/{enrolment.Id}/status", new EnrolmentStatusRequest { Status = "Confirmed" });

        var response = await organiser.PutAsJsonAsync($"/api/enrolments/{enrolment.Id}/status",
            new EnrolmentStatusRequest { Status = "pending" });

        var updated = await response.Content.ReadFromJsonAsync<EnrolmentResponse>();
        Assert.Equal("Pending", updated!.Status);
    }

    [Fact]
    public async Task InvalidStatus_ReturnsBadRequest()
    {
        var (organiser, _, enrolment) = await SetUp();

        var response = await organiser.PutAsJsonAsync($"/api/enrolments/{enrolment.Id}/status",
            new EnrolmentStatusRequest { Status = "Cancelled" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotChangeStatus_ReturnsForbidden()
    {
        var (_, participant, enrolment) = await SetUp();

        var response = await participant.PutAsJsonAsync($"/api/enrolments/{enrolment.Id}/status",
            new EnrolmentStatusRequest { Status = "Confirmed" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotChangeStatus_ForAnotherOrganisersEvent()
    {
        var (_, _, enrolment) = await SetUp();
        var (otherOrganiser, _, _) = await TestApi.RegisterAndLogin(factory, "Organiser");

        var response = await otherOrganiser.PutAsJsonAsync($"/api/enrolments/{enrolment.Id}/status",
            new EnrolmentStatusRequest { Status = "Confirmed" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task OrganiserEnrolmentList_ShowsStatusAndCategory()
    {
        var (organiser, _, enrolment) = await SetUp();

        var list = await organiser.GetFromJsonAsync<List<EnrolmentResponse>>($"/api/events/{enrolment.EventId}/enrolments");

        var found = Assert.Single(list!);
        Assert.Equal("Pending", found.Status);
        Assert.Equal(enrolment.CategoryName, found.CategoryName);
    }
}
