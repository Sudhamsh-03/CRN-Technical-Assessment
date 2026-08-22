# Products API

A RESTful backend API for managing Products (and their child Items) built with **.NET 8 / ASP.NET Core Web API**, following **Clean Architecture** principles.

## Tech Stack

| Concern | Choice |
|---|---|
| Framework | .NET 8, C# 12 |
| API | ASP.NET Core Web API |
| Database | SQL Server + Entity Framework Core 8 (Code First / Migrations) |
| Auth | JWT bearer access tokens + rotating refresh tokens |
| Validation | FluentValidation |
| Logging | Serilog (console + rolling file sinks) |
| Docs | Swagger / OpenAPI (Swashbuckle) |
| Testing | xUnit, Moq, FluentAssertions, `WebApplicationFactory`, EF Core InMemory |
| Containerization | Docker + Docker Compose |

## Architecture

The solution is split into four layers, each its own project, with dependencies flowing strictly inward (API → Application/Infrastructure → Domain):

```
ProductsApi.sln
├── src/
│   ├── Domain/                 # Entities, enums, domain exceptions. No dependencies.
│   ├── Application/             # DTOs, service interfaces + implementations, validators,
│   │                            # repository/UoW interfaces, mapping extensions.
│   │                            # Depends only on Domain.
│   ├── Infrastructure/          # EF Core DbContext, entity configurations, repositories,
│   │                            # Unit of Work, JWT token service, password hasher.
│   │                            # Depends on Application (implements its interfaces) + Domain.
│   └── API/                     # Controllers, middleware, filters, DI/composition root.
│                                # Depends on Application + Infrastructure.
└── tests/
    ├── Application.Tests/       # Unit tests for services (Moq-based).
    ├── Infrastructure.Tests/    # Repository tests against EF Core InMemory.
    └── API.Tests/               # Full HTTP integration tests via WebApplicationFactory.
```

**Why this split:** Domain has zero framework dependencies. Application defines *what* the app does (contracts + business logic) without knowing *how* data is persisted — it depends on repository/`IUnitOfWork` interfaces, not EF Core directly. Infrastructure provides the *how* (EF Core, JWT, BCrypt). The API layer only wires things together and exposes HTTP endpoints. This makes the business logic testable in isolation (see `Application.Tests`) and the persistence/auth technology swappable without touching business rules.

### Request flow

```
HTTP request → Middleware (exceptions, security headers) → Auth (JWT) → Action Filter (FluentValidation)
            → Controller → Application Service → Repository (via IUnitOfWork) → EF Core → SQL Server
```

## Database Schema

Matches the schema specified in the assessment, plus `User` / `RefreshToken` tables for authentication:

```sql
CREATE TABLE [dbo].[Product]
(
    [Id]           INT IDENTITY(1,1) PRIMARY KEY,
    [ProductName]  NVARCHAR(255) NOT NULL,
    [CreatedBy]    NVARCHAR(100) NOT NULL,
    [CreatedOn]    DATETIME NOT NULL,
    [ModifiedBy]   NVARCHAR(100) NULL,
    [ModifiedOn]   DATETIME NULL
)

CREATE TABLE [dbo].[Item]
(
    [Id]         INT IDENTITY(1,1) PRIMARY KEY,
    [ProductId]  INT NOT NULL FOREIGN KEY REFERENCES Product(Id),
    [Quantity]   INT NOT NULL
)
```

Schema is generated and versioned via EF Core Migrations (`src/Infrastructure/Data/Migrations`) — no manual SQL scripts needed.

## API Endpoints

All product endpoints are versioned (`/api/v1/...`) and require a valid JWT (`Authorize`), except auth endpoints.

### Auth
| Method | Route | Description |
|---|---|---|
| POST | `/api/v1/auth/register` | Create a user account, returns tokens |
| POST | `/api/v1/auth/login` | Authenticate, returns access + refresh token |
| POST | `/api/v1/auth/refresh` | Exchange a refresh token for a new pair (rotation) |
| POST | `/api/v1/auth/revoke` | Revoke a refresh token (logout) |

### Products
| Method | Route | Auth | Description |
|---|---|---|---|
| GET | `/api/v1/products?pageNumber=&pageSize=&searchTerm=` | Any authenticated user | Paged list of products |
| GET | `/api/v1/products/{id}` | Any authenticated user | Get one product with its items |
| POST | `/api/v1/products` | Any authenticated user | Create a product (optionally with items) |
| PUT | `/api/v1/products/{id}` | Any authenticated user | Update a product's name |
| DELETE | `/api/v1/products/{id}` | **Admin** role only | Delete a product |
| GET | `/api/v1/products/{id}/items` | Any authenticated user | List items for a product |
| POST | `/api/v1/products/{id}/items` | Any authenticated user | Add an item to a product |

Every error response uses the standard `application/problem+json` shape (RFC 7807), with a global exception-handling middleware translating domain exceptions to the right HTTP status:

- `NotFoundException` → 404
- FluentValidation `ValidationException` → 400 (with per-field error details)
- `ConflictException` → 409 (e.g. duplicate username)
- `AuthenticationException` → 401
- anything else → 500 (logged, not leaked to the client)

## Authentication Flow

1. `POST /auth/register` or `/auth/login` returns a short-lived **access token** (JWT, 15 min default) and a long-lived **refresh token** (opaque random string, 7 days, stored server-side in `RefreshToken`).
2. Clients send `Authorization: Bearer <accessToken>` on every request.
3. When the access token expires, the client calls `POST /auth/refresh` with the refresh token. The old refresh token is revoked and a new access/refresh pair is issued (rotation) — if a revoked/expired token is replayed, the request is rejected, which also lets you detect token theft.
4. `POST /auth/revoke` invalidates a refresh token immediately (logout).
5. Passwords are hashed with BCrypt (work factor 12); the JWT is signed with HMAC-SHA256 using a symmetric key from configuration.

## Performance & Security

- `AsNoTracking()` on all read-only queries (list, get, existence checks).
- Server-side pagination on the products list endpoint (capped page size).
- Indexes on `Product.ProductName`, `Item.ProductId`, `User.Username`/`Email` (unique), `RefreshToken.Token` (unique).
- Response compression middleware enabled.
- Fully async/await throughout (repositories, services, controllers).
- Security response headers (`X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy`).
- CORS policy configurable via `Cors:AllowedOrigins` in configuration.
- Role-based authorization (`DELETE` restricted to `Admin`).
- Centralized FluentValidation via an `IAsyncActionFilter`, so invalid payloads never reach a service/handler.

## Running Locally

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server (LocalDB, Express, or full) **or** Docker

### Option A — dotnet CLI + local SQL Server

1. Update `src/API/appsettings.json` → `ConnectionStrings:DefaultConnection` to match your SQL Server instance (defaults to a local `SQLEXPRESS` instance with Windows/Trusted authentication).
2. Run the API — migrations are applied automatically on startup:

   ```bash
   dotnet run --project src/API/ProductsApi.Api.csproj
   ```
3. Open `https://localhost:<port>/swagger` (the console output on startup shows the exact port).
4. In Swagger: call `POST /api/v1/auth/register`, copy the `accessToken` from the response, click **Authorize** in Swagger UI, enter `Bearer <token>`, and try the `products` endpoints.

### Option B — Docker Compose (API + SQL Server container)

```bash
docker compose up --build
```

This starts a SQL Server 2022 container and the API (migrations run automatically on API startup). Swagger is available at `http://localhost:8080/swagger`.

> The default `Jwt:Secret` and SQL `sa` password in `appsettings.json` / `docker-compose.yml` are **development-only placeholders** — replace them via environment variables/secrets before any real deployment.

## Running Tests

```bash
dotnet test
```

33 tests across three projects:
- `Application.Tests` (16) — service logic in isolation, mocked repositories (Moq).
- `Infrastructure.Tests` (4) — repository behavior against EF Core InMemory (pagination, filtering, includes).
- `API.Tests` (13) — full HTTP round-trips through `WebApplicationFactory` (register/login/refresh/revoke, CRUD, validation errors, 401/403/404 paths) against an in-memory database.

## Deployment (High Level)

1. Build and push the API image: `docker build -f src/API/Dockerfile -t products-api:latest .`
2. Provision a SQL Server instance (Azure SQL, RDS, or a managed container) and set `ConnectionStrings__DefaultConnection` as an environment variable/secret.
3. Set `Jwt__Secret`, `Jwt__Issuer`, `Jwt__Audience` via environment variables or a secrets manager — never commit real values.
4. Run the container behind a reverse proxy/load balancer terminating TLS 1.2+; the app itself only needs to listen on HTTP behind that proxy.
5. EF Core migrations apply automatically on startup (`Database.Migrate()` in `Program.cs`); for a stricter release process, run migrations as a separate CI/CD step instead and disable auto-migrate in production.
