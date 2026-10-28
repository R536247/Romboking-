# StudyRoom – Subapp 1 (ASP.NET Core 10 MVC)

ITPE3200 Web Applications, 2026H – Exam group project, Task 1: Study Room Booking Tool.

**Group members:** [NAME / STUDENT NUMBER], [NAME / STUDENT NUMBER], [NAME / STUDENT NUMBER]

Students book study rooms for study sessions (course, topic, time), others find and join them,
and participants talk through messages. The app prevents double bookings and other conflicts.

## Requirements

- **.NET SDK 10.0.x** (`dotnet --version`)
- **Node.js is not used in Subapp 1.**
- Internet access when viewing pages (Bootstrap, icons, jQuery validation and the font load from CDNs).

## How to run

```bash
cd Subapp1-MVC/StudyRoomBooking
dotnet run
```

Open **http://localhost:5080**. The SQLite database `StudyRoomBooking.db` is created on first start with
roles, demo users, rooms and sample sessions. To reset: stop the app, delete `StudyRoomBooking.db*`, run again.

## Demo accounts

| Role | Email | Password |
|---|---|---|
| Admin (also a student) | admin@studyroom.no | Admin1234 |
| Student | ingrid@studyroom.no | Student1234 |
| Student | amir@studyroom.no | Student1234 |

New users can also register (they get the Student role).

## How to run the tests

```bash
cd Subapp1-MVC
dotnet test
```

21 unit tests (xUnit + Moq): 8 positive and 13 negative, in `StudyRoomBooking.Tests/`.

## Requirement coverage

| Requirement | Where |
|---|---|
| 3+ domain entities with CRUD | `Room` (admin), `StudySession`, `Message`; plus `Student` profile (create/read/update) |
| Many-to-many | `Student` ↔ `StudySession` through `Participation` (composite key, `JoinedAt`) |
| Complex server-side domain rule | `Services/BookingService.cs` – no double booking, capacity, no overlapping participation, full/finished sessions, organizer cannot leave, ownership |
| Conditional rendering | `@if/else if` (join box, admin buttons, nav) and `? :` (badges, labels) in views |
| Multi-parameter search/filter | `StudySession/Index`: text, building, room, date, free spots, include finished (`SessionQuery`) |
| Repository pattern + DAL | `DAL/` – interface + implementation per entity, `AppDbContext`, `DBInit` |
| Async database access | All repository methods use `async`/`await` |
| ViewModels | `ViewModels/` – forms, filters, details, profile, login/register |
| Validation (client + server) | Data Annotations + `IValidatableObject` on ViewModels, jQuery unobtrusive validation, `ModelState` checks, BookingService rules |
| Error handling + logging | try/catch + `ILogger` in all repositories, controllers and services; global error and 404 pages; Serilog files in `Logs/` |
| Authentication + authorisation | ASP.NET Core Identity, roles Admin/Student, `[Authorize]`, ownership checks (organizer/author/admin) |
| Unit tests | 21 tests in `StudyRoomBooking.Tests/` |

## Project structure

| Folder | Content |
|---|---|
| `Models/` | Entities: `Student`, `Room`, `StudySession`, `Participation`, `Message`; `Roles` |
| `ViewModels/` | Data shaped for each view and form |
| `DAL/` | `AppDbContext`, repository interfaces/implementations, `SessionQuery`, `DBInit` |
| `Services/` | `BookingService` (domain rules), `ServiceResult`, `CurrentStudentService` |
| `Controllers/` | Home, Account, Profile, Room, StudySession, Message (+ `AppControllerBase`) |
| `Views/` | Razor views; `Shared/` has layout and partials |

## Known limitations (MVP)

- A session that has already started cannot be edited (the start time may not be in the past).
- The NuGet advisory GHSA-2m69-gcr7-jv3q in the SQLite native bundle is suppressed in the `.csproj`:
  no patched 2.x version exists, and the app runs locally without user-controlled SQL.

## Sources and references

- Microsoft Learn: ASP.NET Core MVC, Identity, EF Core, model validation, unit testing controllers.
- ITPE3200 course material and lecture demos.
- Bootstrap 5, Bootstrap Icons, jQuery Validation, Serilog, xUnit, Moq.
- An AI assistant (Claude, Anthropic) was used to help generate and explain code. All code was reviewed and tested by the group.
