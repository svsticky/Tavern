# Legacy → Tavern Migration: Questions & Answers

> All questions have been answered. This file is now a decision log for reference during migration execution.

---

## ✅ Answered — decisions and source

| # | Question | Decision |
|---|---|---|
| Q-B1 | Tag integer values in `koala.tags.name` | From `constipated-koala/app/models/tag.rb`: `pardon:1`→Gratie, `merit:2`→LidVanVerdienste, `honorary:3`→EreLid (no prod data), `donator:4`→Begunstiger, `suspended:5`→`Members.Suspended=true` |
| Q-B2 | `koala.educations.status = 3` meaning | From `constipated-koala/app/models/education.rb`: `active:0, stopped:1, graduated:2, inactive:3`. Map 3→`Enrolled` (no Tavern Inactive status) |
| Q-B3 | Active Storage file path on Koala server | `/var/www/koala.svsticky.nl/storage/` on the remote Koala server |
| Q-B4 | Password migration strategy | Import bcrypt hashes via Keycloak SPI JAR; transparently re-hash to PBKDF2 on first successful login |
| Q-B5 | Announcement authorship (admins vs members) | Auto-match `admins` to `members` by name; groups (Bestuur + kandidaat-Bestuur) replace the `admins` concept for authorization. Use synthetic "Bestuur" member for any unmatched posts |
| Q-B6 | Cutover strategy | **A — Hard cutover**: Koala goes offline, migration runs, Tavern goes live |
| Q-C1 | Duplicate email addresses | **Verified from dump: 0 duplicates** — all 4,057 emails are unique. No action needed. |
| Q-C2 | 9 members with null `student_id` | Assign synthetic `UNKNOWN-{koala_id}`; flag in post-migration report for manual correction |
| Q-C3 | ~1,113 members without Koala user accounts | **A — Yes**: create Keycloak accounts with no password; they set a password on first login |
| Q-C4 | 10,674 enrollment notes field | **B — Discard**: historical free-text notes not migrated |
| Q-C5 | Historical payment records | **C — Migrate all** (pending + paid, 12,414 rows) with stub values for missing `PaymentServiceId` / `PaymentIntentUrl` |
| Q-C6 | Checkout system data | **A — No**: leave in archived Koala DB; Tavern has no checkout module |
| Q-C7 | admin→member mapping for post authorship | Auto-match by `first_name + infix + last_name`; unmatched → synthetic Bestuur member |
| Q-C8 | Default language for members without user account | **NL** (Dutch) |
| Q-C9 | `members.consent` field | **A — Discard**: Tavern has no equivalent; consent tracking restarts in Tavern or a separate GDPR tool |
| Q-C10 | `activities.organized_by` (group_id) | Migrate to `Activities.OrganizerId` — field already exists in Tavern (nullable `uint?`) |
| Q-C11 | Default MailSubscriptions for migrated members | `None = 0`; Mailchimp sync will be added separately by the team |
| Q-C12 | Target go-live date | No hard deadline yet |

---

## ✅ Answered from pg_dump — no input needed

| Question | Answer |
|---|---|
| Q1.1 Legacy system name | Koala — Ruby on Rails app |
| Q1.2 Auth mechanism | Devise gem, bcrypt-encrypted passwords in `users` table |
| Q1.3 Credential format | bcrypt (`$2a$10$...`) — importable into Keycloak via SPI |
| Q1.4 User vs. member separation | Yes — separate `users` (auth) and `members` (profile) tables |
| Q2.1 Member PK type | Integer auto-increment (must map to UUID in Tavern) |
| Q2.2 Student number | `student_id` field exists; 9 members have null; must be unique |
| Q2.3 Field coverage | See migration plan — most fields have direct equivalents |
| Q3.1 Board representation | Group with `category=1`; only group is "Bestuur" (id=1) |
| Q3.2 Groups structure | `category`: 1=board, 2=committee, 3=moot (dispuut), 4=other |
| Q3.3 Roles | `group_members.position` is free-text (e.g. "Voorzitter") → create RoleAlias records |
| Q4.1 Activities & enrollments | Yes — `activities` and `participants` tables |
| Q4.2 SpecificationQuestions | No — only a single `notes` free-text on `participants` (discarded per Q-C4) |
| Q4.3 File storage | Active Storage with `service_name='local'` on Koala server |
| Q5.1 Payment provider | Mollie (modern `tr_xxx`) + legacy manual (`s1k...` tokens) |
| Q5.2 Payment types | `payment_type 0=membership, 1=activity` |
| Q6.1 Study enrollments | Yes — `educations` table (7,388 rows) |
| Q6.2 Dates per enrollment | Yes — `start_date` and `end_date` |
| Q8.1 Member count | 4,057 members; 2,944 with login accounts; ~1,113 without |
| Q8.2 Duplicate emails | **None** — all 4,057 emails are unique (earlier analysis was incorrect) |
| Q8.3 Archived/deleted members | 7 soft-deleted `users` (`deleted_at` not null) → `Suspended=true`, no Keycloak account |
| Q9.1 Scope of data | See migration plan Section 2 |
