namespace RaceDay.Tests;

/// <summary>Small helpers that talk to the API the way a real client would.</summary>
public static class TestApi
{
    public const string Password = "Passw0rd!123";

    public static string NewEmail() => $"{Guid.NewGuid():N}@raceday.test";

    public static RegisterRequest NewRegistration(string role, string? email = null) => new()
    {
        Email = email ?? NewEmail(),
        Password = Password,
        FullName = $"Test {role}",
        Role = role,
        OrganiserCode = role == "Organiser" ? RaceDayFactory.OrganiserCode : null
    };

    /// <summary>Registers a new user and logs in. The returned client carries the session cookie.</summary>
    public static async Task<(HttpClient Client, int UserId, string Email)> RegisterAndLogin(RaceDayFactory factory, string role)
    {
        var client = factory.CreateClient();
        var registration = NewRegistration(role);

        var register = await client.PostAsJsonAsync("/api/auth/register", registration);
        register.EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = registration.Email, Password = registration.Password });
        login.EnsureSuccessStatusCode();

        var user = await login.Content.ReadFromJsonAsync<AuthResponse>();
        return (client, user!.Id, registration.Email);
    }

    public static EventRequest NewEventRequest(string name = "City Run", string type = "Run") => new()
    {
        Name = name,
        Description = "A test event",
        Date = DateTime.UtcNow.AddDays(30),
        Location = "Johannesburg",
        DistanceKm = 10m,
        EventType = type
    };

    public static async Task<EventResponse> CreateEvent(HttpClient organiser, string name = "City Run", string type = "Run")
    {
        var response = await organiser.PostAsJsonAsync("/api/events", NewEventRequest(name, type));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EventResponse>())!;
    }

    public static async Task<CategoryResponse> CreateCategory(HttpClient organiser, int eventId, string name = "10km")
    {
        var response = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories",
            new CategoryRequest { Name = name, Description = "Test category" });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CategoryResponse>())!;
    }

    public static async Task<EnrolmentResponse> Enrol(HttpClient participant, int eventId, int categoryId)
    {
        var response = await participant.PostAsJsonAsync("/api/enrolments",
            new EnrolmentRequest { EventId = eventId, CategoryId = categoryId });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EnrolmentResponse>())!;
    }
}
