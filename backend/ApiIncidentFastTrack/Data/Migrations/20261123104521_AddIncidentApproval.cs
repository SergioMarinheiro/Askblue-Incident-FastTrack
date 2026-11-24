using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiIncidentFastTrack.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIncidentApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "Incidents",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ApprovedByUserId",
                table: "Incidents",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_ApprovedByUserId",
                table: "Incidents",
                column: "ApprovedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Incidents_Users_ApprovedByUserId",
                table: "Incidents",
                column: "ApprovedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Incidents_Users_ApprovedByUserId",
                table: "Incidents");

            migrationBuilder.DropIndex(
                name: "IX_Incidents_ApprovedByUserId",
                table: "Incidents");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "Incidents");

            migrationBuilder.DropColumn(
                name: "ApprovedByUserId",
                table: "Incidents");
        }
    }
}
