using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuitSmokingApi.Infrastructure.Data.Migrations
{
    /// <summary>
    /// The Unspecified trigger was dropped from RelapseTrigger. Triggers are persisted by name,
    /// so rows still holding "Unspecified" would fail to materialise once the member is gone.
    /// They move to "Other", which carries the same meaning - the user did not say what caused it.
    /// No schema change: the column is already TEXT.
    /// </summary>
    public partial class RetireUnspecifiedTrigger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "SmokedDays"
                SET "Trigger" = 'Other'
                WHERE "Trigger" = 'Unspecified';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Not reversible: once merged into "Other" there is no way to tell which rows
            // were originally "Unspecified".
        }
    }
}
