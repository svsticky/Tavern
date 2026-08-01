-- Koala → Tavern data migration
-- Run against the 'postgres' database after 01_load_koala.sh and 02_run_ef_migrations.sh
-- All Koala source data is in the 'koala' schema.
-- All Tavern target tables are in the 'public' schema.

BEGIN;

-- ============================================================
-- Temp mapping tables
-- ============================================================
CREATE TEMP TABLE _study_map   (koala_id int PRIMARY KEY, tavern_id bigint NOT NULL);
CREATE TEMP TABLE _group_map   (koala_id int PRIMARY KEY, tavern_id bigint NOT NULL);
CREATE TEMP TABLE _role_alias_map (position_text text PRIMARY KEY, tavern_role_alias_id bigint NOT NULL);
CREATE TEMP TABLE _member_map  (koala_id int PRIMARY KEY, tavern_uuid uuid NOT NULL);
CREATE TEMP TABLE _activity_map(koala_id int PRIMARY KEY, tavern_id bigint NOT NULL);

-- ============================================================
-- Phase 1a: Studies
-- ============================================================
WITH inserted AS (
    INSERT INTO "Studies" ("Title", "NominalDurationYears", "Type")
    SELECT
        code,
        CASE WHEN masters THEN 2 ELSE 3 END,
        CASE WHEN masters THEN 1 ELSE 0 END
    FROM koala.studies
    RETURNING "Id", "Title"
)
INSERT INTO _study_map (koala_id, tavern_id)
SELECT s.id, i."Id"
FROM koala.studies s
JOIN inserted i ON i."Title" = s.code;

-- ============================================================
-- Phase 1b: Groups
-- ============================================================
WITH inserted AS (
    INSERT INTO "Groups" ("Name", "Type", "DefaultGLAccount", "DefaultCostCenter", "Active")
    SELECT
        name,
        CASE category
            WHEN 3 THEN 2   -- Dispute
            WHEN 4 THEN 1   -- WorkingGroup
            ELSE 0          -- Committee (category 1 = Bestuur, category 2 = normal)
        END,
        NULLIF(TRIM(ledgernr), ''),
        NULLIF(TRIM(cost_location), ''),
        true
    FROM koala.groups
    RETURNING "Id", "Name"
)
INSERT INTO _group_map (koala_id, tavern_id)
SELECT g.id, i."Id"
FROM koala.groups g
JOIN inserted i ON i."Name" = g.name;

-- ============================================================
-- Phase 1c: Roles + RoleAliases (from unique group_members.position values)
-- ============================================================
WITH roles_inserted AS (
    INSERT INTO "Roles" ("Name")
    SELECT DISTINCT "position"
    FROM koala.group_members
    WHERE "position" IS NOT NULL AND TRIM("position") <> ''
    RETURNING "Id", "Name"
),
aliases_inserted AS (
    INSERT INTO "RoleAliases" ("RoleId", "Name")
    SELECT "Id", "Name" FROM roles_inserted
    RETURNING "Id", "Name"
)
INSERT INTO _role_alias_map (position_text, tavern_role_alias_id)
SELECT "Name", "Id" FROM aliases_inserted;

-- ============================================================
-- Phase 2a: Members (core insert)
-- Members with no user account, or with a deleted user → AuthSystemUserId stays NULL
-- Soft-deleted users (deleted_at NOT NULL) → Suspended = true
-- ============================================================
WITH inserted AS (
    INSERT INTO "Members" (
        "Id", "StudentNumber", "FirstName", "LastName", "Email",
        "PhoneNumber", "ParentPhoneNumber", "Street", "HouseNumber",
        "PostalCode", "City", "DateOfBirth", "RegisteredOn", "Notes",
        "PreferredLanguage", "MailSubscriptions",
        "Gratie", "LidVanVerdienste", "EreLid", "Begunstiger", "Suspended",
        "AuthSystemUserId"
    )
    SELECT
        gen_random_uuid(),
        COALESCE(NULLIF(TRIM(m.student_id), ''), 'UNKNOWN-' || m.id::text),
        m.first_name,
        COALESCE(NULLIF(TRIM(m.infix), '') || ' ', '') || m.last_name,
        m.email,
        COALESCE(m.phone_number, ''),
        m.emergency_phone_number,
        LEFT(COALESCE(m.address, ''), 40),
        LEFT(COALESCE(m.house_number, ''), 10),
        LEFT(COALESCE(m.postal_code, ''), 10),
        LEFT(COALESCE(m.city, ''), 40),
        COALESCE(m.birth_date, '1900-01-01')::timestamptz,
        COALESCE(m.join_date, m.created_at::date)::timestamptz,
        m.comments,
        CASE WHEN u.language = 1 THEN 1 ELSE 0 END,
        0,   -- MailSubscriptions.None
        false, false, false, false,
        CASE WHEN u.deleted_at IS NOT NULL THEN true ELSE false END,
        NULL -- filled in by Phase 3 (Keycloak provisioning)
    FROM (
        SELECT DISTINCT ON (m.email) m.*
        FROM koala.members m
        ORDER BY m.email, m.id DESC
    ) m
    LEFT JOIN koala.users u ON u.credentials_type = 'Member' AND u.credentials_id = m.id
    RETURNING "Id", "Email"
)
INSERT INTO _member_map (koala_id, tavern_uuid)
SELECT m.id, i."Id"
FROM (
    SELECT DISTINCT ON (email) id, email FROM koala.members ORDER BY email, id DESC
) m
JOIN inserted i ON i."Email" = m.email;

-- ============================================================
-- Phase 2b: Tag flags
-- ============================================================
UPDATE "Members" mb SET "Gratie" = true
FROM _member_map mm JOIN koala.tags t ON t.member_id = mm.koala_id AND t.name = 1
WHERE mb."Id" = mm.tavern_uuid;

UPDATE "Members" mb SET "LidVanVerdienste" = true
FROM _member_map mm JOIN koala.tags t ON t.member_id = mm.koala_id AND t.name = 2
WHERE mb."Id" = mm.tavern_uuid;

UPDATE "Members" mb SET "Begunstiger" = true
FROM _member_map mm JOIN koala.tags t ON t.member_id = mm.koala_id AND t.name = 4
WHERE mb."Id" = mm.tavern_uuid;

UPDATE "Members" mb SET "Suspended" = true
FROM _member_map mm JOIN koala.tags t ON t.member_id = mm.koala_id AND t.name = 5
WHERE mb."Id" = mm.tavern_uuid;

-- ============================================================
-- Phase 4: Group memberships
-- ============================================================
INSERT INTO "GroupMemberships" ("MemberId", "GroupId", "MembershipYear", "RoleAliasId")
SELECT
    mm.tavern_uuid,
    gm.tavern_id,
    gm_src.year + 1,   -- Koala uses start-year (2024 = 2024-25); Tavern uses end-year (2025 = 2024-25)
    ra.tavern_role_alias_id
FROM koala.group_members gm_src
JOIN _member_map mm ON mm.koala_id = gm_src.member_id
JOIN _group_map gm ON gm.koala_id = gm_src.group_id
LEFT JOIN _role_alias_map ra ON ra.position_text = gm_src."position";

-- ============================================================
-- Phase 5: Study enrollments
-- ============================================================
INSERT INTO "StudyEnrollments" ("MemberId", "StudyId", "EnrollmentDate", "CompletionDate", "Status")
SELECT
    mm.tavern_uuid,
    sm.tavern_id,
    e.start_date::timestamptz,
    e.end_date::timestamptz,
    CASE e.status
        WHEN 0 THEN 0   -- active → Enrolled
        WHEN 1 THEN 2   -- stopped → DroppedOut
        WHEN 2 THEN 1   -- graduated → Completed
        WHEN 3 THEN 0   -- inactive → Enrolled (no better fit)
        ELSE 0
    END
FROM koala.educations e
JOIN _member_map mm ON mm.koala_id = e.member_id
JOIN _study_map sm ON sm.koala_id = e.study_id
WHERE e.member_id IS NOT NULL;

-- ============================================================
-- Phase 6: Activities
-- Stage via temp table to get reliable koala_id → tavern_id mapping
-- ============================================================
CREATE TEMP TABLE _activities_staging AS
SELECT
    a.id AS koala_id,
    a.name,
    COALESCE(a.price, 0)::numeric(18,2) AS price,
    LEFT(COALESCE(a.description_nl, ''), 2000) AS description_nl,
    LEFT(COALESCE(a.description_en, ''), 2000) AS description_en,
    (a.start_date::text || ' ' || COALESCE(a.start_time::text, '00:00:00'))::timestamptz AT TIME ZONE 'Europe/Amsterdam' AS dt_start,
    (COALESCE(a.end_date, a.start_date)::text || ' ' || COALESCE(a.end_time::text, '23:59:00'))::timestamptz AT TIME ZONE 'Europe/Amsterdam' AS dt_end,
    LEFT(COALESCE(a.location, ''), 200) AS location,
    a.participant_limit,
    COALESCE(a.is_enrollable, false) AS is_enrollable,
    COALESCE(a.show_on_website, false) AS show_on_website,
    CASE WHEN a.unenroll_date IS NOT NULL
        THEN a.unenroll_date::timestamptz AT TIME ZONE 'Europe/Amsterdam'
        ELSE NULL END AS unenroll_deadline,
    COALESCE(
        a.payment_deadline::timestamptz AT TIME ZONE 'Europe/Amsterdam',
        (COALESCE(a.end_date, a.start_date)::text || ' 23:59:59')::timestamptz AT TIME ZONE 'Europe/Amsterdam'
    ) AS payment_deadline,
    CASE WHEN a.open_date IS NOT NULL
        THEN (a.open_date::text || ' ' || COALESCE(a.open_time::text, '00:00:00'))::timestamptz AT TIME ZONE 'Europe/Amsterdam'
        ELSE NULL END AS enroll_open_date,
    CASE
        WHEN NOT COALESCE(a.is_masters, false)
          AND NOT COALESCE(a.is_freshmans, false)
          AND NOT COALESCE(a.is_sophomores, false)
          AND NOT COALESCE(a.is_seniors, false) THEN 63
        ELSE
          (CASE WHEN COALESCE(a.is_freshmans, false)  THEN 1 ELSE 0 END) |
          (CASE WHEN COALESCE(a.is_sophomores, false) THEN 2 ELSE 0 END) |
          (CASE WHEN COALESCE(a.is_seniors, false)    THEN 4 ELSE 0 END) |
          (CASE WHEN COALESCE(a.is_masters, false)    THEN 8 ELSE 0 END)
    END AS allowed_audience,
    gm.tavern_id AS organizer_id
FROM koala.activities a
LEFT JOIN _group_map gm ON gm.koala_id = a.organized_by;

-- Insert activities and capture the new ID per staging row
DO $$
DECLARE r record;
        new_id bigint;
BEGIN
    FOR r IN SELECT * FROM _activities_staging ORDER BY koala_id LOOP
        INSERT INTO "Activities" (
            "Name", "Price", "DutchDescription", "EnglishDescription",
            "DateTimeStart", "DateTimeEnd", "Location", "ParticipantLimit",
            "IsEnrollable", "ShowOnWebsite", "ShowInKoala",
            "AreParticipantsVisible", "IsAdultOnly", "IsWeeklyDrinks", "IsOpenForPayment",
            "UnenrollmentDeadline", "PaymentDeadline", "EnrollOpenDate",
            "AllowedAudience", "OrganizerId"
        ) VALUES (
            r.name, r.price, r.description_nl, r.description_en,
            r.dt_start, r.dt_end, r.location, r.participant_limit,
            r.is_enrollable, r.show_on_website, false,
            false, false, false, false,
            r.unenroll_deadline, r.payment_deadline, r.enroll_open_date,
            r.allowed_audience, r.organizer_id
        ) RETURNING "Id" INTO new_id;
        INSERT INTO _activity_map (koala_id, tavern_id) VALUES (r.koala_id, new_id);
    END LOOP;
END $$;

-- ============================================================
-- Phase 7: Enrollments
-- ============================================================
INSERT INTO "Enrollments" ("MemberId", "ActivityId", "Price", "RegisteredOn", "IsOnWaitingList")
SELECT
    mm.tavern_uuid,
    am.tavern_id,
    COALESCE(p.price, 0),
    p.created_at,
    COALESCE(p.reservist, false)
FROM koala.participants p
JOIN _member_map mm ON mm.koala_id = p.member_id
JOIN _activity_map am ON am.koala_id = p.activity_id;

-- ============================================================
-- Phase 8: Payments
-- ============================================================
-- MembershipPayments: unique per member — import most recent paid, else most recent pending
INSERT INTO "MembershipPayments" (
    "Price", "PaymentServiceId", "PaymentIntentUrl", "PaidAt", "ManuallyMarkedAsPaid", "MemberId"
)
SELECT DISTINCT ON (mm.tavern_uuid)
    COALESCE(p.amount, 0),
    COALESCE(NULLIF(p.trxid, ''), NULLIF(p.token, ''), 'LEGACY-' || md5(mm.tavern_uuid::text || p.created_at::text)),
    COALESCE(NULLIF(p.redirect_uri, ''), 'https://legacy-import'),
    CASE WHEN p.status = 2 THEN p.updated_at ELSE NULL END,
    true,
    mm.tavern_uuid
FROM koala.payments p
JOIN _member_map mm ON mm.koala_id = p.member_id
WHERE p.payment_type = 0
ORDER BY mm.tavern_uuid, p.status DESC, p.created_at DESC;

-- EnrollmentPayments: unique per member — import most recent
INSERT INTO "EnrollmentPayments" (
    "Price", "PaymentServiceId", "PaymentIntentUrl", "PaidAt", "ManuallyMarkedAsPaid", "MemberId"
)
SELECT DISTINCT ON (mm.tavern_uuid)
    COALESCE(p.amount, 0),
    COALESCE(NULLIF(p.trxid, ''), NULLIF(p.token, ''), 'LEGACY-' || md5(mm.tavern_uuid::text || p.created_at::text)),
    COALESCE(NULLIF(p.redirect_uri, ''), 'https://legacy-import'),
    CASE WHEN p.status = 2 THEN p.updated_at ELSE NULL END,
    true,
    mm.tavern_uuid
FROM koala.payments p
JOIN _member_map mm ON mm.koala_id = p.member_id
WHERE p.payment_type = 1
ORDER BY mm.tavern_uuid, p.status DESC, p.created_at DESC;

-- ============================================================
-- Phase 9: Announcements
-- Requires a synthetic Bestuur member for unmatched admin authors.
-- ============================================================

-- Helper: convert Koala/Trix HTML to Markdown for announcement content
CREATE OR REPLACE FUNCTION _koala_html_to_md(html TEXT) RETURNS TEXT LANGUAGE plpgsql AS $$
DECLARE r TEXT := html;
BEGIN
    IF r IS NULL THEN RETURN r; END IF;
    -- Links: <a href="url" ...>text</a> → [text](url)
    r := regexp_replace(r, '<a[^>]*href="([^"]*)"[^>]*>([^<]*)</a>', '[\2](\1)', 'gi');
    -- Bold
    r := regexp_replace(r, '<(strong|b)>([^<]*)</(strong|b)>', '**\2**', 'gi');
    -- Italic
    r := regexp_replace(r, '<(em|i)>([^<]*)</(em|i)>', '*\2*', 'gi');
    -- Headings
    r := regexp_replace(r, '<h1[^>]*>([^<]*)</h1>', E'# \\1\n\n', 'gi');
    r := regexp_replace(r, '<h2[^>]*>([^<]*)</h2>', E'## \\1\n\n', 'gi');
    r := regexp_replace(r, '<h3[^>]*>([^<]*)</h3>', E'### \\1\n\n', 'gi');
    -- List items
    r := regexp_replace(r, '<li[^>]*>([^<]*)</li>', E'- \\1\n', 'gi');
    r := regexp_replace(r, '<[uo]l[^>]*>', '', 'gi');
    r := regexp_replace(r, '</[uo]l>', E'\n', 'gi');
    -- Line breaks and paragraphs
    r := regexp_replace(r, '<br\s*/?>', E'\n', 'gi');
    r := regexp_replace(r, '</p>', E'\n\n', 'gi');
    r := regexp_replace(r, '<p[^>]*>', '', 'gi');
    -- Strip all remaining tags
    r := regexp_replace(r, '<[^>]+>', '', 'g');
    -- HTML entities
    r := replace(r, '&amp;', '&');
    r := replace(r, '&lt;', '<');
    r := replace(r, '&gt;', '>');
    r := replace(r, '&nbsp;', ' ');
    r := replace(r, '&quot;', '"');
    r := replace(r, '&#39;', '''');
    r := replace(r, '&apos;', '''');
    r := replace(r, '&euro;', '€');
    -- Collapse excessive blank lines
    r := regexp_replace(trim(r), E'\n{3,}', E'\n\n', 'g');
    RETURN r;
END;
$$;

-- 9a: Synthetic Bestuur member (author fallback for posts)
INSERT INTO "Members" (
    "Id", "StudentNumber", "FirstName", "LastName", "Email",
    "PhoneNumber", "Street", "HouseNumber", "PostalCode", "City",
    "DateOfBirth", "RegisteredOn",
    "PreferredLanguage", "MailSubscriptions",
    "Gratie", "LidVanVerdienste", "EreLid", "Begunstiger", "Suspended",
    "AuthSystemUserId"
)
VALUES (
    gen_random_uuid(), 'BOARD-000', 'Bestuur', 'S.V. Sticky', 'bestuur@svsticky.nl',
    '', '', '', '', '',
    '1990-01-01'::timestamptz, NOW(),
    0, 0,
    false, false, false, false, false,
    NULL
)
ON CONFLICT DO NOTHING;

-- 9b: Admin → member name mapping (best effort)
CREATE TEMP TABLE _admin_member_map AS
SELECT
    a.id AS admin_id,
    COALESCE(
        (SELECT mm.tavern_uuid FROM koala.members km
         JOIN _member_map mm ON mm.koala_id = km.id
         WHERE km.first_name = a.first_name
           AND COALESCE(km.infix, '') = COALESCE(a.infix, '')
           AND km.last_name = a.last_name
         LIMIT 1),
        (SELECT "Id" FROM "Members" WHERE "Email" = 'bestuur@svsticky.nl')
    ) AS author_tavern_uuid
FROM koala.admins a;

-- 9c: Published posts only (status=1) — convert HTML content to Markdown
-- Koala announcements are Dutch-only (no separate English title/content); Tavern
-- requires both languages, so the Dutch text is duplicated into the English fields
-- as a placeholder until someone provides a real translation.
INSERT INTO "Announcements" ("TitleDutch", "TitleEnglish", "ContentDutch", "ContentEnglish", "CreatedAt", "CreatedById")
SELECT
    LEFT(p.title, 100),
    LEFT(p.title, 100),
    LEFT(_koala_html_to_md(p.content), 10000),
    LEFT(_koala_html_to_md(p.content), 10000),
    COALESCE(p.published_at, p.created_at),
    am.author_tavern_uuid
FROM koala.posts p
JOIN _admin_member_map am ON am.admin_id = p.author_id
WHERE p.status = 1;

DROP FUNCTION _koala_html_to_md;

-- ============================================================
-- Verification counts
-- ============================================================
SELECT 'Members'          AS table_name, COUNT(*) AS rows FROM "Members"
UNION ALL SELECT 'Groups',          COUNT(*) FROM "Groups"
UNION ALL SELECT 'Studies',         COUNT(*) FROM "Studies"
UNION ALL SELECT 'GroupMemberships',COUNT(*) FROM "GroupMemberships"
UNION ALL SELECT 'StudyEnrollments',COUNT(*) FROM "StudyEnrollments"
UNION ALL SELECT 'Activities',      COUNT(*) FROM "Activities"
UNION ALL SELECT 'Enrollments',     COUNT(*) FROM "Enrollments"
UNION ALL SELECT 'MembershipPayments',   COUNT(*) FROM "MembershipPayments"
UNION ALL SELECT 'EnrollmentPayments',   COUNT(*) FROM "EnrollmentPayments"
UNION ALL SELECT 'Announcements',   COUNT(*) FROM "Announcements"
ORDER BY table_name;

COMMIT;
