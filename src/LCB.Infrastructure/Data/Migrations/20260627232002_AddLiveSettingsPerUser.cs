using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LCB.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLiveSettingsPerUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LiveSettings",
                columns: table => new
                {
                    SettingsId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TikTokUsername = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    YouTubeUsername = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    TwitchUsername = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    ReloadTimeInSec = table.Column<long>(type: "INTEGER", nullable: false, defaultValue: 5L),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedByUser = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LiveSettings", x => x.SettingsId);
                    table.ForeignKey(
                        name: "FK_LiveSettings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LiveSettings_UpdatedAtUtc",
                table: "LiveSettings",
                column: "UpdatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_LiveSettings_UserId",
                table: "LiveSettings",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LiveSettings");
        }
    }
}
