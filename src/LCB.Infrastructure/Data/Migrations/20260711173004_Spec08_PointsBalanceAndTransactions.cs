using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LCB.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Spec08_PointsBalanceAndTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PointsBalances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Provider = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ChannelId = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Points = table.Column<long>(type: "INTEGER", nullable: false, defaultValue: 0L),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PointsBalances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PointsTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Provider = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ChannelId = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Points = table.Column<long>(type: "INTEGER", nullable: false),
                    Situation = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    TransactionDateTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PointsTransactions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PointsBalances_Provider_ChannelId_UserId",
                table: "PointsBalances",
                columns: new[] { "Provider", "ChannelId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_PointsBalances_Provider_ChannelId_UserId_IsActive",
                table: "PointsBalances",
                columns: new[] { "Provider", "ChannelId", "UserId", "IsActive" },
                unique: true,
                filter: "\"IsActive\" = 1");

            migrationBuilder.CreateIndex(
                name: "IX_PointsTransactions_Provider_ChannelId_UserId",
                table: "PointsTransactions",
                columns: new[] { "Provider", "ChannelId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_PointsTransactions_TransactionDateTime",
                table: "PointsTransactions",
                column: "TransactionDateTime");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PointsBalances");

            migrationBuilder.DropTable(
                name: "PointsTransactions");
        }
    }
}
