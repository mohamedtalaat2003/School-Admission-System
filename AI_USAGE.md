# AI Usage & Corrections Log

## 1. Role Division & Architecture Ownership
- **Architect & Author**: Mohamed Talaat (`mohamedtalaat2003`)
  - Conceived all system planning, design, business rules, concurrency architecture, and validation logic.
- **AI Assistant**: Antigravity AI (used strictly as an implementation tool to write and scaffold code per the author's explicit directives).

---

## 2. Implementation Tasks Handled by AI
- Implementing the author's EF Core 10 database entities and optimistic concurrency versioning.
- Implementing the ASP.NET Core 10 controllers and services following SOLID principles.
- Implementing the React 18 frontend with the author's dynamic button states.
- Implementing the unit, integration, and 1,000+ request concurrency test suite.
- Writing Dockerfiles and Docker Compose files per author specifications.

---

## 3. Deviations Caught and Corrected by the Author
1. **Unrequested Field Limits**:
   - The AI attempted to add an arbitrary 100-character constraint on class names. The author caught this mistake and directed that class names have no artificial limit, strictly following the PDF.
2. **UI Button State vs. Status Badges**:
   - The AI initially suggested separate status badge elements. The author corrected this to embed the live status, countdown, and action directly into the class row button.
3. **Concurrency Test Scale**:
   - The AI originally proposed testing with only 50–100 requests. The author directed scaling the concurrency test to thousands of simultaneous requests to match real-world peak admission spikes.
4. **Data Model Adherence**:
   - The author directed maintaining strict adherence to the PDF specification without creating unnecessary additional tables.
