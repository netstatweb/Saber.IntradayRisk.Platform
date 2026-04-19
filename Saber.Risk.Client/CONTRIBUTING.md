# CONTRIBUTING.md

## Purpose
This document defines contribution standards for the Saber.IntradayRisk.Platform repository. It focuses on code style, documentation, tests and the process for database changes. The goal is to keep the codebase readable, maintainable and easy to onboard for new engineers.

## Language policy
- Primary language for code comments and documentation: English.
  - All public APIs must include XML documentation (`/// <summary>`) in English.
- Business-domain explanations may be provided in Polish where they help traders or local stakeholders.
  - Polish explanations should be placed in `docs/pl/` or in short inline comments only when necessary, marked with `// PL:` prefix.
- Do not duplicate long explanations in both languages inline; prefer English XML docs + Polish supplemental doc files.

## Documentation
- Public types and methods MUST have XML documentation comments (`<summary>`, `<param>`, `<returns>`) in English.
- Complex business logic must be documented with a short rationale and examples. Use `docs/` for longer design notes.
- For every database migration or stored procedure change, add a SQL script under `DataBase/` and a short markdown description in `docs/db/`.

## Comments
- Keep inline comments concise and technical. Explain the "why", not the "what".
- Use Polish inline comments only for domain-specific notes that help local stakeholders; keep them short and mark them with `// PL:` prefix.

## Coding style and .editorconfig
- The repository enforces a single coding style via `.editorconfig`. Add or update rules there; formatters and IDEs should follow it.
- Prefer expressive naming and small methods. Avoid long methods with mixed responsibilities.

## Asynchrony and threading
- Never perform blocking I/O on the UI thread. Use `async`/`await` and `ConfigureAwait(false)` in library layers if necessary.
- Heavy CPU-bound operations must be offloaded from UI thread (use `Task.Run` and then marshal minimal state updates to the UI via the Dispatcher).

## Testing
- New features must include unit tests for ViewModels and business logic. Use dependency injection and mocking for tests.
- SQL procedures and data-access logic should be covered by integration tests where feasible.

## Pull Requests
- Each PR should include a short description, the motivation, and screenshots or recording if UI changes.
- PR checklist (must pass before merge):
  - Build succeeds
  - Unit tests pass
  - Documentation updated (if public API or DB changes)
  - SQL scripts added under `DataBase/` for schema changes

## Database changes
- Place SQL migration scripts under `DataBase/` and name them with an incremental prefix (e.g. `001_CreateCurrentRiskTable.sql`, `002_GetRiskMetricsPaged.sql`).
- Include an `Up` and `Down` section or separate rollback script.
- Avoid ad-hoc changes directly on production without a migration script.

## Code review and ownership
- Code reviewers should focus on correctness, performance, security, and clarity.
- For critical parts (risk calculations, P&L), involve at least one senior engineer and a domain expert.

## Documentation generation
- Use XML comments to generate API docs (DocFX or similar). Keep generated artifacts out of source control.