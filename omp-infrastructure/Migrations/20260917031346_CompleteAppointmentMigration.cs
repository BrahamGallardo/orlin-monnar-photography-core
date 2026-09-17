using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace omp_infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CompleteAppointmentMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedDate",
                table: "Appointments",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletedDate",
                table: "Appointments");
        }
    }
}
