# Koala → Tavern Migration Plan

> **Status:** FINALISED — all 18 questions answered (see `MIGRATION-QUESTIONS.md`). Ready for execution.  
> **Source:** `koala_prod.sql` (PostgreSQL 13, dumped 2026-06-26)  
> **Target:** Tavern (ASP.NET 8 / EF Core / PostgreSQL 17 + Keycloak 26)

---

## 1. System overview

| | Koala (source) | Tavern (target) |
|---|---|---|
| Framework | Ruby on Rails | ASP.NET 8 |
| Auth | Devise (bcrypt) in `users` table | Keycloak OIDC; UUID stored in `Members.AuthSystemUserId` |
| Database | PostgreSQL 13, integer PKs | PostgreSQL 17, UUID PKs |
| Files | Rails Active Storage — `service_name='local'` | AWS S3 (LocalStack in dev) |
| Payments | Mollie (via Koala) | Mollie (via Tavern) |

---

## 2. Data volumes

| Koala table | Rows | Tavern target | Migrate? |
|---|---|---|---|
| `members` | 4,057 | `Members` | ✅ |
| `users` | 2,960 (2,944 Member + 16 Admin) | Keycloak + `Members.AuthSystemUserId` | ✅ (Member type only) |
| `groups` | 81 | `Groups` | ✅ |
| `group_members` | 3,374 | `GroupMemberships` + `RoleAliases` | ✅ (220 have `member_id IS NULL` — not migrated, see §4) |
| `studies` | 13 | `Studies` | ✅ |
| `educations` | 7,388 | `StudyEnrollments` | ✅ (2,036 have `member_id IS NULL` — not migrated, see §4) |
| `activities` | 2,165 | `Activities` | ✅ |
| `participants` | 53,439 | `Enrollments` | ✅ (5,326 have `member_id IS NULL` — not migrated, see §4) |
| `payments` | 12,414 | `MembershipPayments` / `EnrollmentPayments` | ⚠️ deduplicated to 1 row per member per type (unique constraint on `MemberId`) — most recent paid, else most recent pending; earlier rows are not migrated |
| `posts` | 78 | `Announcements` | ✅ |
| `tags` | 46 | Boolean flags on `Members` + `Suspended` | ✅ |
| `active_storage_blobs` | ~4,250 | S3 bucket | ✅ rsync from `/var/www/koala.svsticky.nl/storage/` |
| `admins` | 16 | No equivalent — auto-match to `members` by name | partial |
| `checkout_*` | ~10k rows | No equivalent | ❌ skip — archived in Koala DB |
| `impressions` | large | No equivalent | ❌ skip |
| `oauth_*` | stale tokens | Superseded by Keycloak | ❌ skip |
| `tokens` | — | Superseded by Keycloak | ❌ skip |
| `settings` | — | Tavern `Settings` (manual, not migrated) | ❌ skip |

---

## 3. Schema field mapping

### 3.1 `members` → `Members`

| Koala field | Tavern field | Notes |
|---|---|---|
| `id` (int) | — | Used only as join key during migration; not stored in Tavern |
| *(derived)* | `Id` (UUID) | `gen_random_uuid()` per member |
| *(from users)* | `AuthSystemUserId` (UUID) | Keycloak user UUID written back after provisioning |
| `student_id` | `StudentNumber` | 9 nulls → `UNKNOWN-{koala_id}`; flagged in post-migration report |
| `first_name` | `FirstName` | |
| `infix` | *(no field)* | Prepend to `LastName`: `COALESCE(infix||' ','')||last_name` |
| `last_name` | `LastName` | Combined with infix |
| `email` | `Email` | 9 duplicates: keep newer (higher id); see `MIGRATION-DUPLICATES.md` |
| `phone_number` | `PhoneNumber` | |
| `emergency_phone_number` | `ParentPhoneNumber` | semantic match for minors |
| `address` | `Street` | |
| `house_number` | `HouseNumber` | |
| `postal_code` | `PostalCode` | |
| `city` | `City` | |
| `birth_date` | `DateOfBirth` | |
| `join_date` | `RegisteredOn` | |
| `comments` | `Notes` | |
| *(tag value 1, pardon)* | `Gratie = true` | |
| *(tag value 2, merit)* | `LidVanVerdienste = true` | |
| *(tag value 4, donator)* | `Begunstiger = true` | |
| *(tag value 5, suspended)* | `Suspended = true` | Overrides the default false |
| *(tag value 3, honorary)* | `EreLid` — no prod data, skip | |
| *(default)* | `Suspended` = false | No Koala equivalent; soft-deleted users → Suspended=true |
| *(from users.language)* | `PreferredLanguage` | 0→NL, 1→EN; members without users → NL |
| *(default)* | `MailSubscriptions` = 0 | None; Mailchimp sync added separately by team |
| `consent` | *(discard)* | No Tavern equivalent; GDPR consent restarts in Tavern |

### 3.2 `groups` → `Groups`

| Koala field | Tavern field | Notes |
|---|---|---|
| `id` (int) | — | Join key only |
| `name` | `Name` | |
| `category` | `Type` | 1→Committee (Bestuur is special), 2→Committee, 3→Dispute, 4→WorkingGroup |
| `ledgernr` | `DefaultGLAccount` | |
| `cost_location` | `DefaultCostCenter` | |
| `comments` | *(discard)* | No Notes field on Group |
| *(derived)* | `Active` = true | All groups migrated as active; mark inactive ones manually |

**Group category mapping:**

| Koala `category` | Group name(s) | Tavern `GroupType` |
|---|---|---|
| 1 | Bestuur (id=1) | `Committee` |
| 2 | Committees (63 groups) | `Committee` |
| 3 | Disputes (8 groups, e.g. "Muziekdispuut C#", "Damesdispuut Fiore") | `Dispute` |
| 4 | Working groups (9 groups) | `WorkingGroup` |

### 3.3 `group_members` → `GroupMemberships` + `RoleAliases`

| Koala field | Tavern field | Notes |
|---|---|---|
| `member_id` | `MemberId` | Via legacy→UUID map |
| `group_id` | `GroupId` | Via legacy→UUID map |
| `year` | `MembershipYear` | |
| `position` | `RoleAlias.Name` | Collect all unique positions, create RoleAlias records, then FK |

Unique `position` values will be extracted, deduplicated, and inserted into `RoleAliases` with auto-assigned `Role.Id`. The `RoleAliasId` FK on `GroupMemberships` will point to the matching alias.

### 3.4 `studies` → `Studies`

| Koala field | Tavern field | Notes |
|---|---|---|
| `code` | `Title` | Short codes like "INCA", "INKU"; Tavern has no separate code field |
| `masters` | `Type` | true→Master, false→Bachelor |
| `active` | *(discard)* | All studies migrated; inactive ones noted post-migration |
| `id` | — | Join key only |

### 3.5 `educations` → `StudyEnrollments`

| Koala field | Tavern field | Notes |
|---|---|---|
| `member_id` | `MemberId` | Via legacy→UUID map |
| `study_id` | `StudyId` | Via legacy→UUID map |
| `start_date` | `EnrollmentDate` | Converted to DateTimeOffset (UTC midnight) |
| `end_date` | `CompletionDate` | Nullable |
| `status` | `Status` | See mapping below |

**Status mapping** (from `constipated-koala/app/models/education.rb`):

| Koala | Koala name | Count | Tavern |
|---|---|---|---|
| 0 | `active` | 1,963 | `Enrolled` (0) |
| 1 | `stopped` | 1,024 | `DroppedOut` (2) |
| 2 | `graduated` | 3,557 | `Completed` (1) |
| 3 | `inactive` | 844 | `Enrolled` (0) — registered but not actively studying; no Tavern equivalent |

### 3.6 `activities` → `Activities`

| Koala field | Tavern field | Notes |
|---|---|---|
| `name` | `Name` | |
| `price` | `Price` | |
| `description_nl` | `DutchDescription` | |
| `description_en` | `EnglishDescription` | |
| `start_date` + `start_time` | `DateTimeStart` | Combine into UTC datetime |
| `end_date` + `end_time` | `DateTimeEnd` | Combine into UTC datetime |
| `participant_limit` | `ParticipantLimit` | |
| `location` | `Location` | |
| `is_enrollable` | `IsEnrollable` | |
| `show_on_website` | `ShowInKoala` | |
| `unenroll_date` | `UnenrollmentDeadline` | |
| `payment_deadline` | `PaymentDeadline` | |
| `open_date` + `open_time` | `EnrollOpenDate` | Combine; if null, EnrollOpenDate=null |
| `is_masters` | Contributes to `AllowedAudience` | See audience mapping below |
| `is_freshmans` | Contributes to `AllowedAudience` | |
| `is_sophomores` | Contributes to `AllowedAudience` | |
| `is_seniors` | Contributes to `AllowedAudience` | |
| `organized_by` | `OrganizerId` | Field already exists in Tavern (nullable `uint?`); map via `_group_map` |
| `is_alcoholic`, `is_borrel` | *(discard)* | No Tavern equivalent |
| `notes` | *(discard or DutchDescription append)* | Admin notes, not public description |
| `cost_unit` | *(discard)* | No Tavern equivalent |
| `include_in_weekoverzicht` | *(discard)* | No Tavern equivalent |
| `show_participants` | *(discard)* | No Tavern equivalent |
| `VAT` | *(discard)* | No Tavern equivalent |
| `comments` | *(discard)* | Internal admin comments |

**Audience flag mapping:**

```
AllowedAudience =
  (is_freshmans=true  → FirstYears  =  1) |
  (is_sophomores=true → SecondYears =  2) |
  (is_seniors=true    → ThirdYearsAndAbove = 4) |
  (is_masters=true    → Masters     =  8) |
  (none set OR all false → All      = 63)
```

### 3.7 `participants` → `Enrollments`

| Koala field | Tavern field | Notes |
|---|---|---|
| `member_id` | `MemberId` | Via legacy→UUID map |
| `activity_id` | `ActivityId` | Via legacy→int map (Activity.Id is uint) |
| `price` | `Price` | |
| `created_at` | `RegisteredOn` | |
| `reservist` | `IsOnWaitingList` | true→true |
| `notes` | *(discard)* | Historical free-text not migrated |
| `paid` | *(discard)* | Replaced by `EnrollmentPayments.PaidAt` |

### 3.8 `payments` → `MembershipPayments` / `EnrollmentPayments`

All 12,414 rows migrated (all statuses). Stub values used for missing required fields.

| Koala field | Tavern field | Notes |
|---|---|---|
| `member_id` | `MemberId` | Via legacy→UUID map |
| `amount` | `Price` | |
| `trxid` / `transaction_id` | `PaymentServiceId` | `trxid` for Mollie, else `token`; if both null → `'LEGACY-{hash}'` |
| `redirect_uri` | `PaymentIntentUrl` | Null → `'https://legacy-import'` |
| `updated_at` | `PaidAt` | For status=2 only; null for pending |
| `status` | *(drive `PaidAt`)* | 0=pending→PaidAt=null, 2=paid→PaidAt=updated_at |
| `payment_type=0` | → `MembershipPayments` table | |
| `payment_type=1` | → `EnrollmentPayments` table | |
| — | `ManuallyMarkedAsPaid` = true | All migrated payments treated as manual |

### 3.9 `posts` → `Announcements`

| Koala field | Tavern field | Notes |
|---|---|---|
| `title` | `Title` | |
| `content` | `Content` | HTML content |
| `published_at` | `PublicationDate` | (check Tavern Announcement model fields) |
| `pinned` | `IsPinned` | (check Tavern Announcement model fields) |
| `author_id` (Admin) | `CreatedById` | Auto-match admin by name to member; unmatched → synthetic Bestuur member |

---

## 4. Data quality issues to resolve before migration

| Issue | Count | Resolution |
|---|---|---|
| Duplicate emails in `members` | 0 | None — all 4,057 emails are unique; no action needed |
| Null `student_id` | 9 | Assign `UNKNOWN-{koala_id}`; see list below |
| Soft-deleted users (`deleted_at` not null) | 7 | Migrate as `Suspended = true`; no Keycloak account (`AuthSystemUserId` = null) |
| Members with no user account | ~1,113 | Create Keycloak accounts with no password; `requiredActions: ["UPDATE_PASSWORD"]` |
| `educations` with null `member_id` | **2,036** (not 1 — see note) | Not migrated; `03_migrate_data.sql` Phase 5 filters `WHERE e.member_id IS NOT NULL` |
| `participants` with null `member_id` | **5,326** | Not migrated; the `JOIN _member_map` in Phase 7 silently excludes them |
| `group_members` with null `member_id` | **220** | Not migrated; the `JOIN _member_map` in Phase 4 silently excludes them |

> **Note on the counts above:** the original analysis (and MIGRATION-PLAN.md's earlier draft) only found 1 orphaned `educations` row. Re-verified against the current `koala_prod.pgdump` on 2026-08-01 during a full local dry-run: the real numbers are far larger across all three tables. In every case the row's `member_id` column is genuinely `NULL` in Koala's own data — not a dangling reference to a deleted member (verified: 0 rows reference a non-existent, non-null member id; see queries below). None of Tavern's target tables (`Enrollments`, `StudyEnrollments`, `GroupMemberships`) allow a null `MemberId`, so these rows structurally cannot carry over and are silently dropped by the existing `JOIN`/`WHERE` clauses in `migration/03_migrate_data.sql` — this is expected, not a migration bug.
>
> `participants` is the highest-stakes of the three: the 5,326 orphaned rows span `created_at` from 2014-08-30 to 2025-11-11 and sum to **€22,146.36** in `price`. There is no free-text name/identifier column on `participants` to recover who these belonged to (checked: `id, member_id, activity_id, price, paid, created_at, updated_at, reservist, notes` — `notes` was empty on the sampled rows). If this money/attendance history needs to be preserved, it would have to be reconciled from something outside this dump (e.g. Mollie's own transaction records), not from Koala's Postgres data.

**How to find these instances yourself** (run against the loaded `koala` schema, e.g. inside the devcontainer via `PGPASSWORD=postgres psql -h db -U postgres -d postgres`):

```sql
-- Count of null-member_id rows per table
SELECT 'participants'   AS tbl, COUNT(*) FROM koala.participants   WHERE member_id IS NULL
UNION ALL
SELECT 'educations',           COUNT(*) FROM koala.educations     WHERE member_id IS NULL
UNION ALL
SELECT 'group_members',        COUNT(*) FROM koala.group_members  WHERE member_id IS NULL;

-- Confirm there are no *non-null but dangling* member_id references (would be a different, worse problem)
SELECT 'participants'   AS tbl, COUNT(*) FROM koala.participants p
  WHERE p.member_id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM koala.members m WHERE m.id = p.member_id)
UNION ALL
SELECT 'educations',            COUNT(*) FROM koala.educations e
  WHERE e.member_id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM koala.members m WHERE m.id = e.member_id)
UNION ALL
SELECT 'group_members',         COUNT(*) FROM koala.group_members gm
  WHERE gm.member_id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM koala.members m WHERE m.id = gm.member_id);

-- Financial significance of the orphaned participants rows
SELECT COUNT(*), SUM(price), MIN(created_at), MAX(created_at)
FROM koala.participants WHERE member_id IS NULL;

-- List the actual orphaned rows, e.g. for participants:
SELECT * FROM koala.participants WHERE member_id IS NULL ORDER BY created_at DESC;
```

**Members with null student_id (post-migration these need real student numbers):**

| Koala id | Name | Email | Synthetic StudentNumber |
|---|---|---|---|
| 1828 | Maaike Horst | maaikehorst@hotmail.com | UNKNOWN-1828 |
| 2610 | Mathijs Hes | mathijs_hes@live.nl | UNKNOWN-2610 |
| 2611 | Boudewijn Simons | boudewijn.simons@hotmail.com | UNKNOWN-2611 |
| 2638 | Jiansen Zheng | jiansenzheng@gmail.com | UNKNOWN-2638 |
| 2941 | Jotte Sonneveld | sticky@jotte.net | UNKNOWN-2941 |
| 2947 | Max Meijers | smax5@live.nl | UNKNOWN-2947 |
| 4247 | Laura Bosch | l.c.vandenbosch@gmail.com | UNKNOWN-4247 |
| 5724 | Capser Herwaarden | caspervanherwaarden2004@gmail.com | UNKNOWN-5724 |
| 5787 | Hidde Weide | hwei017@gmail.com | UNKNOWN-5787 |

---

## 5. Migration phases

### Phase 0 — Pre-migration tasks

**Human tasks (before running any SQL):**
1. Build and deploy the Keycloak bcrypt credential provider SPI JAR to the Keycloak instance
3. Rsync Active Storage files from Koala server to S3:
   ```bash
   rsync -avz user@koala.svsticky.nl:/var/www/koala.svsticky.nl/storage/ s3://tavern-bucket/legacy/ --exclude "*.variant_record"
   ```
4. Take a full snapshot of the Tavern DB (empty + migrations applied) before starting

### Phase 1 — Reference data (no foreign key dependencies)

**Step 1a: Studies**
```sql
INSERT INTO "Studies" (Id, Title, NominalDurationYears, Type)
SELECT
  gen_random_uuid(),
  code,
  CASE WHEN masters THEN 2 ELSE 3 END,  -- nominal years: Master=2, Bachelor=3
  CASE WHEN masters THEN 1 ELSE 0 END   -- StudyType: 0=Bachelor, 1=Master
FROM koala.studies;
```
Store Koala `id` → Tavern UUID mapping in a temp table `_study_map(koala_id, tavern_id)`.

**Step 1b: Groups**
```sql
INSERT INTO "Groups" (Id, Name, Type, DefaultGLAccount, DefaultCostCenter, Active, GroupPicturePath, GroupPictureFileName)
SELECT
  gen_random_uuid(),
  name,
  CASE category
    WHEN 3 THEN 2   -- Dispute
    WHEN 4 THEN 1   -- WorkingGroup
    ELSE 0          -- Committee (includes Bestuur with category=1 and all category=2)
  END,
  NULLIF(ledgernr, ''),
  NULLIF(cost_location, ''),
  true,
  NULL, NULL
FROM koala.groups;
```
Store `_group_map(koala_id, tavern_id)`.

**Step 1c: Role aliases from group_members.position**
```sql
-- Collect unique non-null positions
INSERT INTO "Roles" (Id, Name)
SELECT gen_random_uuid(), position
FROM (SELECT DISTINCT "position" FROM koala.group_members WHERE "position" IS NOT NULL AND "position" != '') p;

INSERT INTO "RoleAliases" (Id, RoleId, Name)
SELECT gen_random_uuid(), r."Id", r."Name"
FROM "Roles" r;
```
Store `_role_alias_map(position_text, tavern_role_alias_id)`.

### Phase 2 — Members

**Step 2a: Insert Tavern Member records**
```sql
-- Build from members + left join users for language
INSERT INTO "Members" (
  Id, StudentNumber, FirstName, LastName, Email,
  PhoneNumber, ParentPhoneNumber, Street, HouseNumber,
  PostalCode, City, DateOfBirth, RegisteredOn, Notes,
  PreferredLanguage, MailSubscriptions,
  Gratie, LidVanVerdienste, EreLid, Begunstiger,
  Suspended, AuthSystemUserId
)
SELECT
  gen_random_uuid(),
  COALESCE(m.student_id, 'UNKNOWN-' || m.id::text),  -- Q-C2 strategy A
  m.first_name,
  COALESCE(m.infix || ' ', '') || m.last_name,
  m.email,
  m.phone_number,
  m.emergency_phone_number,
  m.address,
  m.house_number,
  m.postal_code,
  m.city,
  m.birth_date,
  COALESCE(m.join_date, m.created_at::date),
  m.comments,
  CASE WHEN u.language = 1 THEN 1 ELSE 0 END,  -- 0=NL, 1=EN
  0,  -- MailSubscriptions.None (Q-C11)
  -- Tags: populated in Step 2b after Q-B1 is answered
  false, false, false, false,
  CASE WHEN u.deleted_at IS NOT NULL THEN true ELSE false END,
  NULL  -- AuthSystemUserId filled in Phase 3 after Keycloak provisioning
FROM koala.members m
LEFT JOIN koala.users u ON u.credentials_type = 'Member' AND u.credentials_id = m.id
-- Exclude duplicated emails (resolved in Phase 0)
;
```
Store `_member_map(koala_id, tavern_uuid)`.

**Step 2b: Apply tag flags**
```sql
-- pardon (1) → Gratie
UPDATE "Members" m SET "Gratie" = true
FROM _member_map mm JOIN koala.tags t ON t.member_id = mm.koala_id AND t.name = 1
WHERE m."Id" = mm.tavern_uuid;

-- merit (2) → LidVanVerdienste
UPDATE "Members" m SET "LidVanVerdienste" = true
FROM _member_map mm JOIN koala.tags t ON t.member_id = mm.koala_id AND t.name = 2
WHERE m."Id" = mm.tavern_uuid;

-- donator (4) → Begunstiger
UPDATE "Members" m SET "Begunstiger" = true
FROM _member_map mm JOIN koala.tags t ON t.member_id = mm.koala_id AND t.name = 4
WHERE m."Id" = mm.tavern_uuid;

-- suspended (5) → Suspended (overrides default false set in Step 2a)
UPDATE "Members" m SET "Suspended" = true
FROM _member_map mm JOIN koala.tags t ON t.member_id = mm.koala_id AND t.name = 5
WHERE m."Id" = mm.tavern_uuid;
-- Note: honorary (3) = EreLid; no rows in production data, skip
```

### Phase 3 — Keycloak provisioning

**Strategy: bcrypt import + transparent re-hash on first login**

The custom Keycloak SPI JAR (see Phase 0) enables bcrypt credential verification. On first login, Keycloak re-hashes the password to PBKDF2 and replaces the stored credential. Members see no difference.

**Step 3a: Members with Koala user accounts** (inner join `users WHERE credentials_type='Member'`)
- `deleted_at IS NULL` → create Keycloak user, import bcrypt hash via credential provider API
- `deleted_at IS NOT NULL` → skip (already set `Suspended=true` in Phase 2; no Keycloak account)

**Step 3b: Members without Koala user accounts** (~1,113 members)
- Create Keycloak user with `requiredActions: ["UPDATE_PASSWORD"]` and `emailVerified: false`
- No password set; member must use "Forgot password" to set one on first login

In both cases, after provisioning store returned Keycloak `user.id` (UUID):
```sql
UPDATE "Members" m
SET "AuthSystemUserId" = mm.keycloak_uuid
FROM _member_map mm
WHERE m."Id" = mm.tavern_uuid AND mm.keycloak_uuid IS NOT NULL;
```

The Keycloak provisioning is best done via a migration script (Python/Node) calling the Keycloak Admin REST API in batches of 100 to avoid rate limits. Implemented in `migration/04_provision_keycloak.js`; the synthetic `bestuur@svsticky.nl` member (created in Phase 9) is intentionally excluded — it's an announcement-author placeholder, not a real login.

**Local dry-run result (2026-08-01):** 4,044 / 4,056 provisioned successfully (2,942 with bcrypt hash import, 1,102 with `UPDATE_PASSWORD`). 12 failed — all genuine Koala data-quality issues, not script bugs:

| Email in Koala | Name | Issue |
|---|---|---|
| `unknown1` through `unknown10` (10 members) | real names, e.g. Jochem Oosterveen, Tim Bogers, Georg Winters... | Literal string `"unknownN"` stored as the email — not a placeholder row, a real member with no real email on file |
| `alexander..bogicevic@gmail.com` | Alexander Bogicevic | Double dot in email fails Keycloak's `error-invalid-email` validation |
| `kloudyes@live.com` | Yun (Kate) Feng | Parentheses in `FirstName` fail Keycloak's `error-person-name-invalid-character` validation |

These 12 members exist in Tavern's `Members` table but have `AuthSystemUserId = NULL` and cannot log in until resolved (fix the email/name in Koala pre-migration, or in Tavern post-migration and re-run provisioning for just those 12). Nothing else depends on this — the rest of the migration proceeds independently.

**To find these yourself**, look at the provisioning script's failure output, or check post-migration:
```sql
SELECT "FirstName", "LastName", "Email", "StudentNumber" FROM "Members"
WHERE "AuthSystemUserId" IS NULL AND "Email" != 'bestuur@svsticky.nl';
```

### Phase 4 — Group memberships

```sql
INSERT INTO "GroupMemberships" (Id, MemberId, GroupId, MembershipYear, RoleAliasId)
SELECT
  gen_random_uuid(),
  mm.tavern_uuid,
  gm_map.tavern_uuid,
  gm.year,
  ra_map.tavern_role_alias_id
FROM koala.group_members gm
JOIN _member_map mm ON mm.koala_id = gm.member_id
JOIN _group_map gm_map ON gm_map.koala_id = gm.group_id
LEFT JOIN _role_alias_map ra_map ON ra_map.position_text = gm."position";
```

### Phase 5 — Study enrollments

```sql
INSERT INTO "StudyEnrollments" (Id, MemberId, StudyId, EnrollmentDate, CompletionDate, Status)
SELECT
  gen_random_uuid(),
  mm.tavern_uuid,
  sm.tavern_id,
  e.start_date::timestamptz,
  e.end_date::timestamptz,
  CASE e.status
    WHEN 0 THEN 0   -- Enrolled
    WHEN 1 THEN 2   -- stopped → DroppedOut
    WHEN 2 THEN 1   -- graduated → Completed
    WHEN 3 THEN 0   -- inactive → Enrolled (closest match; no Tavern Inactive status)
    ELSE 0
  END
FROM koala.educations e
JOIN _member_map mm ON mm.koala_id = e.member_id
JOIN _study_map sm ON sm.koala_id = e.study_id
WHERE e.member_id IS NOT NULL;
```

### Phase 6 — Activities

```sql
INSERT INTO "Activities" (
  Name, Price, DutchDescription, EnglishDescription,
  DateTimeStart, DateTimeEnd, Location, ParticipantLimit,
  IsEnrollable, ShowInKoala, UnenrollmentDeadline,
  PaymentDeadline, EnrollOpenDate, AllowedAudience,
  OrganizerId, PosterPath, PosterFileName
)
SELECT
  name,
  COALESCE(price, 0),
  COALESCE(description_nl, ''),
  COALESCE(description_en, ''),
  (start_date || ' ' || COALESCE(start_time::text, '00:00:00'))::timestamptz AT TIME ZONE 'Europe/Amsterdam',
  (end_date || ' ' || COALESCE(end_time::text, '00:00:00'))::timestamptz AT TIME ZONE 'Europe/Amsterdam',
  COALESCE(location, ''),
  participant_limit,
  COALESCE(is_enrollable, false),
  COALESCE(show_on_website, false),
  unenroll_date::timestamptz,
  payment_deadline::timestamptz,
  CASE WHEN open_date IS NOT NULL
    THEN (open_date || ' ' || COALESCE(open_time::text, '00:00:00'))::timestamptz AT TIME ZONE 'Europe/Amsterdam'
    ELSE NULL END,
  -- AllowedAudience bitmask
  CASE
    WHEN NOT (is_masters OR is_freshmans OR is_sophomores OR is_seniors) THEN 63  -- All
    ELSE
      (CASE WHEN is_freshmans  THEN 1  ELSE 0 END) |
      (CASE WHEN is_sophomores THEN 2  ELSE 0 END) |
      (CASE WHEN is_seniors    THEN 4  ELSE 0 END) |
      (CASE WHEN is_masters    THEN 8  ELSE 0 END)
  END,
  gm.tavern_uuid,  -- OrganizerId from _group_map
  NULL, NULL       -- Poster paths filled in Phase 10 after file migration
FROM koala.activities a
LEFT JOIN _group_map gm ON gm.koala_id = a.organized_by;
```
Store `_activity_map(koala_id, tavern_id)`.

### Phase 7 — Enrollments

```sql
INSERT INTO "Enrollments" (MemberId, ActivityId, Price, RegisteredOn, IsOnWaitingList)
SELECT
  mm.tavern_uuid,
  am.tavern_id,
  COALESCE(p.price, 0),
  p.created_at,
  p.reservist
FROM koala.participants p
JOIN _member_map mm ON mm.koala_id = p.member_id
JOIN _activity_map am ON am.koala_id = p.activity_id;
```

Enrollment notes are discarded (decision Q-C4B). No SpecificationQuestion migration needed.

### Phase 8 — Payments

All 12,414 rows migrated (pending + paid). `PaidAt` is set only for `status=2` rows.

```sql
-- Membership payments (payment_type=0)
INSERT INTO "MembershipPayments" (
  MemberId, Price, PaymentServiceId, PaymentIntentUrl, PaidAt, ManuallyMarkedAsPaid
)
SELECT
  mm.tavern_uuid,
  COALESCE(p.amount, 0),
  COALESCE(NULLIF(p.trxid,''), NULLIF(p.token,''), 'LEGACY-' || md5(mm.tavern_uuid::text || p.created_at::text)),
  COALESCE(NULLIF(p.redirect_uri,''), 'https://legacy-import'),
  CASE WHEN p.status = 2 THEN p.updated_at ELSE NULL END,
  true
FROM koala.payments p
JOIN _member_map mm ON mm.koala_id = p.member_id
WHERE p.payment_type = 0;

-- Enrollment payments (payment_type=1)
INSERT INTO "EnrollmentPayments" (
  MemberId, Price, PaymentServiceId, PaymentIntentUrl, PaidAt, ManuallyMarkedAsPaid
)
SELECT
  mm.tavern_uuid,
  COALESCE(p.amount, 0),
  COALESCE(NULLIF(p.trxid,''), NULLIF(p.token,''), 'LEGACY-' || md5(mm.tavern_uuid::text || p.created_at::text)),
  COALESCE(NULLIF(p.redirect_uri,''), 'https://legacy-import'),
  CASE WHEN p.status = 2 THEN p.updated_at ELSE NULL END,
  true
FROM koala.payments p
JOIN _member_map mm ON mm.koala_id = p.member_id
WHERE p.payment_type = 1;
```

> Note: Koala's `payments` table has no FK to `participants`, so `ActivityId` cannot be reliably set on `EnrollmentPayments`. It's left null — the payment history is preserved but not linked to a specific enrollment.

### Phase 9 — Announcements

Authorship is auto-matched from `koala.admins` to `koala.members` by name (`first_name + infix + last_name`). A synthetic member named "Bestuur" (email `bestuur@svsticky.nl`, `StudentNumber='BOARD-000'`) is created for posts where no match is found.

```sql
-- Step 9a: Build admin→member name map
CREATE TEMP TABLE _admin_member_map AS
SELECT
  a.id AS admin_id,
  COALESCE(mm.tavern_uuid, board.tavern_uuid) AS author_tavern_uuid
FROM koala.admins a
LEFT JOIN koala.members m
  ON m.first_name = a.first_name
 AND COALESCE(m.infix,'') = COALESCE(a.infix,'')
 AND m.last_name = a.last_name
LEFT JOIN _member_map mm ON mm.koala_id = m.id
CROSS JOIN (SELECT "Id" AS tavern_uuid FROM "Members" WHERE "Email" = 'bestuur@svsticky.nl') board;

-- Step 9b: Insert published announcements
INSERT INTO "Announcements" (Title, Content, CreatedAt, CreatedById, IsPinned)
SELECT
  title,
  content,
  COALESCE(published_at, created_at),
  am.author_tavern_uuid,
  COALESCE(pinned, false)
FROM koala.posts p
JOIN _admin_member_map am ON am.admin_id = p.author_id
WHERE p.status = 1;
```

> **Known issue found during the 2026-08-01 local dry-run: the synthetic Bestuur fallback silently didn't get created.** The script's `ON CONFLICT DO NOTHING` on the synthetic member insert (email `bestuur@svsticky.nl`) is a no-op if that email already exists — and it does: a **real** Koala member, Wilko Blaauw, is already registered with `bestuur@svsticky.nl` (presumably the shared board inbox). So instead of a neutral placeholder, Wilko's real account absorbed every unmatched-author post.
>
> Verified: **17 of 78 announcements** (~22%) are attributed to Wilko Blaauw, and **all 17** are actually unmatched-admin fallback cases (`koala.admins` rows whose name doesn't match any `koala.members` row) — Wilko did not genuinely author any of the 78 posts himself. The unmatched admins are: 4 `koala.admins` rows with blank first/last name (ids 22, 24, 16, 17 — likely deleted/deactivated admin accounts), plus `Bram de Haas`, `Tom Wassenberg`, and `Tobias de Bruijn (Admin)` — the last one fails the name match only because Koala literally stored `" (Admin)"` as part of the last name.
>
> **To find this yourself:**
> ```sql
> -- Who does bestuur@svsticky.nl actually belong to in Koala?
> SELECT first_name, infix, last_name FROM koala.members WHERE email = 'bestuur@svsticky.nl';
>
> -- Which admins failed to name-match to a real member (these become fallback posts)?
> SELECT a.id, a.first_name, a.infix, a.last_name FROM koala.admins a
> WHERE NOT EXISTS (
>   SELECT 1 FROM koala.members km
>   WHERE km.first_name = a.first_name AND COALESCE(km.infix,'') = COALESCE(a.infix,'') AND km.last_name = a.last_name
> );
>
> -- Post-migration: how many announcements ended up attributed to the fallback author?
> SELECT m."FirstName", m."LastName", COUNT(*) FROM "Announcements" a
> JOIN "Members" m ON m."Id" = a."CreatedById" GROUP BY 1, 2 ORDER BY 3 DESC;
> ```
>
> Not fixed yet — needs a decision: use a distinct email for the synthetic placeholder to avoid colliding with a real member (e.g. `bestuur-legacy@svsticky.nl`), fix the `Tobias de Bruijn (Admin)` string match specifically, or accept Wilko's account as the de facto historical author for these.

### Phase 10 — File migration

Files are stored at `/var/www/koala.svsticky.nl/storage/` on the Koala server. Rails Active Storage uses a two-level directory structure derived from the blob `key` (e.g. `ab/cd/abcdef...`).

```bash
# From the Koala server (or via SSH tunnel), rsync to S3:
aws s3 sync /var/www/koala.svsticky.nl/storage/ s3://tavern-bucket/legacy/ \
  --exclude "*.variant_record"

# Or via SSH if running from a separate machine:
rsync -avz user@koala.svsticky.nl:/var/www/koala.svsticky.nl/storage/ /tmp/koala-storage/
aws s3 sync /tmp/koala-storage/ s3://tavern-bucket/legacy/
```

After files are in S3, link activity posters in Tavern:

```sql
UPDATE "Activities" a
SET "PosterPath" = 'legacy/' || b.key,
    "PosterFileName" = b.filename
FROM koala.active_storage_attachments att
JOIN koala.active_storage_blobs b ON b.id = att.blob_id
JOIN _activity_map am ON am.koala_id = att.record_id::int
WHERE att.record_type = 'Activity'
  AND att.name = 'poster'
  AND a."Id" = am.tavern_id;
```

> Checkout product images (`record_type = 'CheckoutProduct'`) are not migrated since checkout data is archived in Koala.

---

## 6. Rollback strategy

The migration runs against a **fresh Tavern database** (empty, with all EF Core migrations applied). At no point does the script touch the Koala database or production data. 

Steps:
1. Take a snapshot of the Tavern DB before starting
2. Run migration phases in a single transaction where possible
3. If any phase fails, `ROLLBACK` and investigate before retrying
4. Only flip DNS / go-live after full dry-run pass verified

---

## 7. Post-migration verification queries

```sql
-- Member count matches source (adjust for deduplication decisions)
SELECT COUNT(*) FROM "Members";  -- expect ~4,057 minus duplicates

-- All members with Keycloak accounts linked
SELECT COUNT(*) FROM "Members" WHERE "AuthSystemUserId" IS NOT NULL;  -- expect ~2,944

-- Group membership integrity
SELECT COUNT(*) FROM "GroupMemberships" gm
WHERE NOT EXISTS (SELECT 1 FROM "Members" m WHERE m."Id" = gm."MemberId");  -- expect 0

-- No orphaned enrollments
SELECT COUNT(*) FROM "Enrollments" e
WHERE NOT EXISTS (SELECT 1 FROM "Activities" a WHERE a."Id" = e."ActivityId");  -- expect 0

-- Board members can be identified
SELECT COUNT(*) FROM "GroupMemberships" gm
JOIN "Groups" g ON g."Id" = gm."GroupId"
WHERE g."Name" = 'Bestuur';  -- should be > 0
```

---

## 8. What is NOT migrated

| Koala data | Reason |
|---|---|
| `checkout_*` tables | No Tavern equivalent; POS system out of scope |
| `impressions` | Page-view analytics, no equivalent |
| `oauth_access_*` / `oauth_applications` / `oauth_openid_requests` | Superseded by Keycloak |
| `tokens` | Superseded by Keycloak tokens |
| `ar_internal_metadata`, `schema_migrations` | Rails internals |
| `active_storage_variant_records` | Generated image variants; regenerate from source |
| `admins` records | Board members already in `members`; admin access via Keycloak roles in Tavern |
| `settings` | App-level config; set manually in Tavern via admin UI |
| `payments` with missing member FK | Skip — `JOIN _member_map` excludes orphaned payments |

---

## 9. Cutover procedure (hard cutover)

1. Announce maintenance window to members (recommend a low-traffic time, e.g. Friday night)
2. Put Koala in read-only / maintenance mode (disable signups, payments)
3. Run Phase 0 pre-migration tasks if not already done
4. Execute Phases 1–10 in order against the production Tavern DB
5. Run post-migration verification queries (Section 7)
6. Smoke test Tavern: log in as a test member, check enrollments, announcements, group memberships
7. Flip DNS from Koala to Tavern
8. Keep Koala server running (read-only archive) for 30 days, then decommission

---

## 10. Remaining work items

| Item | Owner |
|---|---|
| Build Keycloak bcrypt credential provider SPI JAR | ✅ Done — `keycloak-bcrypt-provider/`, deployed in `.devcontainer/keycloak-plugins/` |
| Rsync files from `/var/www/koala.svsticky.nl/storage/` to S3 | DevOps (local dry-run instead uploaded `storage.zip` to LocalStack, see Phase 10 notes above) |
| Create synthetic Bestuur member in Tavern before Phase 9 | ⚠️ Script has this, but silently no-ops — see the Phase 9 note above (a real member already owns `bestuur@svsticky.nl`) |
| Wire Mailchimp sync to Tavern `MailSubscriptions` | Future sprint |
| Re-apply Sprint 1 security fixes (see `SPRINT1-SECURITY-RAPPORT.md`) | ✅ Done 2026-08-01 — all 7 findings re-applied and verified |
