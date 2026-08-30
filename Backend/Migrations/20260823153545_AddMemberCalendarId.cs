using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddMemberCalendarId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Written as raw SQL rather than migrationBuilder.AddColumn so that re-applying this migration
            // after a rollback keeps the calendar identifiers the Down method left behind. Members subscribe
            // to a URL containing this value from their own calendar application, so handing out a new one
            // silently breaks every existing subscription with no way for us to notify the subscriber.
            migrationBuilder.Sql(@"ALTER TABLE ""Members"" ADD COLUMN IF NOT EXISTS ""CalendarId"" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';");

            // Give every member that does not have one yet its own random identifier, so the unique index
            // below can be created. gen_random_uuid() is built into PostgreSQL 13 and later and draws from a
            // cryptographically secure source, which is required here because possession of this value is the
            // only thing guarding a member's calendar feed. Restricting the update to the all-zero default
            // leaves already-published identifiers untouched.
            migrationBuilder.Sql(@"UPDATE ""Members"" SET ""CalendarId"" = gen_random_uuid() WHERE ""CalendarId"" = '00000000-0000-0000-0000-000000000000';");

            // Drop the all-zero column default so that a future insert which forgets to supply a calendar
            // identifier fails loudly on the unique index instead of silently publishing a guessable feed.
            migrationBuilder.Sql(@"ALTER TABLE ""Members"" ALTER COLUMN ""CalendarId"" DROP DEFAULT;");

            migrationBuilder.Sql(@"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Members_CalendarId"" ON ""Members"" (""CalendarId"");");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Deliberately non-destructive: only the index is dropped and the column is left in place with its
            // data intact. Dropping the column would permanently destroy every member's calendar identifier,
            // and because that identifier is the secret embedded in the feed URL, rolling forward again would
            // hand out fresh ones and silently break every calendar subscription in existence. The previous
            // application version simply ignores the surplus column, so leaving it costs nothing.
            migrationBuilder.DropIndex(
                name: "IX_Members_CalendarId",
                table: "Members");
        }
    }
}
