using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LCB.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Spec29_IntegrationTypeCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PointsIntegrationTypeCatalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    StreamerUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Provider = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    IntegrationType = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Delta = table.Column<long>(type: "INTEGER", nullable: false, defaultValue: 0L),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PointsIntegrationTypeCatalog", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PointsIntegrationTypeCatalog_StreamerUserId_Provider_IntegrationType",
                table: "PointsIntegrationTypeCatalog",
                columns: new[] { "StreamerUserId", "Provider", "IntegrationType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PointsIntegrationTypeCatalog");
        }
    }
}
