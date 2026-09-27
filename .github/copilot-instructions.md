# AI Development Instructions

## 1. Project Overview

Nearby Eats is a production-oriented web application that helps users select a point on a map and discover restaurants within a 2 km radius, with results including rating, review count, distance, and location.

The application will be developed incrementally. Do not implement functionality that has not been explicitly requested.

## 2. Technology Direction

- Backend: ASP.NET Core / C#
- Frontend: React + TypeScript
- Database: PostgreSQL
- ORM: Entity Framework Core
- Automated testing: xUnit
- Containerization: Docker
- CI/CD: GitHub Actions

Do not introduce additional frameworks or infrastructure without a concrete requirement and an explanation of the trade-off.

## 3. Architecture

Use Clean Architecture principles with clear separation between:

- Domain
- Application
- Infrastructure
- API

Dependency direction must remain inward:

- Domain must not depend on Application, Infrastructure, API, databases, frameworks, or external providers.
- Application may depend on Domain.
- Infrastructure may depend on Application and Domain.
- API may depend on Application and the composition root may reference Infrastructure for dependency injection registration.
- Business rules belong in Domain/Application, not in API endpoints.
- External provider and persistence concerns belong in Infrastructure.

Use DDD concepts where they provide real value. Do not introduce aggregates, value objects, domain events, repositories, or other abstractions merely for ceremony.

Use CQRS where it improves separation or maintainability. Do not force CQRS for trivial operations.

## 4. Coding Principles

- Prefer readable, maintainable code over clever code.
- Keep methods and classes focused.
- Prefer explicit behavior over unnecessary abstraction.
- Follow established project conventions once they exist.
- Avoid unrelated refactoring while implementing a task.
- Do not duplicate business rules.
- Validate input at appropriate boundaries.
- Keep configuration externalized.
- Never hard-code secrets, API keys, passwords, connection strings containing credentials, or environment-specific secrets.
- Use asynchronous APIs appropriately.
- Respect cancellation tokens for long-running or I/O-bound operations where appropriate.
- Handle failures explicitly and consistently.
- Do not suppress exceptions without a documented reason.

## 5. API Design

- Keep HTTP concerns in the API layer.
- Do not place business logic directly in controllers/endpoints.
- Use appropriate HTTP status codes.
- Validate incoming requests.
- Define stable request/response contracts.
- Do not expose persistence entities directly as public API contracts when a dedicated contract is appropriate.
- Keep API error responses consistent.

## 6. Data Access

- Entity Framework Core is the initial persistence technology.
- Database access belongs in Infrastructure.
- Avoid leaking EF Core-specific concerns into Domain.
- Use migrations for schema evolution.
- Avoid unnecessary database round trips.
- Be explicit about loading related data.
- Consider pagination for potentially large result sets.
- Do not retrieve an entire dataset when a filtered query can be performed in the database.

## 7. External Providers

Restaurant/location data may eventually come from an external provider.

- External provider contracts must not leak into Domain.
- Isolate provider-specific implementations behind application-facing abstractions where appropriate.
- Treat external APIs as unreliable dependencies.
- Handle timeouts, transient failures, rate limits, and unavailable data deliberately.
- Never commit provider credentials.

## 8. Testing

Production features should have appropriate automated tests.

Use:

- Unit tests for business rules and isolated application behavior.
- Integration tests for persistence, API behavior, and important infrastructure interactions.
- End-to-end tests only where they provide meaningful value.

Tests should verify behavior rather than implementation details.

Every bug fix should add or update a regression test when practical.

Before declaring a task complete:

1. Build the affected solution.
2. Run the relevant automated tests.
3. Report the exact commands/results when possible.
4. Do not claim success when build or tests fail.

## 9. Git Workflow

The repository uses the following promotion flow:

feature/* -> dev -> test -> prod

Rules:

- Create focused feature branches from the appropriate development branch.
- Feature branches should contain one coherent change.
- Feature branches target `dev`.
- `dev` is promoted to `test` after development validation.
- `test` is promoted to `prod) after release validation.
- `prod` represents production.
- Do not commit directly to `prod` unless explicitly required by an emergency procedure.
- Keep commit messages concise and meaningful.
- Do not mix unrelated changes in the same commit or pull request.

## 10. AI Agent Behavior

Before changing code:

1. Inspect the relevant repository structure and existing implementation.
2. Identify applicable project instructions and skills.
3. Understand existing conventions.
4. Form a concise implementation plan.
5. Identify affected files.
6. Implement only the requested scope.

During implementation:

- Make the smallest coherent change that satisfies the requirement.
- Reuse existing code when appropriate.
- Do not create duplicate abstractions.
- Do not rewrite working code without a reason.
- Do not modify unrelated files.
- Do not invent APIs, dependencies, configuration, or infrastructure requirements.
- If a requirement is ambiguous and the ambiguity materially affects architecture or behavior, ask for clarification or explicitly state the assumption before proceeding.

After implementation:

1. Inspect the diff.
2. Build the affected project/solution.
3. Run relevant tests.
4. Fix failures caused by the implementation.
5. Review for security and unintended side effects.
6. Summarize the work.

The completion report should include:

- What changed
- Files changed
- Tests added/updated
- Tests executed and their result
- Important design decisions
- Remaining risks or TODOs

## 11. Dependency Management

Before adding a package:

- Confirm that the functionality cannot reasonably be implemented with the existing stack.
- Explain why the package is needed.
- Prefer well-maintained, established packages.
- Avoid adding packages solely for convenience when the dependency introduces unnecessary complexity.

## 12. Security

Treat all external input as untrusted.

Pay particular attention to:

- Authentication and authorization
- Input validation
- SQL injection
- XSS
- CSRF where applicable
- SSRF
- Sensitive data exposure
- Secrets management
- Dependency vulnerabilities
- Unsafe deserialization
- Rate limiting for publicly exposed endpoints

Do not place secrets in source control, logs, tests, prompts, documentation, or error responses.

## 13. Documentation

Important architectural decisions should be documented.

Use:

- README.md for project setup and developer usage.
- docs/ for broader project documentation.
- ADRs for significant architectural decisions.

Documentation should describe the actual system, not an intended future system.

## 14. Scope Control

This repository is being developed incrementally.

Do not implement future features merely because they are mentioned in the product vision.

For example, the following should not be implemented until explicitly requested:

- Authentication
- AI recommendations
- User accounts
- Favorites
- Reviews
- Notifications
- Redis
- Microservices
- Kubernetes
- Advanced observability

When a task is small, keep the implementation small.

## 15. Definition of Done

A task is considered complete only when:

- The requested behavior is implemented.
- The implementation follows the project's architecture.
- Relevant tests exist.
- Build succeeds.
- Relevant tests pass.
- No unrelated files were changed.
- The diff has been reviewed.
- Documentation is updated when required.
- Any known limitation is clearly reported.
