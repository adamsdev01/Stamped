using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Stamped.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddJobPostingExternalFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                table: "JobPostings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ExternalUrl",
                table: "JobPostings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "JobPostings");

            migrationBuilder.DropColumn(
                name: "ExternalUrl",
                table: "JobPostings");
        }
    }
}
