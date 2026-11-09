using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaceDay.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddStatusAndImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProfilePictureBlobName",
                table: "UserProfile",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BannerBlobName",
                table: "Event",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Enrolment",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Pending");

            migrationBuilder.AddCheckConstraint(
                name: "CHK_Enrolment_Status",
                table: "Enrolment",
                sql: "[Status] IN ('Pending', 'Confirmed')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CHK_Enrolment_Status",
                table: "Enrolment");

            migrationBuilder.DropColumn(
                name: "ProfilePictureBlobName",
                table: "UserProfile");

            migrationBuilder.DropColumn(
                name: "BannerBlobName",
                table: "Event");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Enrolment");
        }
    }
}
