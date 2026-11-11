# School Admission System

A resilient, high-concurrency School Admission System designed for first-come, first-served seat reservations, fee confirmation, dynamic hold expiration, and multi-instance concurrency protection.

## Technology Stack
- **Backend**: C# (.NET 10 LTS) ASP.NET Core Web API
- **Database & ORM**: PostgreSQL 16 + Entity Framework Core 10 (`Npgsql.EntityFrameworkCore.PostgreSQL`)
- **Frontend**: React 18+ (Vite + TypeScript + Tailwind CSS)
- **Containerization**: Docker & Docker Compose

---

## Running the Application with Docker

The entire project runs using Docker Compose without needing any local runtimes installed:

```bash
docker compose up --build
```

### Application URLs
- **Frontend UI**: [http://localhost:3000](http://localhost:3000)
- **Backend API**: [http://localhost:8080](http://localhost:8080)
- **Swagger Documentation**: [http://localhost:8080/swagger](http://localhost:8080/swagger)

---

## Running the Automated Tests

### 1. Backend Unit, Integration, and Concurrency Tests (.NET 10)
```bash
dotnet test backend/tests/SchoolAdmissionSystem.UnitTests/
dotnet test backend/tests/SchoolAdmissionSystem.IntegrationTests/
```

### 2. Frontend Automated Tests (Vitest)
```bash
cd frontend
npm test
```

---

## Environment Variables Configuration

| Variable | Description | Default |
|---|---|---|
| `HOLD_DURATION_SECONDS` | Number of seconds a seat hold remains active before automatically expiring | `600` |
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string | `Host=db;Port=5432;Database=SchoolAdmissionDb;Username=postgres;Password=postgres` |
| `VITE_API_URL` | Backend API base URL consumed by the React frontend | `http://localhost:8080` |
