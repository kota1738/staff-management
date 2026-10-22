# Staff management system

**Status: in progress.** Specification committed, implementation underway. No
feature below is built yet; this README will be replaced with real setup,
screenshots and test output as the work lands.

## What this will be

A C# application backed by a relational database, taken from coursework to
something a reviewer can clone, run and test:

- Staff records — create, edit, delete, list, with search, filter and paging.
- Validation — data annotations plus server-side checks, with useful errors.
- Authentication — login with hashed passwords.
- Authorisation — Admin, Manager and Viewer roles; Viewers get 403 on writes.
- Database — EF Core migrations, foreign keys, unique email constraint,
  required fields, indexes.
- Errors — consistent problem-details responses, no stack traces to users.
- Tests — unit tests for validation and rules, integration tests per endpoint,
  auth tests for 401 and 403 paths.
- Ops — Docker Compose with SQL Server, migrations applied repeatably.

## Stack

C# · .NET 8 · ASP.NET Core · EF Core · SQL Server · xUnit · Docker · GitHub Actions

## Plan

The full specification, feature table, build order and acceptance criteria live
in [`docs/spec.md`](docs/spec.md).
