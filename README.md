# WIL Assignment Task 2 - Web Application

<div align="center">
  <h1>Simmer & Sear</h1>
  <p><i>Premium cookware e-commerce platform and administrative dashboard.</i></p>
  <br>
  <!-- You can replace the src link below with the actual URL to your logo -->
  <img width="500" height="300" alt="Simmer & Sear Logo" src="https://github.com/user-attachments/assets/12a23b84-269e-40a8-ad83-596e72972ab2">
</div>

---

### Developed by: The Group™

* Tiaan Walters - ST10434073
* Jared Nel - ST10434039
* Heinrich Rossouw - ST10434056
* Richard Goble - ST10083358
* Ross Wassenaar - ST10434912

**Module:** INSY7315 — Work Integrated Learning

<br>

## Table of Contents
- [Purpose of the Web App](#purpose-of-the-web-app)
- [System Architecture & Infrastructure](#system-architecture--infrastructure)
- [Architecture Decision Records](#architecture-decision-records)
- [Design Considerations](#design-considerations)
- [Key Features](#key-features)
- [Project Structure](#project-structure)
- [How to Set up and Run Application](#how-to-set-up-and-run-application)
- [Running Tests](#running-tests)
- [Known Limitations](#known-limitations)
- [System Requirements](#system-requirements)
- [Academic Project Disclaimer](#academic-project-disclaimer)

<br>

---

## Purpose of the Web App
**Simmer & Sear** is a premium cookware e-commerce platform built to bring high-quality cast iron, stainless steel and non-stick cookware to kitchens across Africa. Our client, a cookware retailer, faced a fragmented buying process: retail consumers and wholesale stockists bought through different channels, and every large order needed a manual quotation cycle before a sale could close.

We set out to solve this with one platform that serves both audiences without asking either to leave the site. A home chef can browse a unified catalogue, filter by type, size, material, brand and range, and check out normally. A restaurant owner requesting two hundred pans is automatically detected by a configurable bulk-order threshold, which intercepts the standard checkout and routes the order to a PDF quotation and a booking with a sales agent instead. Administrators get a single, secure dashboard to manage the catalogue, orders, content and staff, with every sensitive action recorded in an audit log.

<br>

## System Architecture & Infrastructure
The system is built as **one ASP.NET Core (.NET 10) application**, serving both the MVC storefront/admin views and the `/api` endpoints from a single deployable. This removes cross-origin requests and cross-app authentication entirely: the browser and the API share one session cookie.

* **Application layers:** Domain → Application → Infrastructure, with MVC controllers and `/api` modules sitting above Application. Layer boundaries are enforced in code with `NetArchTest`, not just drawn on a diagram.
* **Database:** MongoDB Atlas (M0 cluster) stores all application data as documents, with product images stored in GridFS. `$jsonSchema` validators and compound/text indexes are created at startup, defined as code.
* **Authentication & security:** Application-managed identity using `PasswordHasher` (PBKDF2-HMAC-SHA256, 100k iterations, per-user salt). Role-based access control is enforced server-side on every endpoint (Customer, Sales Admin, Master Admin), with denied requests logged to an append-only audit collection before the 403 response is returned.
* **Hosting:** Azure App Service (B1 plan, Linux, .NET 10, Spain Central — the region available under our Azure for Students subscription). MongoDB Atlas is the managed database layer. One production environment is used; local development runs against MongoDB in Docker.
* **CI/CD:** GitHub Actions builds the solution, runs unit and integration tests against a real MongoDB container, scans for committed secrets with `gitleaks`, then deploys to Azure on every merge to `main`, gated by a `/health/ready` smoke test.
* **Integrations:** PayFast for payment processing (sandbox), Brevo for transactional email, QuestPDF for generating bulk-order quotations.

<br>

## Architecture Decision Records
Key technical decisions, and why we made them, are recorded as ADRs:

| ADR | Decision |
|---|---|
| ADR-001 | Single deployable — MVC and `/api` share one origin, removing CORS and cross-app token handling |
| ADR-002 | MongoDB Atlas for all records, not Firestore — the original rationale for Firestore was that Firebase Auth was already the identity provider; once SSO was dropped, that reason no longer applied |
| ADR-003 | Application-managed identity (cookie auth + `PasswordHasher`, PBKDF2) instead of Firebase Authentication |
| ADR-004 | Azure App Service B1 over the free F1 tier — F1 cannot hold Always On and stops after 60 CPU-minutes a day |
| ADR-005 | One MongoDB cluster, with data separated at the database level |

<br>

## Design Considerations
The application was engineered with a strict focus on premium aesthetics, user experience (UX), and high performance.
* **Premium Aesthetics:** A locked-in design system utilizing deep navy headers and ember-orange accents to convey a luxury brand identity.
* **Fluid Responsiveness:** Advanced CSS grid layouts automatically scale and reflow to fit any device, down to 320px mobile screens, without relying on messy media queries.
* **Lightweight Architecture:** Heavy JavaScript frameworks were entirely avoided on the front end. The 12-tab Admin Dashboard runs on Vanilla JavaScript, ensuring it operates as fast as a Single Page Application (SPA) with zero loading lag.

<br>

## Key Features
* **Secure Authentication:** Robust user login and role-based access control (Customer, Sales Admin, Master Admin).
* **Dynamic E-Commerce Journey:** Users can filter product catalogues, manage shopping carts, and submit contact inquiries.
* **Bulk Order Engine:** An automated threshold detects when a cart qualifies as a bulk restaurant order, intercepting standard checkout and redirecting the user to book a sales agent for wholesale quoting.
* **Comprehensive Admin Portal:** Administrators can securely manage users, approve product reviews, publish journal news posts, and process orders all from a single dashboard.
* **Audit Logging:** Every sensitive administrative action, and every denied access attempt, is written to an append-only audit log.

<br>

## Project Structure & Architecture
Simmer & Sear is built using a modern **Clean Architecture** approach. This ensures strict separation of concerns, high testability, and a highly scalable codebase. 

```text
📦 SimmerAndSear
 ┣ 📄 SimmerAndSear.slnx                 # Main solution file
 ┣ 📄 Directory.Packages.props           # Central package version management
 ┣ 📄 Directory.Build.props              # Shared build settings (nullable, analyzers)
 ┃
 ┣ 📂 src/                               # Core Application Source
 ┃ ┣ 📂 SimmerAndSear.Domain/            # Entities, enums, value objects (Zero external dependencies)
 ┃ ┣ 📂 SimmerAndSear.Application/       # Core business logic, interfaces, validators, factories
 ┃ ┣ 📂 SimmerAndSear.Infrastructure/    # External concerns (Mongo Repos, PayFast, Brevo, QuestPDF)
 ┃ ┣ 📂 SimmerAndSear.Contracts/         # DTOs shared between API, Views, and Tests
 ┃ ┣ 📂 SimmerAndSear.Seeder/            # Idempotent console application for database seeding
 ┃ ┗ 📂 SimmerAndSear.Web/               # The single deployable ASP.NET Core application
 ┃   ┣ 📂 Api/Modules/                   # Feature-sliced API endpoints (Catalogue, Orders, Accounts)
 ┃   ┣ 📂 Controllers/                   # MVC controllers rendering the front-end Views
 ┃   ┣ 📂 Views/                         # Razor views and shared UI components
 ┃   ┗ 📂 wwwroot/css/                   # Frozen design system (tokens.css, base.css, components.css)
 ┃
 ┗ 📂 tests/                             # Automated Testing Suite
   ┣ 📂 SimmerAndSear.UnitTests/         # Isolated testing of core business logic
   ┣ 📂 SimmerAndSear.IntegrationTests/  # End-to-end testing of infrastructure and database connections
   ┗ 📂 SimmerAndSear.ArchitectureTests/ # NetArchTest definitions enforcing strict layer boundaries
```

<br>

---

## How to Set up and Run Application

**Using the Command Line (Terminal):**
1. Clone this repository to your local machine.
2. Ensure you have a MongoDB instance running locally (e.g., via Docker: `docker run -d -p 27017:27017 --name mongo mongo`).
3. Open your terminal and navigate to the web project directory:
   ```bash
   cd src/SimmerAndSear.Web
   ```
4. Build and run the application:
   ```bash
   dotnet run
   ```
5. Open your web browser and navigate to the URL provided in the terminal (typically `http://localhost:5100`).

**Alternatively (Using an IDE):**
1. Clone this repository to your local machine.
2. Open the solution file in **Visual Studio** or **JetBrains Rider**.
3. Ensure you have MongoDB running locally.
4. Set `SimmerAndSear.Web` as your Startup Project.
5. Click the **Run/Play** button to automatically build and launch the application in your browser.

On first run, the application seeds a Master Admin account from configuration and a small catalogue of demo products, so you can sign in and explore immediately.

<br>

## Running Tests
```bash
dotnet test
```
This runs three test projects:
* **Unit tests** — business rules, threshold boundaries, validators, mappers.
* **Integration tests** — run against a real MongoDB container, exercising real queries, indexes and GridFS.
* **Architecture tests** — NetArchTest assertions that Domain never references Infrastructure or Contracts, and that Application never references Web.

<br>

## Known Limitations
* Catalogue filters for **type, material and size** sort in memory rather than by a dedicated index; only category, brand and price carry compound indexes at this data volume. This was a deliberate, measured trade-off.
* **One hosted environment** (production only) — there is no staging tier and no deployment slots, so each deploy causes a short restart.
* **PayFast** runs against the sandbox; a documented `simulate-payment` endpoint is available as a fallback if the live integration is unavailable during demonstration.

<br>

## System Requirements
* **Framework:** .NET 10.0 SDK or higher.
* **Database:** MongoDB (running locally on port `27017` or configured via connection string).
* **Browser:** A modern web browser (Chrome, Edge, Firefox, Safari) to properly render the responsive CSS Grid layouts.
* **Active Internet Connection:** Required for connecting to the PayFast payment sandbox and loading any external web assets.

<br>

## Academic Project Disclaimer
This repository was created solely as a university assignment (module INSY7315, Work Integrated Learning) to demonstrate software design and development skills. It is not a live commercial store: no products are actually offered for sale, no goods are shipped, and PayFast is used only in sandbox mode.

Brand names, logos and product names, including ELO, Fissler and Sambonet, belong to their respective owners and are used here for demonstration and educational purposes only. This project is not affiliated with, endorsed by, or sponsored by any of these brands.
