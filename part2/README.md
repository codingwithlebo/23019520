# RaceDay API — Part 2

RESTful API for the RaceDay event system, built with ASP.NET Core Web API, Entity Framework Core (code-first) and SQL Server. It implements the endpoint plan in [`docs/api-endpoint-plan.md`](../docs/api-endpoint-plan.md).

## The two roles

- **Organiser** can create, edit and delete their own events, define categories for them, capture finish times and positions, and view all enrolments for their events.
- **Participant** can register, browse events and categories, enter an event by selecting a category, view their own enrolments, and track their own results.

Role-based access is enforced on the server with `[Authorize(Roles = ...)]`: an Organiser gets **403 Forbidden** on Participant endpoints and vice versa, and anonymous requests to non-public endpoints get **401 Unauthorized**. Organisers can only change events (and their categories, enrolments and results) that they own.

## Authentication

- **Session-based.** `POST /api/auth/login` issues an HttpOnly session cookie holding the user's id and role. The cookie is sent automatically on later requests, and `POST /api/auth/logout` ends the session.
- **Passwords are hashed** with salted PBKDF2 (SHA-256, 100 000 iterations). The plain-text password is never stored.
- **Organiser registration needs an invite code** (`Registration:OrganiserCode` in `appsettings.json`, default `RACEDAY-ORG-2026`), so nobody can simply pick the Organiser role. Participants register freely.

## Setup and run

Prerequisites: Visual Studio 2022 (ASP.NET workload), .NET 8 SDK, SQL Server (LocalDB or SQL Server Express).

1. Clone the repository and open `part2/RaceDay.sln`.
2. In `part2/RaceDay.Api/appsettings.json`, check `ConnectionStrings:RaceDayDb` points at your SQL Server instance. The API uses its own database, `RaceDayApiDb`, created by EF Core. The Part 1 script `docs/raceday-schema.sql` creates a separate `RaceDayDB` for the SSMS demo.
3. Create the database from the code-first model. In Visual Studio open **Tools > NuGet Package Manager > Package Manager Console**, set the default project to `RaceDay.Api` and run:
   ```
   Add-Migration InitialCreate
   Update-Database
   ```
   (The API also applies pending migrations when it starts.)
4. Press **F5**. Swagger UI opens at `http://localhost:5080/swagger`.

### Trying it in Swagger

1. `POST /api/auth/register` to create a Participant, or an Organiser using the invite code.
2. `POST /api/auth/login`. The session cookie is stored by the browser.
3. Call any endpoint with **Try it out**. Each one shows its purpose, the data it expects and the responses it returns.
4. `POST /api/auth/logout` to end the session, then log in as the other role to see role-based access.

## Unit tests

```
dotnet test part2/RaceDay.sln
```

or **Test > Run All Tests** in Visual Studio. The tests start the real API in memory with an in-memory database and cover, for both success and failure cases:

- registration and login (duplicate email, wrong password, weak password, wrong organiser code, hashed passwords, logout)
- event management by Organisers (create, update, delete, validation, ownership)
- role-based access (Participants rejected from Organiser functions, Organisers rejected from Participant functions, anonymous requests rejected)
- event enrolments (Participants can enrol and the participant/event/category link is recorded, duplicates and mismatched categories rejected)
- results and profiles

## CI/CD

GitHub Actions (`.github/workflows/part2-ci.yml`) restores, builds and runs all tests on every push.

![Green build](docs/ci-green-build.png)

## Changes from the Part 1 plan

Documented in full at the bottom of [`docs/api-endpoint-plan.md`](../docs/api-endpoint-plan.md):

1. Event gained `DistanceKm` and `EventType` (Run/Walk/Cycle), required by the Part 2 brief. ERD and SQL script updated to match.
2. Session (cookie) authentication, with an added `POST /api/auth/logout` endpoint.
3. Organiser registration protected by an invite code.
4. Results for an event require login.
5. The user entity class is called `AppUser` in C# (to avoid a clash with `ControllerBase.User`) but maps to the `User` table from the ERD.

## Video

YouTube (unlisted): _add link here_

## Project structure

```
part2/
  RaceDay.Api/        Controllers, Models, Data (DbContext), Dtos, Services, Program.cs
  RaceDay.Tests/      xUnit tests (WebApplicationFactory + in-memory database)
  RaceDay.sln
```

## Troubleshooting

- If the API cannot connect to SQL Server, verify the ConnectionStrings:RaceDayDb value in appsettings.json and confirm that the database server is running.
- If an endpoint returns 401 Unauthorized, log in and check that the session cookie is being sent.
- If an endpoint returns 403 Forbidden, check that the logged-in account has the required role and resource ownership.
- If tests fail, run dotnet test part2/RaceDay.sln and inspect the first failing test and its error message.

## Common HTTP Response Codes

- **200 OK:** The request completed successfully.
- **201 Created:** A new resource was created successfully.
- **400 Bad Request:** The request contains invalid data or is incorrectly formatted.
- **401 Unauthorized:** Authentication is missing or invalid.
- **403 Forbidden:** The user is authenticated but lacks the required permission.
- **404 Not Found:** The requested resource or endpoint could not be found.
- **500 Internal Server Error:** An unexpected error occurred on the server.

## API Testing Checklist

Before submitting or demonstrating the API, verify the following:

- [ ] The application starts successfully.
- [ ] The database connection works and migrations are applied.
- [ ] Swagger loads and displays the available endpoints.
- [ ] Registration and login work with valid input.
- [ ] Invalid input is handled appropriately.
- [ ] Protected endpoints reject unauthenticated requests.
- [ ] Role-restricted endpoints enforce the required permissions.
- [ ] Automated tests pass using `dotnet test part2/RaceDay.sln`.

## Configuration

The API reads its application settings from `RaceDay.Api/appsettings.json`.

- **ConnectionStrings:RaceDayDb:** Configures the SQL Server database connection. The default development configuration uses SQL Server LocalDB.
- **Registration:OrganiserCode:** Configures the code required for organiser registration. Keep this value private and replace it with an appropriate value for each environment.
- **Logging:LogLevel:** Controls the minimum log levels for the application and ASP.NET Core.
- **AllowedHosts:** Specifies which host names the application accepts.

### Configuration and Security Notes

- Update the database connection string to match the environment where the API runs.
- Do not commit real passwords, production credentials, or private organiser codes to source control.
- Use environment variables or an appropriate secrets manager for sensitive deployment settings.
- The API uses cookie-based authentication. After logging in, clients must send the session cookie when accessing protected endpoints.
