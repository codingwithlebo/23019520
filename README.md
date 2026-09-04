# RaceDay System

A race event management system supporting event organisers and participants, built as part of PROG6212 POE (Part 1: System Planning and Database).

## Part 1 — System Planning and Database

This part covers the planning phase only. No API code is included yet — that begins in Part 2.

### Contents

* `docs/raceday_erd.png` — Entity Relationship Diagram containing 6 entities: User, UserProfile, Event, Category, Enrolment, and Result.
* `docs/api-endpoint-plan.md` — Full REST API endpoint plan covering Authentication, User Profile, Events, Categories, Enrolments, and Results.
* `docs/raceday-schema.sql` — SQL script to create and populate the database schema, matching the ERD.

## Project Structure

```text
23019520/
├── .github/
│   └── workflows/
│       └── validate-structure.yml
├── docs/
│   ├── raceday_erd.png
│   ├── api-endpoint-plan.md
│   └── raceday-schema.sql
├── .gitignore
└── README.md
```

The `/docs` directory contains the main Part 1 planning and database deliverables. The GitHub Actions workflow automatically validates that the required project files are present.

## Setup and Run Instructions (Database)

1. Open SQL Server Management Studio (SSMS).
2. Connect to your local SQL Server instance.
3. Open `docs/raceday-schema.sql`.
4. Execute the script. This creates the `RaceDayDB` database, all six tables, and inserts sample data.
5. Verify the database by running:

```sql
SELECT * FROM [User];
SELECT * FROM UserProfile;
SELECT * FROM Event;
SELECT * FROM Category;
SELECT * FROM Enrolment;
SELECT * FROM Result;
```

The sample data should populate the tables correctly.

## Roles

The system supports two distinct user roles:

* **Organiser** — can create, edit, and delete events, manage event categories, capture participant results, and view all event enrolments.
* **Participant** — can create an account, browse events, enrol in an event by selecting a category, view their own enrolments, and track their personal results.

Role-based access will be enforced at the API level in Part 2 and reflected consistently in the MVC interface in Part 3.

## API Planning

The REST API has been planned before implementation to ensure that the Part 2 development follows a clear structure.

The endpoint plan covers:

* Authentication — registration and login
* User profiles — viewing and updating profiles
* Events — creating, viewing, updating, and deleting events
* Categories — managing race categories
* Enrolments — registering participants for event categories
* Results — capturing and viewing race results
* Role-based access control
* Common API error responses

The complete endpoint specification is available in:

`docs/api-endpoint-plan.md`

## Database Design

The RaceDay database contains six main entities:

1. **User** — stores account and authentication information.
2. **UserProfile** — stores additional participant or organiser profile information.
3. **Event** — stores race event information and identifies the organiser.
4. **Category** — stores categories available within an event.
5. **Enrolment** — records participants enrolled in event categories.
6. **Result** — stores the results achieved by enrolled participants.

The Entity Relationship Diagram is available in:

`docs/raceday_erd.png`

The database schema and sample data are available in:

`docs/raceday-schema.sql`

## GitHub Actions CI/CD

A GitHub Actions workflow is included to validate the repository structure automatically.

The workflow checks that:

* The `/docs` directory exists.
* The ERD file exists.
* The API endpoint plan exists.
* The SQL schema exists.
* The README exists.
* The SQL script contains content.
* The API endpoint plan contains content.

Workflow file:

`.github/workflows/validate-structure.yml`

A successful workflow run demonstrates that the required Part 1 repository structure has passed automated validation.

<!-- Add the successful GitHub Actions screenshot here before final submission. -->

## Future Development

### Part 2 — RESTful API

Part 2 will add the RESTful API in C#, connected to the RaceDay database.

Planned features include:

* Authentication
* JWT-based authorisation
* Role-based access control
* Event management
* Category management
* Event enrolments
* Race results
* Unit testing
* GitHub Actions CI/CD

### Part 3 — MVC Web Application

Part 3 will add the MVC web application with:

* Organiser and participant interfaces
* Role-based functionality
* Azure Blob Storage integration
* Docker containerisation

## Part 1 Deliverables

The repository contains the required Part 1 planning and database deliverables:

* Entity Relationship Diagram (ERD)
* REST API endpoint specification
* SQL database creation and population script
* README documentation
* GitHub Actions repository validation workflow

The implementation of the API and web application will follow the designs documented in this Part 1 repository.
