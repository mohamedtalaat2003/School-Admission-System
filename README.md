# School Admission System

A resilient, high-concurrency School Admission System designed for first-come, first-served seat reservations, fee confirmation, dynamic hold expiration, and multi-instance concurrency protection.

## Technology Stack
- **Backend**: C# (.NET 10 LTS) ASP.NET Core Web API
- **Database & ORM**: PostgreSQL 16 + Entity Framework Core 10 (`Npgsql.EntityFrameworkCore.PostgreSQL`)
- **Frontend**: React 18+ (Vite + TypeScript + Tailwind CSS + Lucide Icons)
- **Containerization**: Docker & Docker Compose
- **Testing**: xUnit + WebApplicationFactory (.NET) & Vitest + React Testing Library (Frontend)

---

## Running the Application with Docker

The entire system runs via Docker Compose with persistent database storage:

```bash
docker compose up --build
```

### Application URLs
- **Frontend Portal**: [http://localhost:3000](http://localhost:3000)
- **Backend API**: [http://localhost:8080](http://localhost:8080)
- **Swagger Documentation**: [http://localhost:8080/swagger](http://localhost:8080/swagger)

---

## API Contract Specification

All endpoints adhere strictly to REST resource naming, return `camelCase` JSON, and use ASP.NET Core 10 `IExceptionHandler` (`GlobalExceptionHandler`) with zero `try-catch` blocks in controllers.

| Method | Path | Required Headers | Description |
|---|---|---|---|
| `POST` | `/classes` | — | Create a class with name and seat capacity |
| `GET` | `/classes` | — | List all classes with live seat counts (supports optional `?page=1&pageSize=10`) |
| `GET` | `/classes/{classId}` | — | Get single class seat status (`totalSeats`, `heldSeats`, `confirmedSeats`, `availableSeats`) |
| `POST` | `/classes/{classId}/holds` | `X-Student-Id` (17 digits) | Reserve a seat with optimistic concurrency retry loop |
| `GET` | `/holds/{holdId}` | — | Query status of a specific hold |
| `POST` | `/holds/{holdId}/confirm` | `X-Student-Id` (17 digits) | Confirm admission fee (idempotent) |
| `DELETE` | `/holds/{holdId}` | `X-Student-Id` (17 digits) | Cancel hold and immediately release seat |
| `GET` | `/me/holds` | `X-Student-Id` (17 digits) | Retrieve all applications for the authenticated student |

---

## Running Automated Tests

### 1. Backend Test Suite (26 Tests — 100% Green)
Run all 21 Unit Tests and 5 End-to-End Integration Tests:
```bash
dotnet test backend/SchoolAdmissionSystem.slnx
```

#### Included Test Suites:
- **Unit Tests (`SchoolAdmissionSystem.UnitTests`)**:
  - `ClassServiceUnitTests`: Class creation validation, seat calculations, positive capacities.
  - `HoldServiceUnitTests`: Hold creation, expiration boundaries, ownership authorization, idempotent confirmation, and cancellation.
  - `HoldCleanupBackgroundServiceTests`: Background service interval cleanup and proper cancellation token handling.
- **Integration Tests (`SchoolAdmissionSystem.IntegrationTests`)**:
  - `AdmissionLifecycleTests`: Full end-to-end journey (`POST /classes` -> `GET /classes` -> `POST /classes/{id}/holds` -> `POST /holds/{id}/confirm` -> `GET /me/holds`).
  - `StudentDuplicateApplicationTests`: Single active application rule per class (`HTTP 409 Conflict`) and re-application after cancellation.
  - `AutomaticHoldExpirationTests`: Zero-call seat release verification (seat returns to available pool immediately at `ExpiresAt` with 0 API calls).
  - `ThousandsScaleConcurrencyTests`: High-concurrency stress test with **1,000 simultaneous asynchronous HTTP requests** against a 25-seat class.

### 2. Frontend Test Suite (8 Tests — 100% Green)
Run Vitest tests in the frontend directory:
```bash
cd frontend
npm test
```

#### Included Test Suites:
- `StudentIdentityBar.test.tsx`: 17-digit birth registration validation (`^\d{17}$`), numeric sanitization, and real-time badge updates.
- `ClassRow.test.tsx`: Dynamic button transitions (`[ Apply ]` -> `[ HELD (MM:SS) - Pay Fee ]` + `[ Cancel ]` -> `[ CONFIRMED ]` / `[ EXPIRED ]` / `[ Class Full ]`) and zero-reload countdown timer expiry.

---

## Concurrency & Performance Guarantees

Under high concurrency (e.g. 1,000 parents applying at the exact millisecond registration opens):
1. **Overselling is Impossible**: Enforced via EF Core optimistic locking token (`Version` on `Class`) + explicit database transaction. Competing transactions fail with `DbUpdateConcurrencyException` and enter an automated retry loop with randomized jitter backoff (`Random.Shared.Next(10, 30) * attempt`).
2. **Stress Test Proof**: In `ThousandsScaleConcurrencyTests`, 1,000 parallel requests against 25 seats strictly produce:
   - Exactly **25 requests** receive **HTTP 201 Created**.
   - Exactly **975 requests** receive **HTTP 409 Conflict**.
   - `heldSeats + confirmedSeats == 25`, `availableSeats == 0`.

---

## Environment Variables Configuration

| Variable | Description | Default |
|---|---|---|
| `HOLD_DURATION_SECONDS` | Number of seconds a seat hold remains active before automatically expiring | `600` |
| `MAX_CONCURRENCY_RETRIES` | Maximum retry attempts for optimistic concurrency collisions | `20` |
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string | `Host=db;Port=5432;Database=SchoolAdmissionDb;Username=postgres;Password=postgrespassword` |
