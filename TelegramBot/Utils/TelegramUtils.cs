using Database.Models;

namespace TelegramBot.Utils;

public static class TelegramUtils
{
	public static string BuildShortUserInfo(TelegramUser user) =>
		$"@{user.Username} ({BuildUserStatusInfo(user.Status)})";

	public static string BuildUserStatusInfo(TelegramUserStatus status) =>
		status switch
		{
			TelegramUserStatus.Refused => "Заблокированный пользователь",
			TelegramUserStatus.PendingApprove => "Ожидает подтверждения",
			TelegramUserStatus.ApprovedUser => "Подтвержденный пользователь",
			TelegramUserStatus.Administrator => "Администратор",
			_ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
		};

	public static string BuildShortCameraInfo(Camera camera) =>
		$"{camera.Address} ({BuildCameraStateInfo(camera.CameraState)})";

	public static string BuildCameraStateInfo(CameraState state) =>
		state switch
		{
			CameraState.Active => "Активна",
			CameraState.Disabled => "Отключена",
			CameraState.OutOfOrder => "Не исправна",
			_ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
		};
}