# RaceDay System — API Endpoint Plan (revision 3)

Authentication uses **cookie-based sessions**: `POST /api/auth/login` creates a session cookie carrying the user's id and role, and every later request is authorised from that session. Passwords are stored as salted PBKDF2 hashes, never in plain text.

Standard error responses apply to every endpoint: **400** validation failure, **401** not logged in, **403** logged in with the wrong role (or not the owner of the resource), **404** resource not found, **409** conflict (duplicate).

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/auth/register | Register a new account. Participant is open to anyone; the Organiser role needs a valid organiser invite code | Public | Email, Password, FullName, Phone (optional), Role, OrganiserCode (Organiser only) | 201 Created + user info. 409 if email exists, 403 if organiser code is wrong |
| POST | /api/auth/login | Log in and start a session | Public | Email, Password | 200 OK + user info (session cookie set). 401 if credentials are wrong |
| POST | /api/auth/logout | End the current session | Authenticated | — | 204 No Content |
| GET | /api/users/{id}/profile | View own profile | Authenticated (self) | — | 200 OK + profile. 403 if {id} is not the caller |
| PUT | /api/users/{id}/profile | Update own profile | Authenticated (self) | FullName, Phone, Bio | 200 OK + updated profile |
| POST | /api/users/{id}/profile/picture | Upload or replace own profile picture (stored in Azure Blob Storage by the API) | Authenticated (self) | multipart/form-data, field `file` (JPG/PNG/GIF/WebP, max 5 MB) | 200 OK + profile with ProfilePictureUrl. 400 if not an image |
| GET | /api/users/{id}/profile/picture | Get a profile picture | Authenticated | — | 200 OK + image. 404 if none |
| GET | /api/events | List events. Optional filters: `type` (Run/Walk/Cycle), `search` (name or location), `minDistanceKm`, `maxDistanceKm`, `upcomingOnly` | Public | — | 200 OK + list of events (each with a BannerUrl when one exists) |
| GET | /api/events/mine | List the Organiser's own events with the number of enrolments for each (dashboard) | Organiser | — | 200 OK + list of events with EnrolmentCount |
| GET | /api/events/{id} | Get one event's details | Public | — | 200 OK + event. 404 if missing |
| POST | /api/events | Create a new event | Organiser | Name, Description, Date, Location, DistanceKm, EventType (Run/Walk/Cycle) | 201 Created + event |
| PUT | /api/events/{id} | Update an event | Organiser (owner) | Name, Description, Date, Location, DistanceKm, EventType | 200 OK + updated event |
| DELETE | /api/events/{id} | Delete an event | Organiser (owner) | — | 204 No Content |
| POST | /api/events/{id}/banner | Upload or replace the event banner image (stored in Azure Blob Storage by the API) | Organiser (owner) | multipart/form-data, field `file` (JPG/PNG/GIF/WebP, max 5 MB) | 200 OK + event with BannerUrl. 400 if not an image |
| GET | /api/events/{id}/banner | Get the event banner image | Public | — | 200 OK + image. 404 if none |
| GET | /api/events/{eventId}/categories | List categories for an event | Public | — | 200 OK + list |
| POST | /api/events/{eventId}/categories | Add a category to an event | Organiser (owner of event) | Name, Description | 201 Created + category |
| PUT | /api/categories/{id} | Update a category | Organiser (owner of event) | Name, Description | 200 OK + updated category |
| DELETE | /api/categories/{id} | Delete a category | Organiser (owner of event) | — | 204 No Content. 409 if participants are enrolled in it |
| POST | /api/enrolments | Enrol in an event by selecting a category | Participant | EventId, CategoryId | 201 Created + enrolment. 400 if the category is not part of the event, 409 if already enrolled |
| GET | /api/enrolments/me | View own enrolments | Participant | — | 200 OK + list |
| GET | /api/events/{eventId}/enrolments | View all enrolments for an event | Organiser (owner of event) | — | 200 OK + list |
| PUT | /api/enrolments/{id}/status | Confirm an enrolment or set it back to Pending | Organiser (owner of event) | Status (Pending/Confirmed) | 200 OK + updated enrolment. 400 if the status is invalid |
| DELETE | /api/enrolments/{id} | Cancel own enrolment | Participant (owner) | — | 204 No Content |
| POST | /api/results | Capture a result for an enrolment | Organiser (owner of event) | EnrolmentId, FinishTime, Position, Points (optional) | 201 Created + result. 409 if a result already exists |
| PUT | /api/results/{id} | Update a result | Organiser (owner of event) | FinishTime, Position, Points (optional) | 200 OK + updated result |
| GET | /api/results/me | View own results: event name and date, category, finish time, position and total finishers (for "47th of 312") | Participant | — | 200 OK + list |
| GET | /api/events/{eventId}/results | View all results for an event | Authenticated | — | 200 OK + list |

## Changes since revision 1

Revision 1 was written before the Part 2 requirements were fully mapped. Revision 2 corrects these points, and the implementation matches this revision:

1. **Event gains DistanceKm and EventType (Run/Walk/Cycle).** The Part 2 brief requires every event to capture distance and event type. The ERD and SQL script were updated to match.
2. **Session authentication instead of JWT.** The brief asks for session management that keeps the user's authenticated state and role across requests, so login now sets a session cookie.
3. **Added `POST /api/auth/logout`.** A session needs a way to end.
4. **Organiser registration is protected by an invite code.** Letting anyone self-select Organiser would defeat role-based access. Participants register freely.
5. **Results for an event require login.** Revision 1 listed this as Public/Organiser. Results belong to named participants, so any authenticated user may view them.
6. **Error responses documented**, including 409 for duplicate enrolments and a unique constraint on (Participant, Event) in the database.

## Changes in revision 3 (Part 3: MVC front end, Azure Blob Storage)

The Part 3 brief adds requirements the earlier plan did not cover. The API was extended, and the ERD and SQL script were updated to match:

1. **Enrolment status.** Enrolments now carry a `Status` (Pending or Confirmed). New enrolments start as Pending, and the Organiser who owns the event confirms them with `PUT /api/enrolments/{id}/status`.
2. **Image uploads through the API.** The MVC application must never talk to Azure, so the API stores images in Azure Blob Storage and serves them back. `Event.BannerBlobName` and `UserProfile.ProfilePictureBlobName` hold the blob names, and the new upload and download endpoints are listed above. The container is private, so images are only reachable through the API.
3. **Richer results.** Results now include the event date, the category and the total number of finishers, so the Participant's page can show "47th out of 312".
4. **Organiser dashboard.** `GET /api/events/mine` returns the Organiser's events with enrolment counts.
5. **Event filtering.** `GET /api/events` gained search, distance and upcoming-only filters for the Participant home page.

## Assumptions

- Participants can only view and cancel their own enrolments.
- An organiser can only change events, categories and results that belong to their own events.
- A participant can enter a given event once, in one category, and the category must belong to that event.
