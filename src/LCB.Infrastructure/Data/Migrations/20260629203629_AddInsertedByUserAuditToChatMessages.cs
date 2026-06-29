using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LCB.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInsertedByUserAuditToChatMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InsertedByUser",
                table: "ChatMessages",
                type: "TEXT",
                maxLength: 255,
                nullable: false,
                defaultValue: "system:legacy");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_InsertedByUser",
                table: "ChatMessages",
                column: "InsertedByUser");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ChatMessages_InsertedByUser",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "InsertedByUser",
                table: "ChatMessages");
        }
    }
}
