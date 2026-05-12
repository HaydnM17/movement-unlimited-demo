using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MU5PrototypeProject.Data.MUMigrations
{
    /// <inheritdoc />
    public partial class MakeSessionClientSessionsPerWeekNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "SessionsPerWeekRecommended",
                table: "SessionClients",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "SessionsPerWeekRecommended",
                table: "SessionClients",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);
        }
    }
}
