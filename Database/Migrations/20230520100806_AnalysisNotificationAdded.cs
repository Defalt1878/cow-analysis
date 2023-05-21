using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.Migrations
{
    /// <inheritdoc />
    public partial class AnalysisNotificationAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CameraId",
                table: "Notifications",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NewCalfCount",
                table: "Notifications",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NewCowCount",
                table: "Notifications",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PreviousCalfCount",
                table: "Notifications",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PreviousCowCount",
                table: "Notifications",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_CameraId",
                table: "Notifications",
                column: "CameraId");

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Cameras_CameraId",
                table: "Notifications",
                column: "CameraId",
                principalTable: "Cameras",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Cameras_CameraId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_CameraId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "CameraId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "NewCalfCount",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "NewCowCount",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "PreviousCalfCount",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "PreviousCowCount",
                table: "Notifications");
        }
    }
}
