using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiIncidentFastTrack.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveResolutionTypeDescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "ResolutionTypes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "ResolutionTypes",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }
    }
}
