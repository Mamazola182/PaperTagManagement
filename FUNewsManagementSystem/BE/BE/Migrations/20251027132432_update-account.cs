using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAPI.Migrations
{
    /// <inheritdoc />
    public partial class updateaccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<short>(
                name: "SystemAccountAccountId",
                table: "AuditLogs",
                type: "smallint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_SystemAccountAccountId",
                table: "AuditLogs",
                column: "SystemAccountAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_SystemAccount_SystemAccountAccountId",
                table: "AuditLogs",
                column: "SystemAccountAccountId",
                principalTable: "SystemAccount",
                principalColumn: "AccountID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogs_SystemAccount_SystemAccountAccountId",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_SystemAccountAccountId",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "SystemAccountAccountId",
                table: "AuditLogs");
        }
    }
}
