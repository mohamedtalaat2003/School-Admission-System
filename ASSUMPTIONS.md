# Assumptions & Architecture Decisions

This document outlines the design decisions and architectural assumptions made where the specification left details open, along with technical explanations for concurrency and persistence.

---

## 1. Concurrency Guarantee Across Multiple Backend Instances

### Requirement
*"Even if two backend processes run simultaneously on different ports while using the same database, the number of held plus confirmed seats must never exceed the class's total seat capacity. Explain in ASSUMPTIONS.md how you ensure this."*

### How We Ensure This
1. **Optimistic Concurrency with EF Core & PostgreSQL**:
   - The `Class` entity includes a concurrency token (`[ConcurrencyCheck] public int Version { get; set; }`).
   - Every seat reservation (`POST /classes/{classId}/holds`) executes inside an explicit database transaction.
2. **Transaction & State Verification**:
   - When a hold is requested, the transaction reads the `Class` entity and its current version.
   - It queries the database for the active seat count:
     `SELECT COUNT(*) FROM "Holds" WHERE "ClassId" = @classId AND ("Status" = 'CONFIRMED' OR ("Status" = 'HELD' AND "ExpiresAt" > NOW()));`
   - If `activeSeats >= TotalSeats`, the transaction aborts and immediately returns **HTTP 409 Conflict**.
   - If a seat is available, the transaction increments `Class.Version = Version + 1` and inserts the new `Hold` entity.
3. **Collision Detection via `DbUpdateConcurrencyException`**:
   - If two or more backend processes attempt to hold a seat for the same class concurrently, both read the same initial `Version`.
   - The first process to commit successfully updates `Class.Version`.
   - The competing processes fail with an EF Core `DbUpdateConcurrencyException` because the row version in PostgreSQL no longer matches.
   - The backend catches `DbUpdateConcurrencyException` and enters an automated retry loop (up to 3 attempts): it re-queries the freshly committed seat count. If seats are now exhausted, it returns **HTTP 409 Conflict**.
   - This database-backed optimistic concurrency mechanism guarantees zero over-allocation regardless of how many backend instances run in parallel.

---

## 2. Automatic Seat Release Mechanism (Zero API Call)

### Requirement
*"When a hold expires, its seat must be released automatically. No API call should be required to release it."*

### Implementation
- **Dynamic Real-Time Calculation**: All queries calculating available seats or checking hold validity evaluate `ExpiresAt > DateTimeOffset.UtcNow`. A hold whose expiration timestamp has passed is considered inactive with **0 ms delay**, instantly freeing the seat for subsequent applicants.
- **Background Cleanup Worker**: A background service (`HoldCleanupBackgroundService`) runs periodically every 5 seconds to transition expired hold records to `Status = 'EXPIRED'` in the database for auditing and UI consistency.

---

## 3. Student Identity & Data Model

- **No Separate Student Table**: Strict compliance with the PDF specification. The student is identified strictly by their 17-digit birth registration number passed in the `X-Student-Id` header and recorded as `StudentId` on the `Hold` entity.
- **Duplicate Application Prevention**: A student is prevented from having more than one active hold (`Status = 'HELD' AND ExpiresAt > NOW()`) or confirmed admission (`Status = 'CONFIRMED'`) for the same class.

---

## 4. UI & Role Separation

- **School Admin vs. Parent/Student**: Class creation is placed in a dedicated School Admin interface so administrators can define classes and capacities, while parents and students interact with the public registration portal.
- **Dynamic Action Button**: In place of separate static badges, the action button on each class row dynamically reflects status: `Apply` -> `HELD (MM:SS) - Pay Fee` -> `CONFIRMED` / `EXPIRED` / `Class Full`.
