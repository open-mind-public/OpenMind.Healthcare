using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DietApi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBeerDays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BeerDays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DietPlanId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BeerDays", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BeerDays_DietPlanId_Date",
                table: "BeerDays",
                columns: new[] { "DietPlanId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BeerDays_UserId_Date",
                table: "BeerDays",
                columns: new[] { "UserId", "Date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BeerDays");
        }
    }
}
