using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DietApi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFoodLibraryAndLogging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FoodLibraryItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SearchName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FoodLibraryItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LoggedDays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DietPlanId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    TargetCalories = table.Column<int>(type: "INTEGER", nullable: false),
                    TargetProteinG = table.Column<decimal>(type: "TEXT", precision: 6, scale: 1, nullable: true),
                    TargetCarbsG = table.Column<decimal>(type: "TEXT", precision: 6, scale: 1, nullable: true),
                    TargetFatG = table.Column<decimal>(type: "TEXT", precision: 6, scale: 1, nullable: true),
                    TotalCalories = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalProteinG = table.Column<decimal>(type: "TEXT", precision: 7, scale: 1, nullable: false),
                    TotalCarbsG = table.Column<decimal>(type: "TEXT", precision: 7, scale: 1, nullable: false),
                    TotalFatG = table.Column<decimal>(type: "TEXT", precision: 7, scale: 1, nullable: false),
                    Version = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoggedDays", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServingSizes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FoodLibraryItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Label = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    GramWeight = table.Column<decimal>(type: "TEXT", precision: 7, scale: 2, nullable: false),
                    Calories = table.Column<int>(type: "INTEGER", nullable: false),
                    ProteinG = table.Column<decimal>(type: "TEXT", precision: 6, scale: 1, nullable: false),
                    CarbsG = table.Column<decimal>(type: "TEXT", precision: 6, scale: 1, nullable: false),
                    FatG = table.Column<decimal>(type: "TEXT", precision: 6, scale: 1, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServingSizes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServingSizes_FoodLibraryItems_FoodLibraryItemId",
                        column: x => x.FoodLibraryItemId,
                        principalTable: "FoodLibraryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FoodEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    LoggedDayId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FoodLibraryItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ServingSizeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FoodName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ServingLabel = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", precision: 6, scale: 2, nullable: false),
                    MealType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Calories = table.Column<int>(type: "INTEGER", nullable: false),
                    ProteinG = table.Column<decimal>(type: "TEXT", precision: 6, scale: 1, nullable: false),
                    CarbsG = table.Column<decimal>(type: "TEXT", precision: 6, scale: 1, nullable: false),
                    FatG = table.Column<decimal>(type: "TEXT", precision: 6, scale: 1, nullable: false),
                    LoggedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FoodEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FoodEntries_LoggedDays_LoggedDayId",
                        column: x => x.LoggedDayId,
                        principalTable: "LoggedDays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FoodEntries_LoggedDayId",
                table: "FoodEntries",
                column: "LoggedDayId");

            migrationBuilder.CreateIndex(
                name: "IX_FoodLibraryItems_SearchName",
                table: "FoodLibraryItems",
                column: "SearchName");

            migrationBuilder.CreateIndex(
                name: "IX_LoggedDays_DietPlanId_Date",
                table: "LoggedDays",
                columns: new[] { "DietPlanId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoggedDays_UserId_Date",
                table: "LoggedDays",
                columns: new[] { "UserId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_ServingSizes_FoodLibraryItemId",
                table: "ServingSizes",
                column: "FoodLibraryItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FoodEntries");

            migrationBuilder.DropTable(
                name: "ServingSizes");

            migrationBuilder.DropTable(
                name: "LoggedDays");

            migrationBuilder.DropTable(
                name: "FoodLibraryItems");
        }
    }
}
