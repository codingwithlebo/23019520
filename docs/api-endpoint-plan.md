# RaceDay System — API Endpoint Plan

|HTTP Method|Route|Description|Role Required|Request Body|Expected Response|
|-|-|-|-|-|-|
|POST|/api/auth/register|Register a new account|Public|Email, Password, FullName, Role|201 Created + user info|
|POST|/api/auth/login|Authenticate and get a token|Public|Email, Password|200 OK + JWT token|
|GET|/api/users/{id}/profile|View a user's profile|Authenticated (self)|—|200 OK + profile|
|PUT|/api/users/{id}/profile|Update own profile|Authenticated (self)|FullName, Phone, Bio|200 OK + updated profile|
|GET|/api/events|List all events|Public|—|200 OK + list of events|
|GET|/api/events/{id}|Get one event's details|Public|—|200 OK + event|
|POST|/api/events|Create a new event|Organiser|Name, Description, Date, Location|201 Created + event|
|PUT|/api/events/{id}|Update an event|Organiser (owner)|Name, Description, Date, Location|200 OK + updated event|
|DELETE|/api/events/{id}|Delete an event|Organiser (owner)|—|204 No Content|
|GET|/api/events/{eventId}/categories|List categories for an event|Public|—|200 OK + list|
|POST|/api/events/{eventId}/categories|Add a category to an event|Organiser|Name, Description|201 Created + category|
|PUT|/api/categories/{id}|Update a category|Organiser|Name, Description|200 OK + updated category|
|DELETE|/api/categories/{id}|Delete a category|Organiser|—|204 No Content|
|POST|/api/enrolments|Enrol in an event category|Participant|EventId, CategoryId|201 Created + enrolment|
|GET|/api/enrolments/me|View own enrolments|Participant|—|200 OK + list|
|GET|/api/events/{eventId}/enrolments|View all enrolments for an event|Organiser|—|200 OK + list|
|DELETE|/api/enrolments/{id}|Cancel own enrolment|Participant (owner)|—|204 No Content|
|POST|/api/results|Capture a result|Organiser|EnrolmentId, FinishTime, Position, Points|201 Created + result|
|PUT|/api/results/{id}|Update a result|Organiser|FinishTime, Position, Points|200 OK + updated result|
|GET|/api/results/me|View own results|Participant|—|200 OK + list|
|GET|/api/events/{eventId}/results|View all results for an event|Public/Organiser|—|200 OK + list|
|## Role Definitions<br /><br />\| Role \| Responsibilities \|<br />\|---\|---\|<br />\| Organiser \| Create and manage events, manage event categories, view enrolments, and capture or update race results. \|<br />\| Participant \| Register for events, manage their profile, view their enrolments, and view their race results. \|<br />\| Public \| Register or log in and view publicly available events, categories, and results. \|||||||



