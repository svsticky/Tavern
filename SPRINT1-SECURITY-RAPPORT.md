# Sprint 1 — Security & Correctness Remediation Rapport

> Generated: 2026-06-26  
> Status: **APPLIED** (2026-08-01) — all 7 findings re-integrated after the upstream merge and Koala migration work. C6's fix was verified against a real Postgres instance (not just unit tests) since the underlying drift wasn't visible to `dotnet ef migrations add` alone — see the migration's comments. C4's secret rotation covers going-forward commits only; a full git-history scrub is a separate, unexecuted decision (needs force-push + team coordination).  
> All 1048 unit tests passed with these changes applied.

---

## Summary

A 7-agent pre-release audit identified 7 CRITICAL findings. Sprint 1 addresses auth/access-control gaps and correctness bugs. No changes touch business logic beyond the specific defects listed.

---

## C1 — Unauthenticated access to all API controllers

**Severity:** CRITICAL — any anonymous caller could read/write enrollments, announcements, and payments.

**Root cause:** Three controllers had no `[Authorize]` attribute. The Mollie payment webhook must remain `[AllowAnonymous]` because Mollie does not send a Bearer token.

**Files to change:**

| File | Change |
|---|---|
| `Backend/Controllers/Enrollments.cs` | Add `using Microsoft.AspNetCore.Authorization;` and `[Authorize]` at class level |
| `Backend/Controllers/Announcements.cs` | Same |
| `Backend/Controllers/Payments.cs` | Same, plus `[AllowAnonymous]` on the `PaymentWebhook` action method only |

**Exact diff (Enrollments.cs — identical pattern for Announcements.cs):**
```csharp
+ using Microsoft.AspNetCore.Authorization;
  ...
+ [Authorize]
  public class EnrollmentsController : ControllerBase
```

**Exact diff (Payments.cs):**
```csharp
+ using Microsoft.AspNetCore.Authorization;
  ...
+ [Authorize]
  public class PaymentsController : ControllerBase
  ...
+ [AllowAnonymous]
  [HttpPost("webhook")]
  public async Task<IActionResult> PaymentWebhook(...)
```

---

## C2 — Hangfire dashboard publicly accessible

**Severity:** CRITICAL — `/hangfire` was open to any unauthenticated visitor, exposing job history, queues, and manual trigger capability.

**Root cause:** `app.UseHangfireDashboard()` was called without an `Authorization` filter.

**New file to create:** `Backend/Filters/HangfireBoardAuthorizationFilter.cs`

```csharp
using Backend.Interfaces;
using Hangfire.Dashboard;

namespace Backend.Filters;

/// <summary>
/// Restricts the Hangfire dashboard to board and candidate board members.
/// </summary>
public class HangfireBoardAuthorizationFilter : IDashboardAuthorizationFilter
{
    /// <inheritdoc/>
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        if (httpContext.User.Identity?.IsAuthenticated != true)
            return false;

        var userIdClaim = httpContext.User.Claims.FirstOrDefault(c => c.Type == "UserId");
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            return false;

        var permissionService = httpContext.RequestServices.GetRequiredService<IPermissionService>();
        return permissionService.IsBoardOrCandidateBoardMember(userId);
    }
}
```

**Change in `Backend/Program.cs`:**
```csharp
+ using Backend.Filters;
  ...
- app.UseHangfireDashboard();
+ app.UseHangfireDashboard("/hangfire", new DashboardOptions
+ {
+     Authorization = [new HangfireBoardAuthorizationFilter()]
+ });
```

---

## C3 — JWT audience not validated

**Severity:** CRITICAL — a token issued for any Keycloak client (e.g. frontend-tavern, or any other client in the realm) was accepted by the backend, enabling cross-client token reuse.

**Root cause:** `ValidateAudience = false` in `Backend/ServiceExtensions.cs`.

**Change in `Backend/ServiceExtensions.cs`:**
```csharp
- ValidateAudience = false,
+ ValidateAudience = true,
+ ValidAudiences = [Environment.GetEnvironmentVariable("KeycloakBackendClientId") ?? "backend-tavern", "account"]
```

> `"account"` is included as a fallback for production Keycloak instances not yet configured with the audience mapper. Once the mapper is deployed everywhere, `"account"` can be removed.

**Required Keycloak change (devcontainer — already in `realm-export.json` when we re-apply):**

Add an audience mapper to the `frontend-tavern` client so tokens include `backend-tavern` in the `aud` claim:

```json
{
  "name": "backend-audience",
  "protocolMapper": "oidc-audience-mapper",
  "config": {
    "included.client.audience": "backend-tavern",
    "access.token.claim": "true"
  }
}
```

**Required Keycloak change (PRODUCTION — must be done manually):**

In Keycloak Admin → Clients → `frontend-tavern` → Client scopes → Dedicated → Add mapper → By configuration → Audience → set Included Client Audience = `backend-tavern`.

---

## C4 — Secrets committed to repository

**Severity:** CRITICAL — `devcontainer.env` (containing real `KeycloakClientSecret` and `AUTH_WEBHOOK_SECRET`) was committed and is in git history.

**Immediate action required (cannot be fixed by code alone):**
1. Rotate `KeycloakClientSecret` in Keycloak Admin
2. Rotate `AUTH_WEBHOOK_SECRET` (change value in production compose + Keycloak webhook plugin config)
3. Consider using `git filter-repo` to scrub the secrets from history if the repo is public or shared

**Code changes:**

`.gitignore` — add:
```
.devcontainer/devcontainer.env
```

New file `.devcontainer/devcontainer.env.sample`:
```
PostgresqlConnectionString=Host=db;Port=5432;Database=postgres;Username=postgres;Password=postgres
KeycloakUrl=http://keycloak:8080
VITE_KeycloakUrl=http://localhost:8082
AUTH_SYSTEM=KEYCLOAK
KeycloakBackendClientId=backend-tavern
KeycloakClientSecret=<keycloak-backend-client-secret>
AUTH_WEBHOOK_SECRET=<random-secret-min-32-chars>
```

`compose.yaml` — replace hardcoded secret:
```yaml
- webhookSecret: kdfsjf*DFf9A
+ webhookSecret: ${AUTH_WEBHOOK_SECRET}
```

---

## C5 — Password reset URL broken

**Severity:** CRITICAL — `resetCredentials()` in `Frontend/app/auth/KeycloakService.tsx` constructed a Keycloak URL manually with a literal `tab_id=...` placeholder, which is invalid and caused the redirect to fail.

**Root cause:** Manual string concatenation instead of using the Keycloak JS SDK's `createLoginUrl()`.

**Change in `Frontend/app/auth/KeycloakService.tsx`:**

Replace the entire `resetCredentials()` method:
```typescript
// BEFORE (broken):
public async resetCredentials(): Promise<string> {
  const baseUrl = `${this.keycloak.authServerUrl}realms/${this.keycloak.realm}/protocol/openid-connect/auth`;
  const clientId = this.keycloak.clientId ?? "react";
  const redirectUri = encodeURIComponent(`${window.location.origin}/`);
  return `${baseUrl}?client_id=${clientId}&tab_id=...&redirect_uri=${redirectUri}`;
}

// AFTER (correct):
public async resetCredentials(): Promise<string> {
  return this.keycloak.createLoginUrl({
    action: "UPDATE_PASSWORD",
    redirectUri: `${window.location.origin}/`,
  });
}
```

---

## C6 — Unique constraint on EnrollmentPayments.MemberId prevents multiple enrollments

**Severity:** CRITICAL — EF Core's TPC inheritance put a unique constraint on `EnrollmentPayments.MemberId` (inherited from the base `Payment` entity's `HasIndex("MemberId").IsUnique()`). This prevented a member from paying for more than one enrollment.

**Root cause:** `MembershipPayment` correctly uses a unique index (one membership per member). But in TPC mode this constraint was applied to all concrete tables, including `EnrollmentPayments` where a member may have many.

**New migration file:** `Backend/Migrations/20260623000001_FixEnrollmentPaymentsMemberIndex.cs`

```csharp
public partial class FixEnrollmentPaymentsMemberIndex : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_EnrollmentPayments_MemberId",
            table: "EnrollmentPayments");

        migrationBuilder.CreateIndex(
            name: "IX_EnrollmentPayments_MemberId",
            table: "EnrollmentPayments",
            column: "MemberId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_EnrollmentPayments_MemberId",
            table: "EnrollmentPayments");

        migrationBuilder.CreateIndex(
            name: "IX_EnrollmentPayments_MemberId",
            table: "EnrollmentPayments",
            column: "MemberId",
            unique: true,
            filter: "\"MemberId\" IS NOT NULL");
    }
}
```

**Change in `Backend/Migrations/PostgresDbContextModelSnapshot.cs`:**

In the base `Payment` entity configuration, remove `.IsUnique().HasFilter("\"MemberId\" IS NOT NULL")`:
```csharp
// Base entity (applies to all tables via TPC):
- modelBuilder.Entity<Payment>().HasIndex("MemberId").IsUnique().HasFilter("\"MemberId\" IS NOT NULL");
+ modelBuilder.Entity<Payment>().HasIndex("MemberId");

// MembershipPayment only gets the unique constraint:
+ modelBuilder.Entity<MembershipPayment>().HasIndex("MemberId").IsUnique().HasFilter("\"MemberId\" IS NOT NULL");
```

---

## C7 — PromoteFromWaitingList clears all waiting-list entries

**Severity:** CRITICAL — when a spot opened up, `PromoteFromWaitingList` set `IsOnWaitingList = false` on **all** waiting-list members, not just the number being promoted.

**Root cause:** Loop iterated `next` (all waiting list members) instead of `toPromote` (the filtered, limited subset).

**Change in `Backend/Repositories/EnrollmentRepository.cs`:**
```csharp
- foreach (var enrollment in next) { enrollment.IsOnWaitingList = false; }
+ foreach (var enrollment in toPromote) { enrollment.IsOnWaitingList = false; }
```

**Test fixes required** (two tests reflected the old buggy behavior and the pre-existing inconsistent test data):

`Backend.Tests/Repositories/EnrollmentRepositoryTests.cs`:

1. `PromoteFromWaitingList_PromotesInOrderAndChangesWaitingListStatus` — old assertion expected all waiting-list entries to be cleared. Fix:
```csharp
// Before:
Assert.All(list, e => Assert.False(e.IsOnWaitingList));

// After:
Assert.False(list.First(e => e.MemberId == member1.Id).IsOnWaitingList);
Assert.True(list.First(e => e.MemberId == member2.Id).IsOnWaitingList);
```

2. `DeleteEnrollment_DeadlinePassed_ThrowsUnauthorizedAccessException` — activity had `DateTimeEnd` in the past but `DateTimeStart` still in the future; the unenrollment guard checks `DateTimeStart`. Fix:
```csharp
activity.DateTimeStart = DateTime.UtcNow.AddDays(-2);  // add this line
activity.DateTimeEnd = DateTime.UtcNow.AddDays(-1);
```

---

## Pre-existing build issue (not a Sprint 1 change)

`Backend/Backend.Tests/` and `Backend/Backend.IntegrationTests/` directories are nested **inside** `Backend/`. MSBuild's implicit `**/*.cs` glob picks up their generated `obj/` files, causing duplicate assembly attribute errors.

**Fix in `Backend/Backend.csproj`:**
```xml
<ItemGroup>
  <Compile Remove="Backend.Tests/**;Backend.IntegrationTests/**" />
</ItemGroup>
```

This must be applied before the test suite can run locally.

---

## Application order when re-applying

1. Apply `Backend.csproj` build fix (required to compile)
2. Apply C1, C2, C3, C4, C5 (independent, no migration needed)
3. Apply C6 migration + model snapshot (requires DB access — run `dotnet ef database update`)
4. Apply C7 + test fixes
5. Rotate secrets (C4 — external action)
6. Add Keycloak audience mapper to production (C3 — external action)
