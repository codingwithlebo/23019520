namespace RaceDay.Tests;

public class ResultTests(RaceDayFactory factory) : IClassFixture<RaceDayFactory>
{
    private async Task<(HttpClient Organiser, HttpClient Participant, EnrolmentResponse Enrolment, EventResponse Event)> SetUpEnrolment()
    {
        var (organiser, _, _) = await TestApi.RegisterAndLogin(factory, "Organiser");
        var (participant, _, _) = await TestApi.RegisterAndLogin(factory, "Participant");
        var ev = await TestApi.CreateEvent(organiser);
        var category = await TestApi.CreateCategory(organiser, ev.Id);
        var enrolment = await TestApi.Enrol(participant, ev.Id, category.Id);
        return (organiser, participant, enrolment, ev);
    }

    [Fact]
    public async Task Organiser_CanCaptureResult_AndParticipantCanViewIt()
    {
        var (organiser, participant, enrolment, _) = await SetUpEnrolment();

        var capture = await organiser.PostAsJsonAsync("/api/results", new ResultRequest
        {
            EnrolmentId = enrolment.Id,
            FinishTime = "00:45:12",
            Position = 3,
            Points = 90
        });

        Assert.Equal(HttpStatusCode.Created, capture.StatusCode);

        var mine = await participant.GetAsync("/api/results/me");
        Assert.Equal(HttpStatusCode.OK, mine.StatusCode);
        var results = await mine.Content.ReadFromJsonAsync<List<ResultResponse>>();
        var result = Assert.Single(results!);
        Assert.Equal("00:45:12", result.FinishTime);
        Assert.Equal(3, result.Position);
    }

    [Fact]
    public async Task CaptureResult_Twice_ReturnsConflict()
    {
        var (organiser, _, enrolment, _) = await SetUpEnrolment();
        var request = new ResultRequest { EnrolmentId = enrolment.Id, FinishTime = "00:40:00", Position = 1 };
        await organiser.PostAsJsonAsync("/api/results", request);

        var second = await organiser.PostAsJsonAsync("/api/results", request);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_InvalidPosition_ReturnsBadRequest()
    {
        var (organiser, _, enrolment, _) = await SetUpEnrolment();

        var response = await organiser.PostAsJsonAsync("/api/results",
            new ResultRequest { EnrolmentId = enrolment.Id, FinishTime = "00:40:00", Position = 0 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_UnknownEnrolment_ReturnsNotFound()
    {
        var (organiser, _, _) = await TestApi.RegisterAndLogin(factory, "Organiser");

        var response = await organiser.PostAsJsonAsync("/api/results",
            new ResultRequest { EnrolmentId = 999999, FinishTime = "00:40:00", Position = 1 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotCaptureResult_ForAnotherOrganisersEvent()
    {
        var (_, _, enrolment, _) = await SetUpEnrolment();
        var (otherOrganiser, _, _) = await TestApi.RegisterAndLogin(factory, "Organiser");

        var response = await otherOrganiser.PostAsJsonAsync("/api/results",
            new ResultRequest { EnrolmentId = enrolment.Id, FinishTime = "00:40:00", Position = 1 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanUpdateResult()
    {
        var (organiser, _, enrolment, _) = await SetUpEnrolment();
        var capture = await organiser.PostAsJsonAsync("/api/results",
            new ResultRequest { EnrolmentId = enrolment.Id, FinishTime = "00:50:00", Position = 5 });
        var created = await capture.Content.ReadFromJsonAsync<ResultResponse>();

        var update = await organiser.PutAsJsonAsync($"/api/results/{created!.Id}",
            new ResultUpdateRequest { FinishTime = "00:48:30", Position = 4, Points = 70 });

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var updated = await update.Content.ReadFromJsonAsync<ResultResponse>();
        Assert.Equal("00:48:30", updated!.FinishTime);
        Assert.Equal(4, updated.Position);
    }

    [Fact]
    public async Task Participant_OnlySeesOwnResults()
    {
        var (organiser, _, enrolment, _) = await SetUpEnrolment();
        await organiser.PostAsJsonAsync("/api/results",
            new ResultRequest { EnrolmentId = enrolment.Id, FinishTime = "00:55:00", Position = 8 });
        var (otherParticipant, _, _) = await TestApi.RegisterAndLogin(factory, "Participant");

        var response = await otherParticipant.GetAsync("/api/results/me");

        var results = await response.Content.ReadFromJsonAsync<List<ResultResponse>>();
        Assert.Empty(results!);
    }

    [Fact]
    public async Task LoggedInUser_CanViewResultsForAnEvent()
    {
        var (organiser, participant, enrolment, ev) = await SetUpEnrolment();
        await organiser.PostAsJsonAsync("/api/results",
            new ResultRequest { EnrolmentId = enrolment.Id, FinishTime = "00:41:00", Position = 1 });

        var response = await participant.GetAsync($"/api/events/{ev.Id}/results");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var results = await response.Content.ReadFromJsonAsync<List<ResultResponse>>();
        Assert.Single(results!);
    }
}
