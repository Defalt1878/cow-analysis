using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.Migrations
{
    /// <inheritdoc />
    public partial class UserStatusChangeNotificaitonUpdated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Users_UserRequestApprovedNotification_UserId",
                schema: "public",
                table: "Notifications");

            migrationBuilder.RenameTable(
                name: "Users",
                schema: "public",
                newName: "Users");

            migrationBuilder.RenameTable(
                name: "Notifications",
                schema: "public",
                newName: "Notifications");

            migrationBuilder.RenameTable(
                name: "CamerasAnalyses",
                schema: "public",
                newName: "CamerasAnalyses");

            migrationBuilder.RenameTable(
                name: "Cameras",
                schema: "public",
                newName: "Cameras");

            migrationBuilder.RenameColumn(
                name: "UserRequestApprovedNotification_UserId",
                table: "Notifications",
                newName: "UserStatusChangeNotification_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Notifications_UserRequestApprovedNotification_UserId",
                table: "Notifications",
                newName: "IX_Notifications_UserStatusChangeNotification_UserId");

            migrationBuilder.AddColumn<int>(
                name: "PreviousStatus",
                table: "Notifications",
                type: "integer",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Users_UserStatusChangeNotification_UserId",
                table: "Notifications",
                column: "UserStatusChangeNotification_UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Users_UserStatusChangeNotification_UserId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "PreviousStatus",
                table: "Notifications");

            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.RenameTable(
                name: "Users",
                newName: "Users",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "Notifications",
                newName: "Notifications",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "CamerasAnalyses",
                newName: "CamerasAnalyses",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "Cameras",
                newName: "Cameras",
                newSchema: "public");

            migrationBuilder.RenameColumn(
                name: "UserStatusChangeNotification_UserId",
                schema: "public",
                table: "Notifications",
                newName: "UserRequestApprovedNotification_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Notifications_UserStatusChangeNotification_UserId",
                schema: "public",
                table: "Notifications",
                newName: "IX_Notifications_UserRequestApprovedNotification_UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Users_UserRequestApprovedNotification_UserId",
                schema: "public",
                table: "Notifications",
                column: "UserRequestApprovedNotification_UserId",
                principalSchema: "public",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
