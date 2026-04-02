# Order Processing System

A backend E-commerce Order Processing System built with **ASP.NET Core 9**, **Entity Framework Core**, **Dapper**, and **SQL Server**.

## Architecture

```
OPS.Domain              → Entities, Enums, Domain Exceptions (no dependencies)
OPS.Application         → Business logic, Services, DTOs, Validators, Interfaces
OPS.Database            → EF Core DbContext, Migrations, Repositories, Seed data
OPS.Web                 → ASP.NET Core Web API (controllers, middleware, Swagger)
OPS.BackgroundServices  → .NET Worker Service (PENDING → PROCESSING every 5 mins)
```

## Tech Stack

| Concern | Technology |
|---|---|
| Framework | ASP.NET Core 9 |
| ORM (writes) | Entity Framework Core 9 |
| ORM (reads) | Dapper |
| Database | SQL Server 2022 |
| Auth | JWT Bearer |
| Validation | FluentValidation |
| Logging | Serilog (rolling file) |
| API Docs | Swagger / Swashbuckle |
| Unit Tests | xUnit + Moq + FluentAssertions |
| Integration Tests | xUnit + WebApplicationFactory + SQLite |
| Containerization | Docker Compose |

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- SQL Server (local or Docker)
- Docker & Docker Compose (optional)

---

## Option 1 — Run with Docker Compose

```bash
docker-compose up --build
```

This starts:
- SQL Server on port `1433`
- Web API on `http://localhost:5000`
- Background Worker

**Swagger UI:** http://localhost:5000/swagger  
**Health check:** http://localhost:5000/health

---

## Option 2 — Run Locally

### 1. Update connection string

Edit `src/OPS.Web/appsettings.json` and `src/OPS.BackgroundServices/appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=.;Database=OrderProcessingDB;User Id=sa;Password=YourPassword;TrustServerCertificate=True;"
}
```

### 2. Apply migrations (auto-applied in Development on startup)

```bash
dotnet ef database update \
  --project src/OPS.Database \
  --startup-project src/OPS.Web
```

### 3. Run the Web API

```bash
dotnet run --project src/OPS.Web
```

Swagger UI: http://localhost:5000/swagger

### 4. Run the Background Worker (separate terminal)

```bash
dotnet run --project src/OPS.BackgroundServices
```

---

## API Quick Start

### Register
```http
POST /api/v1/auth/register
{ "name": "John Doe", "email": "john@example.com", "password": "Password1" }
```

### Login
```http
POST /api/v1/auth/login
{ "email": "john@example.com", "password": "Password1" }
```
Returns JWT token — include as `Authorization: Bearer <token>` on all subsequent requests.

### Get Products (use IDs when creating orders)
```http
GET /api/v1/products
```

### Add Shipping Address
```http
POST /api/v1/customers/{customerId}/addresses
{ "line1": "123 Main St", "city": "New York", "state": "NY", "zip": "10001", "country": "US" }
```

### Create Order
```http
POST /api/v1/orders
{
  "customerId": "...",
  "shippingAddressId": "...",
  "items": [{ "productId": "...", "quantity": 2 }]
}
```

### Other Order Endpoints
```http
GET    /api/v1/orders                     # list all (filter: ?status=Pending)
GET    /api/v1/orders/{id}                # get by ID with full history
PUT    /api/v1/orders/{id}/status         # update status
POST   /api/v1/orders/{id}/cancel         # cancel (PENDING or PROCESSING only)
```

---

## Status Transition Rules

```
PENDING ──→ PROCESSING ──→ SHIPPED ──→ DELIVERED
   │              │
   └──→ CANCELLED └──→ CANCELLED
```

Background job automatically promotes `PENDING → PROCESSING` every 5 minutes.

---

## Running Tests

```bash
# Unit tests
dotnet test tests/OPS.UnitTests

# Integration tests (uses SQLite in-memory — no SQL Server needed)
dotnet test tests/OPS.IntegrationTests

# All tests
dotnet test
```

---

## Project Structure

```
OrderProcessingSystem/
├── src/
│   ├── OPS.Domain/
│   ├── OPS.Application/
│   ├── OPS.Database/
│   ├── OPS.Web/
│   └── OPS.BackgroundServices/
├── tests/
│   ├── OPS.UnitTests/
│   └── OPS.IntegrationTests/
├── docker-compose.yml
└── README.md
```
