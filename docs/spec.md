## Project 3 specification — staff management system

**Purpose:** turn the C# coursework into something a reviewer can clone, run and
test — and give yourself evidence for the C#/.NET adverts (Man Group's C#/.NET
pathway, CGI, Elwood).

**Stack:** C# / .NET 8, ASP.NET Core (MVC or minimal API + Razor pages), EF Core,
SQL Server in Docker for production-like runs (SQLite allowed for tests), xUnit +
`WebApplicationFactory`, GitHub Actions (`dotnet test`), Dockerfile.

**Features**

| Area | Requirement |
| --- | --- |
| Staff records | Create, edit, delete, list with search, filter and paging |
| Validation | Data annotations plus server-side checks; friendly error messages |
| Auth | Login with hashed passwords; roles: Admin, Manager, Viewer |
| Authorisation | Admins manage users; Managers edit staff; Viewers read-only |
| Database | EF Core migrations, foreign keys, unique constraints (email), required fields, indexes |
| Errors | Consistent problem-details responses; no stack traces to users |
| Tests | Unit tests for validation/rules, integration tests for each endpoint, auth tests |
| Ops | Migrations applied at startup or by command; Docker Compose with SQL Server |
| Docs | README, ER diagram, screenshots, deployment and rollback steps |

**Build order**

1. Domain model and EF Core schema with migrations; seed data.
2. CRUD with validation and paging; integration tests as you go.
3. Identity + roles; tests for forbidden and unauthenticated access.
4. Docker Compose with SQL Server; verify migrations against a real database.
5. Docs, diagram, screenshots; CI workflow.

**Acceptance criteria**

- [ ] `dotnet test` passes from a clean clone.
- [ ] `dotnet ef database update` builds the schema from empty.
- [ ] A Viewer receives 403 on write endpoints; an anonymous request gets 401.
- [ ] Unique-email and required-field constraints are enforced at the database,
      not only in the UI (prove it with a test that bypasses the UI).
- [ ] README has an ER diagram, screenshots and a rollback procedure.
- [ ] No credentials committed.

