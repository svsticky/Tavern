# Tavern

[![Build and Test Status](https://github.com/svsticky/tavern/actions/workflows/test.yml/badge.svg)](https://github.com/svsticky/tavern/actions/workflows/test.yml)
[![License: PolyForm Noncommercial 1.0.0](https://img.shields.io/badge/License-PolyForm%20Noncommercial-blue.svg)](LICENSE)
![Target Framework](https://img.shields.io/badge/.NET-10.0-blue)
![Frontend Tech](https://img.shields.io/badge/Frontend-React%20%2B%20Vite-cyan)
![Code Coverage](https://img.shields.io/badge/Line%20Coverage-%E2%89%A595%25-green)

> **Tavern** is the central platform for **Sticky** members to come together, explore activities, manage enrollments, and coordinate events.

---

## Key Features

* **Event & Activity Management:** Create, schedule, and configure activities visible on the platform and external calendar feeds.
* **Enrollment & Waiting Lists:** Automated enrollments with real-time capacity monitoring and waiting list processing.
* **Personal Calendar Feeds:** Members subscribe to a private iCalendar feed that keeps the activities they are enrolled in up to date automatically.
* **Secure Authentication:** Integrated single sign-on (SSO) and user lifecycle synchronization powered by Keycloak.
* **Integrated Payments:** Smooth checkout experience for paid activities and annual memberships via Mollie API.
* **Robust Background Workers:** Hangfire-managed transactional outbox workers syncing payments, mail subscriptions, and accounting tools.
* **S3 File Storage:** Upload and manage profile pictures and group graphics securely on AWS S3 (mocked locally using LocalStack).

---

## Tech Stack & Ecosystem

To replicate the production environment perfectly, the local development setup orchestrates several services inside Docker:

* **Backend:** ASP.NET 10.0 Web API with PostgreSQL 17 (Entity Framework Core)
* **Frontend:** React SPA built with Vite
* **Identity Provider:** Keycloak 26.1 (with custom themes/plugins)
* **Object Storage:** LocalStack 3.0 (mocking AWS S3)
* **Mail Catcher:** Mailpit (catches both Keycloak's activation/verification/reset emails and Tavern's own outgoing mail locally, no real mail provider needed)
* **Expose Tunneling:** Ngrok (for local webhook testing)
* **Background Processing:** Hangfire with PostgreSQL storage

---

## Prerequisites

Before setting up Tavern, make sure you have the following installed on your host machine:

1. **Docker & Docker Compose** (Required to run the database and auxiliary containers).
2. **An IDE with Devcontainer Support:**
   * **VS Code** (with the *Dev Containers* extension pack installed).
   * **JetBrains Rider** (supported out-of-the-box).

> [!NOTE]
> The Devcontainer workspace automatically installs essential extensions like the **C# Dev Kit** and a **Database Client** for your convenience.

---

## Local Development Setup

Follow these steps to set up the repository for local development:

### 1. Clone the Repository
```bash
git clone https://github.com/svsticky/tavern.git
cd tavern
```

### 2. Configure Environment Variables
Copy the sample environment file to create your active configurations:
```bash
cp sample.env .env
```
The environment variables in the `.env` file must be filled in. Below is a description of the configuration variables, highlighting those that are only required for production (as they are overridden by `.devcontainer/devcontainer.env` in local development):

#### Overwritten in Development (Only needed in Production)
These variables are pre-configured or overridden for the devcontainer environment, but must be configured for a production deployment:
* **Ports & Core Routing:**
  * `FrontendPort`, `BackendPort`, `DocsPort`: Ports used to map and expose services.
  * `HostUrl`, `ApiUrl`: Base URLs routing requests between the frontend and backend.
* **Database Connection:**
  * `PostgresqlConnectionString`: Connection string for the PostgreSQL database (overridden to point to the local `db` service inside the devcontainer).
* **Identity Provider (Keycloak):**
  * `AUTH_SYSTEM`, `KeycloakUrl`, `KeycloakRealm`, `KeycloakClientId`, `KeycloakBackendClientId`, `KeycloakClientSecret`, `AUTH_WEBHOOK_SECRET`: Keycloak client details and webhook secrets (pre-configured for local dev).

#### Required to be Filled In (Production & Local Integration Testing)
Configure these variables in your `.env` to enable specific features locally or in production:
* **Theme & UI Customization:**
  * `LOGO_URL`: Branding logo URL injected into the Vite frontend. Board colors are managed in the admin settings and stored in the database.
* **Object Storage (S3 / LocalStack):**
  * `S3_ACCESS_KEY_ID`, `S3_SECRET_ACCESS_KEY`, `S3_REGION`, `S3_SERVICE_URL`: Credentials and endpoints for S3 object storage (defaults are configured to run with LocalStack locally).
* **External Integrations:**
  * `NGROK_AUTHTOKEN`, `NGROK_URL`: Auth token and public tunnel URL used by Ngrok for local webhook testing. These are optional, but can be usefull for letting mollie call the webhook after a payment.


### 3. Launch the Devcontainer
* Open the project directory in VS Code or JetBrains Rider.
* When prompted, select **"Reopen in Container"** (or press `Ctrl+Shift+P` -> type `Dev Containers: Reopen in Container`).
* Docker Compose will automatically spin up all services (`db`, `localstack`, `keycloak`, `mailpit`, and `ngrok`).

### 4. Start the Application
Once the devcontainer is running, follow these steps to start the application and configure the default local user:

#### Step A: Run the Backend
Start the ASP.NET Core backend. This will automatically apply database migrations and create the default `BACKUP_ACCOUNT` user in Keycloak:
```bash
dotnet run --project Backend
```

#### Step B: Configure the Local Keycloak User
Keycloak's realm in the devcontainer is configured to send its activation/verification/reset emails through **Mailpit** instead of a real mail provider, so you can either go through the real email flow or just set the user up directly:

* **Option 1 - via email (closer to production):**
  1. Open the local Keycloak Admin console at [http://localhost:8082](http://localhost:8082) and log in with `admin` / `admin`.
  2. Navigate to **Users**, search for the user matching your configured `BACKUP_ACCOUNT_EMAIL`, and open it.
  3. Use Keycloak's **Send verification email** / **Send password reset** actions on the user.
  4. Open Mailpit at [http://localhost:8025](http://localhost:8025) to view the email and click the link to verify the address and set a password.
* **Option 2 - directly in Keycloak (quicker):**
  1. Open the local Keycloak Admin console at [http://localhost:8082](http://localhost:8082) and log in with `admin` / `admin`.
  2. Navigate to **Users**, search for the user matching your configured `BACKUP_ACCOUNT_EMAIL`, and click on their username.
  3. Go to the **Credentials** tab, click **Reset password**, enter your desired password, and toggle **Temporary** to **Off**.
  4. Go to the **Details** tab, toggle **Email Verified** to **On**, and save the changes.

#### Step C: Run the Frontend
With the backend running and the local user configured, open a new terminal in the container and start the React dev server:
```bash
cd Frontend
npm run dev
```

Once the dev server starts, everything is fully running and you can access the application at [http://localhost:5173](http://localhost:5173).

---

### Forwarded Ports within Devcontainer

Once the devcontainer is running, the following services are mapped locally:

| Service | Local URL | Credentials / Notes |
| :--- | :--- | :--- |
| **Frontend (React Server)** | [http://localhost:5173](http://localhost:5173) | Development dev server |
| **Backend API** | [http://localhost:8080](http://localhost:8080) | Swagger documentation at `/swagger` |
| **Keycloak Admin** | [http://localhost:8082](http://localhost:8082) | Credentials: `admin` / `admin` |
| **Mailpit** | [http://localhost:8025](http://localhost:8025) | Inbox for Keycloak's and Tavern's outgoing emails |
| **Ngrok Dashboard** | [http://localhost:4040](http://localhost:4040) | Check active public tunnels |

---

## Testing & Code Coverage

### Backend
Backend tests are run using **xUnit** and code coverage is tracked via **Coverlet**. 

#### Running Tests Locally
To execute the backend test suite and calculate code coverage:
```bash
dotnet test
```

#### Exclusions & Coverage Requirements
* **Line Coverage Requirement:** $\ge 95\%$
* **Excluded Classes:** Infrastructure and startup elements (e.g., `Program`, `DatabaseSeeder`, `ServiceExtensions`, `SMTPMailService`, and `MailgunService`) are excluded from coverage statistics.
* **Parallelization:** Disabled (`DisableTestParallelization = true`) to prevent race conditions during process-wide environment variable updates.

### Frontend
Frontend tests are run using **Vitest** and **React Testing Library**, with coverage tracked via **v8** across the entire app (routes, layouts, context, and components — not just utility/handler logic).

#### Exclusions & Coverage Requirements
* **Statement/Function/Line Coverage Requirement:** $\ge 95\%$
* **Branch Coverage Requirement:** $\ge 80\%$ — kept lower than the other metrics because a meaningful share of remaining branches are defensive fallbacks (e.g. `?? new Error(...)` after a guard that already guarantees truthiness) that aren't reachable through real user interaction.
* **Excluded Files:** the generated OpenAPI client (`app/api/**`), composition roots and declarative config with no branching logic of their own (`app/root.tsx`, `app/routes.ts`, `app/i18n/index.ts`), and type-only modules (`**/*.d.ts`, `**/*.gen.ts`, `**/*.types.ts(x)`, `app/auth/IAuthService.ts`, `app/types/TokenParsed.ts`, `app/types/MembersFilterDto.ts`).

#### Running Tests Locally
```bash
cd Frontend
npm test              # run once
npm run test:watch    # watch mode
npm run test:coverage # run with a coverage report
```
Note: Vitest's `jsdom` environment requires Node 22.13+ or 24+; CI uses Node 22.x.

---

## Production Deployment

Tavern utilizes automated builds for production hosting. You do not need to build images manually.

### CI/CD Pipeline
GitHub Actions automatically builds, tests, and pushes optimized production containers to the GitHub Container Registry (`ghcr.io`) whenever changes are merged into the `development` or `main` branches.
* **Backend Coverage Verification:** Any pull request merging into `development` or `main` must pass the test suite and meet the minimum **95%** code coverage requirement to succeed.
* **Frontend Coverage Verification:** Pull requests also run the Vitest suite via `npm run test:coverage` and must meet the frontend coverage thresholds above to succeed.
* **Linting:** Backend (`dotnet format`) and Frontend (Biome) formatting/lint checks run as separate jobs from the test suites.

### Deploying the Stack
To deploy the stack to production, supply your production `.env` file and execute:
```bash
docker compose up -d
```

To force a local build of the production images instead of pulling from GHCR:
```bash
docker compose up -d --build
```

---

## Architecture & Documentation

For a detailed deep dive into the system's architecture, APIs, or docs:
* **Main Branch Documentation:** [docs.tavern.svsticky.nl](https://docs.koala.svsticky.nl)
* **Development Branch Documentation:** [docs.tavern.dev.svsticky.nl](https://docs.koala.dev.svsticky.nl)