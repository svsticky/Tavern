# Tavern Remote Deployment Plan

Target: `rens@100.114.12.87` (Tailscale IP), accessed via `ssh -L 4573:100.114.12.87:4573 rens@100.114.12.87`.
Topology: **self-contained** — Postgres, Keycloak (using this repo's own `realm-export.json`), and the app all run on the remote host itself, mirroring local dev rather than pointing at shared/external infra.

This is a plan, not a script — no step here has been executed. Everything below needs to be run by hand (or with you present) since it requires the SSH password and touches a shared host.

---

## 0. Open question to resolve before starting

Nothing in the repo defines what should listen on port 4573 — no compose file, `.env` default, or doc references it. The SSH command only tunnels **one** port, which implies a single ingress point for the whole stack (frontend + backend + Keycloak), not one port per service like local dev does (5173 / 8080 / 8082).

**Recommendation:** put a lightweight reverse proxy (Caddy) in front of everything, listening on `4573`, path/subdomain-routing to each service. This plan assumes that. If you'd rather just expose the frontend directly on 4573 and open separate tunnels for backend/Keycloak as needed, skip §4 and adjust `FrontendPort=4573` in `.env` instead — flag which you want before I build this out further.

---

## 1. Pre-flight — inspect the host before deciding anything else

Since I can't SSH in myself (no password, and this is a shared host I shouldn't touch without you there), the first real step is you (or us together, live) checking:

```bash
ssh rens@100.114.12.87
docker --version && docker compose version   # Docker present?
docker ps -a                                  # anything already running/using ports 4573, 5432, 8080, 8082?
df -h                                         # disk space — Postgres + Keycloak + images add up
nproc && free -h                              # sanity check on resources
```

If Docker isn't installed, that's a prerequisite step (install Docker Engine + Compose plugin) before anything else here applies.

---

## 2. Get the code onto the host

```bash
# on the remote host
git clone git@github.com:svsticky/Tavern.git tavern
cd tavern
git checkout development   # after today's merge is pushed
```

Requires the host to have a deploy key or your SSH agent forwarded (`ssh -A`) for GitHub access. Simpler alternative: `git archive` + `scp`, or `rsync -avz` the working tree from your machine if you don't want to deal with GitHub auth on the remote box.

---

## 3. Write the deployment compose file

`.devcontainer/compose.yaml` isn't directly reusable — it bind-mounts the workspace into a `sleep infinity` devcontainer and runs the app via `dotnet run`/`npm run dev` inside it. For a persistent remote deployment we want the **built** images (`Backend/Dockerfile`, `Frontend/Dockerfile`) instead, combined with the local-like supporting services.

Create `deploy/compose.yaml` (new file, not in the repo yet):

```yaml
services:
  db:
    image: postgres:17
    restart: unless-stopped
    volumes:
      - postgres-data:/var/lib/postgresql/data/
    environment:
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
      POSTGRES_USER: postgres
      POSTGRES_DB: postgres
    networks: [tavern-net]

  keycloak:
    image: quay.io/keycloak/keycloak:26.1
    restart: unless-stopped
    command: start --import-realm --features=preview --hostname-strict=false --proxy-headers=xforwarded
    environment:
      KC_BOOTSTRAP_ADMIN_USERNAME: admin
      KC_BOOTSTRAP_ADMIN_PASSWORD: ${KEYCLOAK_ADMIN_PASSWORD}
      KC_DB: postgres
      KC_DB_URL: jdbc:postgresql://db:5432/postgres
      KC_DB_USERNAME: postgres
      KC_DB_PASSWORD: ${POSTGRES_PASSWORD}
      webhookUrl: "http://tavern-backend:8080/members/webhook/refresh-email"
      webhookSecret: ${AUTH_WEBHOOK_SECRET}
    volumes:
      - ../.devcontainer/keycloak-setup:/opt/keycloak/data/import
      - ../.devcontainer/keycloak-plugins:/opt/keycloak/providers
    depends_on: [db]
    networks: [tavern-net]

  tavern-backend:
    build: { context: .., dockerfile: Backend/Dockerfile }
    restart: unless-stopped
    env_file: [../.env]
    environment:
      - PostgresqlConnectionString=Host=db;Username=postgres;Password=${POSTGRES_PASSWORD};Database=postgres
      - ASPNETCORE_ENVIRONMENT=Production
    depends_on: [db, keycloak]
    networks: [tavern-net]

  tavern-frontend:
    build: { context: ../Frontend, dockerfile: Dockerfile }
    restart: unless-stopped
    environment:
      - VITE_AUTH_SYSTEM=KEYCLOAK
      - VITE_KeycloakUrl=${KeycloakUrl}
      - VITE_KeycloakRealm=master
      - VITE_KeycloakClientId=frontend-tavern
      - VITE_HostUrl=${HostUrl}
      - VITE_ApiUrl=${ApiUrl}
    depends_on: [tavern-backend]
    networks: [tavern-net]

  caddy:
    image: caddy:2
    restart: unless-stopped
    ports: ["4573:4573"]
    volumes:
      - ./Caddyfile:/etc/caddy/Caddyfile
    depends_on: [tavern-frontend, tavern-backend, keycloak]
    networks: [tavern-net]

volumes:
  postgres-data:

networks:
  tavern-net:
    driver: bridge
```

Note: no `localstack`/S3 mock here — decide in §5 whether this deployment uses real S3 or you still want LocalStack for a staging environment. Also no `ngrok` — not needed once the host is reachable via the SSH tunnel/Tailscale directly (Mollie webhook reachability is a separate concern, see §6).

---

## 4. Reverse proxy — single port 4573

`deploy/Caddyfile`:

```
:4573 {
    handle /auth* {
        reverse_proxy keycloak:8080
    }
    handle /api* {
        uri strip_prefix /api
        reverse_proxy tavern-backend:8080
    }
    handle {
        reverse_proxy tavern-frontend:3000
    }
}
```

This is a starting point, not final — the frontend's `VITE_ApiUrl`/`VITE_KeycloakUrl` need to agree with these paths (e.g. `ApiUrl=http://localhost:4573/api`, `KeycloakUrl=http://localhost:4573/auth`), and the Keycloak realm's client redirect URIs (`realm-export.json`) are currently hardcoded to `http://localhost:5173/*` and `http://localhost:8080/*` — **these need updating** to match whatever `HostUrl` this deployment uses, or Keycloak will reject login redirects.

---

## 5. Environment configuration

Copy `sample.env` → `.env` on the host and fill in, at minimum:

| Variable | Value for this deployment |
|---|---|
| `HostUrl` | `http://localhost:4573` (as seen through your SSH tunnel) |
| `ApiUrl` | `http://localhost:4573/api` |
| `KeycloakUrl` | `http://localhost:4573/auth` |
| `KeycloakRealm` | `master` |
| `KeycloakClientId` | `frontend-tavern` |
| `KeycloakBackendClientId` | `backend-tavern` |
| `KeycloakClientSecret` | from `realm-export.json`'s `backend-tavern` client secret, or rotate it |
| `AUTH_WEBHOOK_SECRET` | matches `webhookSecret` used in the Keycloak service env above |
| `PostgresqlConnectionString` | set via compose env, not needed directly in `.env` |
| `S3_*` | real S3 bucket credentials, or point at a LocalStack service if you add one back |
| `ACCOUNTING_ENABLED` | `false` unless this environment needs it |

Do **not** reuse `.devcontainer/devcontainer.env`'s values as-is in production — those are dev-only secrets (`admin`/`admin` Keycloak password, hardcoded client secrets committed to the repo).

---

## 6. Bring it up

```bash
cd tavern/deploy
docker compose up -d --build
docker compose logs -f tavern-backend   # watch migrations apply cleanly against real Postgres
```

Watch specifically for the migration chain applying without error — the backend auto-migrates on startup (confirmed today: I applied the full chain, 6 migrations, against a real Postgres instance and it succeeded cleanly).

---

## 7. Verify

- [ ] `http://localhost:4573` loads the frontend (through the tunnel)
- [ ] Login redirects to Keycloak (`/auth`) and back correctly
- [ ] `http://localhost:4573/api/swagger` reachable, backend healthy
- [ ] Create/view an activity as a test — confirms DB + auth + API wiring end-to-end
- [ ] Check `docker compose logs keycloak` for realm import success

---

## 8. Explicitly out of scope here

- **Koala data migration** (MIGRATION-PLAN.md) — this stands up an *empty* Tavern instance. Migrating real member/activity/payment data is the separate, already-planned 10-phase effort; run it against this environment only once the stack itself is confirmed healthy.
- **`storage.zip` (13GB)** — if this deployment needs the migrated Active Storage files, that transfer is its own step (rsync to the host or directly to S3), not part of standing up the stack.
- **TLS / public exposure** — this plan only covers reachability through your SSH tunnel. If this needs to be reachable outside of Tailscale/SSH, that's a follow-up (Caddy can auto-provision TLS, but needs a real domain pointed at the host).

---

## 9. Rollback

Everything here is additive to a fresh host (new containers, new named volumes). If something goes wrong: `docker compose down -v` on the remote host removes it cleanly — no shared state to protect on a fresh deploy. Once the Koala migration runs against this environment, that stops being true, so keep that phase clearly separated (a Postgres volume snapshot before migrating is worth doing at that point, not now).
