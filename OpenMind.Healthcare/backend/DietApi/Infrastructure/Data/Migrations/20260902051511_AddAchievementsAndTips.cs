using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DietApi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAchievementsAndTips : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DietAchievements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Icon = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Criterion = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Threshold = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DietAchievements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EatingTips",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Icon = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EatingTips", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UnlockedAchievements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DietPlanId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DietAchievementId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EarnedOn = table.Column<DateOnly>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnlockedAchievements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UnlockedAchievements_DietPlans_DietPlanId",
                        column: x => x.DietPlanId,
                        principalTable: "DietPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UnlockedAchievements_DietPlanId_DietAchievementId",
                table: "UnlockedAchievements",
                columns: new[] { "DietPlanId", "DietAchievementId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DietAchievements");

            migrationBuilder.DropTable(
                name: "EatingTips");

            migrationBuilder.DropTable(
                name: "UnlockedAchievements");
        }
    }
}
