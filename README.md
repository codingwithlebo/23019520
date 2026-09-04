# RaceDay System

A race event management system supporting event organisers and participants, built as part of PROG6212 POE (Part 1: System Planning and Database).

## Part 1 — System Planning and Database

This part covers the planning phase only. No API code is included yet — that begins in Part 2.

### Contents

- `docs/raceday_erd.png` — Entity Relationship Diagram (6 entities: User, UserProfile, Event, Category, Enrolment, Result)
- `docs/api-endpoint-plan.md` — Full REST API endpoint plan covering Authentication, User Profile, Events, Categories, Enrolments, and Results
- `docs/raceday-schema.sql` — SQL script to create and populate the database schema, matching the ERD exactly

### Setup and run instructions (database)

1. Open SQL Server Management Studio (SSMS).
2. Connect to your local SQL Server instance.
3. Open `docs/raceday-schema.sql`.
4. Execute the script — this creates the `RaceDayDB` database, all six tables, and inserts sample data.
5. Verify by running `SELECT * FROM [User];` and checking the other tables populate correctly.

## Roles

The system supports two distinct user roles:

- **Organiser** — can create, edit, and delete events, manage event categories, capture participant results, and view all event enrolments.
- **Participant** — can create an account, browse events, enrol in an event by selecting a category, view their own enrolments, and track their personal results.

Role-based access will be enforced at the API level in Part 2 and reflected consistently in the MVC interface in Part 3.

## Notes

- Part 2 will add the RESTful API in C#, connected to this database, with unit tests and GitHub Actions CI/CD.
- Part 3 will add the MVC web application, Azure Blob Storage integration, and Docker containerisation.
