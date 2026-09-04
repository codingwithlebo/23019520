# RaceDay System

A race event management system supporting event organisers and participants, built as part of PROG6212 POE (Part 1: System Planning and Database).

## Part 1 — System Planning and Database

This part covers the planning phase and database design for the RaceDay System. The REST API and web application will be implemented in later parts of the project.

### Contents

* `docs/raceday_erd.png` — Entity Relationship Diagram containing 6 entities: User, UserProfile, Event, Category, Enrolment, and Result.
* `docs/api-endpoint-plan.md` — REST API endpoint plan covering Authentication, User Profile, Events, Categories, Enrolments, and Results.
* `docs/raceday-schema.sql` — SQL script to create and populate the RaceDay database schema.

## Project Structure

```text
23019520/
├── .github/
│   └── workflows/
│       └── validate-structure.yml
├── docs/
│   ├── raceday_erd.png
│   ├── api-endpoint-plan.md
│   ├── raceday-schema.sql
│   └── github-actions-success.png
├── .gitignore
└── README.md
```

The `/docs` directory contains the main Part 1 planning and database deliverables. The GitHub Actions workflow automatically validates that the required project files are present.

## Setup and Run Instructions (Database)

1. Open SQL Server Management Studio (SSMS).
2. Connect to the local SQL Server instance.
3. Open `docs/raceday-schema.sql`.
4. Execute the script.
5. The script creates the `RaceDayDB` database, all six tables, constraints, indexes, and sample data.
6. Verify the database by running:

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

The system supports two main user roles:

* **Organiser** — can create, edit, and delete events, manage event categories, capture participant results, and view event enrolments.
* **Participant** — can create an account, browse events, enrol in an event category, view their enrolments, and track their personal results.

Public users can register, log in, and view publicly available event information.

Role-based access will be enforced at the API level in Part 2 and reflected consistently in the MVC interface in Part 3.

## API Planning

The REST API has been planned before implementation to provide a clear structure for Part 2 development.

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

## ERD Design Decisions

The RaceDay ERD was designed around the main relationships required by the race event management system.

* **User → UserProfile:** A user can have one profile containing additional personal information.
* **User → Event:** An organiser can create and manage multiple events, while each event has one organiser.
* **Event → Category:** An event can contain multiple race categories, while each category belongs to one event.
* **User → Enrolment:** A participant can have multiple enrolments across events.
* **Event → Enrolment:** An event can have multiple participant enrolments.
* **Category → Enrolment:** Each enrolment is associated with a specific event category.
* **Enrolment → Result:** An enrolment can have a race result recorded after the participant completes the event.

Primary keys uniquely identify records in each entity, while foreign keys maintain relationships between related entities and help preserve referential integrity.

The design separates users, profiles, events, categories, enrolments, and results into independent entities to reduce data duplication and make the database easier to maintain and extend.

## GitHub Actions CI/CD

A GitHub Actions workflow is included to automatically validate the repository structure.

The workflow checks that:

* The `/docs` directory exists.
* The ERD file exists.
* The API endpoint plan exists.
* The SQL schema exists.
* The README exists.
* The SQL script contains content.
* The API endpoint plan contains content.
* The ERD file contains content.

Workflow file:

`.github/workflows/validate-structure.yml`

The latest GitHub Actions validation completed successfully, demonstrating that the required repository structure passed the automated checks.

### Successful GitHub Actions Build

![Successful GitHub Actions build](docs/github-actions-success.png)

## Video Demonstration

The following unlisted YouTube video demonstrates the RaceDay Part 1 planning and database implementation, including the database design and SQL Server Management Studio demonstration.

[Watch the RaceDay System demonstration](https://youtu.be/MgApJHHrRmg)

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
* Successful CI/CD validation
* Video demonstration

The implementation of the API and web application will follow the designs documented in this Part 1 repository.
