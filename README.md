# Tavern

[![Build and Test Status](https://github.com/svsticky/tavern/actions/workflows/test.yml/badge.svg)](https://github.com/svsticky/tavern/actions/workflows/test.yml)
![Target Framework](https://img.shields.io/badge/.NET-8.0-blue)
![Frontend Tech](https://img.shields.io/badge/Frontend-React%20%2B%20Vite-cyan)
![Code Coverage](https://img.shields.io/badge/Line%20Coverage-%E2%89%A595%25-green)

> **Tavern** is the central platform for **Sticky** members to come together, explore activities, manage enrollments, and coordinate events.

---

## Key Features

* **Event & Activity Management:** Create, schedule, and configure activities visible on the platform and external calendar feeds.
* **Enrollment & Waiting Lists:** Automated enrollments with real-time capacity monitoring and waiting list processing.
* **Secure Authentication:** Integrated single sign-on (SSO) and user lifecycle synchronization powered by Keycloak.
* **Integrated Payments:** Smooth checkout experience for paid activities and annual memberships via Mollie API.
* **Robust Background Workers:** Hangfire-managed transactional outbox workers syncing payments, mail subscriptions, and accounting tools.
* **S3 File Storage:** Upload and manage profile pictures and group graphics securely on AWS S3 (mocked locally using LocalStack).

---

## Tech Stack & Ecosystem

To replicate the production environment perfectly, the local development setup orchestrates several services inside Docker:

* **Backend:** ASP.NET 8.0 Web API with PostgreSQL 17 (Entity Framework Core)
* **Frontend:** React SPA built with Vite
* **Identity Provider:** Keycloak 26.1 (with custom themes/plugins)
* **Object Storage:** LocalStack 3.0 (mocking AWS S3)
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
* **Payments & Accounting:**
  * `PAYMENT_PROVIDER`: Payment provider choice (e.g., `MOLLIE`).
  * `MollieApiKey`: API key required for Mollie payment gateway integration.
  * `ACCOUNTING_SERVICE`, `EXACT_ACCESS_TOKEN`, `EXACT_DIVISION`: Credentials for external accounting sync (e.g., Exact Online). This variables are optional and only needed if you use an accounting service.
* **Theme & UI Customization:**
  * `LOGO_URL`, `BOARD_PRIMARY_LIGHT`, `BOARD_PRIMARY`, `BOARD_PRIMARY_DARK`: Branding configuration variables injected into the Vite frontend.
* **Object Storage (S3 / LocalStack):**
  * `AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`, `AWS_REGION`, `S3_SERVICE_URL`: Credentials and endpoints for AWS S3 object storage (defaults are configured to run with LocalStack locally).
* **Mail Services (optional for development, needed if you want mailing to work):**
  * `MAIL_SERVICE`: Selector for the mail service (`SMTP` or `MAILGUN`).
  * `SMTP_HOST`, `SMTP_PORT`, `SMTP_STARTTLS`, `SMTP_USER`, `SMTP_PASS` / `MAILGUN_TOKEN`, `MAILGUN_PUBLIC_KEY`, `MAILGUN_API_BASE_URL`: Server and API configurations for sending system emails.
* **External Integrations:**
  * `MAIL_SUBSCRIPTION_SERVICE`, `MAILCHIMP_LIST_KEY`, `MAILCHIMP_API_KEY`: Credentials for newsletter subscription sync (e.g., Mailchimp). These are optional and only needed if you want to work it with a mail subscription service.
  * `NGROK_AUTHTOKEN`, `NGROK_URL`: Auth token and public tunnel URL used by Ngrok for local webhook testing. These are optional, but can be usefull for letting mollie call the webhook after a payment.


### 3. Launch the Devcontainer
* Open the project directory in VS Code or JetBrains Rider.
* When prompted, select **"Reopen in Container"** (or press `Ctrl+Shift+P` -> type `Dev Containers: Reopen in Container`).
* Docker Compose will automatically spin up all services (`db`, `localstack`, `keycloak`, and `ngrok`).

### 4. Start the Application
Once the devcontainer is running, follow these steps to start the application and configure the default local user:

#### Step A: Run the Backend
Start the ASP.NET Core backend. This will automatically apply database migrations and create the default `BACKUP_ACCOUNT` user in Keycloak:
```bash
dotnet run --project Backend
```

#### Step B: Configure the Local Keycloak User
As keycloak in the devcontainer isn't connected to a mailing service, you can't verify the email and set a password. To log in with the newly created user, you need to set their password and verify their email address in Keycloak:
1. Open the local Keycloak Admin console at [http://localhost:8082](http://localhost:8082).
2. Log in with the admin credentials:
   - **Username**: `admin`
   - **Password**: `admin`
3. Navigate to **Users** in the sidebar, search for the user matching your configured `BACKUP_ACCOUNT_EMAIL`, and click on their username.
4. Go to the **Credentials** tab, click **Reset password**, enter your desired password, and toggle **Temporary** to **Off**.
5. Go to the **Details** tab, toggle **Email Verified** to **On**, and save the changes.

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
| **Ngrok Dashboard** | [http://localhost:4040](http://localhost:4040) | Check active public tunnels |

---

## Testing & Code Coverage

Backend tests are run using **xUnit** and code coverage is tracked via **Coverlet**. 

### Running Tests Locally
To execute the backend test suite and calculate code coverage:
```bash
dotnet test
```

### Exclusions & Coverage Requirements
* **Line Coverage Requirement:** $\ge 95\%$
* **Excluded Classes:** Infrastructure and startup elements (e.g., `Program`, `DatabaseSeeder`, `ServiceExtensions`, `SMTPMailService`, and `MailgunService`) are excluded from coverage statistics.
* **Parallelization:** Disabled (`DisableTestParallelization = true`) to prevent race conditions during process-wide environment variable updates.

---

## Production Deployment

Tavern utilizes automated builds for production hosting. You do not need to build images manually.

### CI/CD Pipeline
GitHub Actions automatically builds, tests, and pushes optimized production containers to the GitHub Container Registry (`ghcr.io`) whenever changes are merged into the `development` or `main` branches.
* **Backend Coverage Verification:** Any pull request merging into `development` or `main` must pass the test suite and meet the minimum **95%** code coverage requirement to succeed.

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
* **Main Branch Documentation:** [docs.tavern.svsticky.nl](https://docs.tavern.svsticky.nl)
* **Development Branch Documentation:** [docs.tavern.dev.svsticky.nl](https://docs.tavern.dev.svsticky.nl)