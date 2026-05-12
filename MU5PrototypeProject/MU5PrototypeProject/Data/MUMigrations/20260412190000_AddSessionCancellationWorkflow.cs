using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MU5PrototypeProject.Data.MUMigrations
{
    /// <inheritdoc />
    [DbContext(typeof(MUContext))]
    [Migration("20260412190000_AddSessionCancellationWorkflow")]
    public partial class AddSessionCancellationWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CanceledBy",
                table: "Sessions",
                type: "TEXT",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CanceledOn",
                table: "Sessions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "Sessions",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCanceled",
                table: "Sessions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CanceledBy",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "CanceledOn",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "IsCanceled",
                table: "Sessions");
        }
    }
}
