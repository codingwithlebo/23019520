namespace RaceDay.Tests;

public class AuthTests(RaceDayFactory factory) : IClassFixture<RaceDayFactory>
{
    [Fact]
    public async Task Register_Participant_ReturnsCreated()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", TestApi.NewRegistration("Participant"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.Equal("Participant", body!.Role);
    }

    [Fact]
    public async Task Register_Organiser_WithValidCode_ReturnsCreated()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", TestApi.NewRegistration("Organiser"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.Equal("Organiser", body!.Role);
    }

    [Fact]
    public async Task Register_Organiser_WithWrongCode_ReturnsForbidden()
    {
        var client = factory.CreateClient();
        var registration = TestApi.NewRegistration("Organiser");
        registration.OrganiserCode = "not-the-code";

        var response = await client.PostAsJsonAsync("/api/auth/register", registration);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Register_Organiser_WithoutCode_ReturnsForbidden()
    {
        var client = factory.CreateClient();
        var registration = TestApi.NewRegistration("Organiser");
        registration.OrganiserCode = null;

        var response = await client.PostAsJsonAsync("/api/auth/register", registration);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        var client = factory.CreateClient();
        var email = TestApi.NewEmail();

        var first = await client.PostAsJsonAsync("/api/auth/register", TestApi.NewRegistration("Participant", email));
        var second = await client.PostAsJsonAsync("/api/auth/register", TestApi.NewRegistration("Participant", email));

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Register_ShortPassword_ReturnsBadRequest()
    {
        var client = factory.CreateClient();
        var registration = TestApi.NewRegistration("Participant");
        registration.Password = "short";

        var response = await client.PostAsJsonAsync("/api/auth/register", registration);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_UnknownRole_ReturnsBadRequest()
    {
        var client = factory.CreateClient();
        var registration = TestApi.NewRegistration("Participant");
        registration.Role = "Admin";

        var response = await client.PostAsJsonAsync("/api/auth/register", registration);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_StoresHashedPassword_NotThePlainText()
    {
        var client = factory.CreateClient();
        var registration = TestApi.NewRegistration("Participant");
        await client.PostAsJsonAsync("/api/auth/register", registration);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        var user = await db.Users.SingleAsync(u => u.Email == registration.Email);

        Assert.NotEqual(registration.Password, user.PasswordHash);
        Assert.DoesNotContain(registration.Password, user.PasswordHash);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOk()
    {
        var client = factory.CreateClient();
        var registration = TestApi.NewRegistration("Participant");
        await client.PostAsJsonAsync("/api/auth/register", registration);

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = registration.Email, Password = registration.Password });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();
        var registration = TestApi.NewRegistration("Participant");
        await client.PostAsJsonAsync("/api/auth/register", registration);

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = registration.Email, Password = "wrong-password" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownEmail_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = TestApi.NewEmail(), Password = TestApi.Password });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutLogin_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/enrolments/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_AfterLogin_ReturnsOk()
    {
        var (client, _, _) = await TestApi.RegisterAndLogin(factory, "Participant");

        var response = await client.GetAsync("/api/enrolments/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Logout_EndsTheSession()
    {
        var (client, _, _) = await TestApi.RegisterAndLogin(factory, "Participant");

        var logout = await client.PostAsync("/api/auth/logout", null);
        var afterLogout = await client.GetAsync("/api/enrolments/me");

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, afterLogout.StatusCode);
    }
}
