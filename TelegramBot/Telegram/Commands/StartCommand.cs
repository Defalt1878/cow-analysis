using Database.Models;
using Database.Repos;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Vostok.Logging.Abstractions;

namespace TelegramBot.Telegram.Commands;

public class StartCommand : ITelegramCommand
{
	private readonly ITelegramUsersRepo _usersRepo;
	private readonly INotificationsRepo _notificationsRepo;

	private static ILog Log => LogProvider.Get().ForContext<StartCommand>();

	public StartCommand(
		ITelegramUsersRepo usersRepo,
		INotificationsRepo notificationsRepo
	)
	{
		_usersRepo = usersRepo;
		_notificationsRepo = notificationsRepo;
	}

	public bool CanProcessUpdate(Update update) =>
		update is {Type: UpdateType.Message, Message.Text: not null} &&
		update.Message.Text.TrimEnd().ToLower() is "/start";

	public async Task ExecuteAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
	{
		Log.Info("Start command received.");
		var sender = update.Message!.From!;
		var user = await _usersRepo.FindUserAsync(sender.Id, TelegramUserStatus.Refused);

		string message;
		if (user is not null)
		{
			message = user.Status switch
			{
				TelegramUserStatus.Refused => "Ваша заявка была отклонена администратором.",
				TelegramUserStatus.PendingApprove => "Ваша заявка принята, ожидайте ответа от администратора.",
				TelegramUserStatus.ApprovedUser or TelegramUserStatus.Administrator =>
					"Вы уже являетесь пользователем нашего бота.",
				_ => throw new ArgumentOutOfRangeException(nameof(user.Status))
			};
		}
		else
		{
			user = await _usersRepo.AddUserAsync(sender.Id, sender.Username!);
			Log.Info("New user request created.");
			await _notificationsRepo.AddNotificationAsync(new NewUserNotification
			{
				UserId = user.Id
			});
			message = "Заявка отправлена. Ожидайте решения администратора.";
		}

		await botClient.SendTextMessageAsync(
			update.Message.Chat.Id,
			message,
			cancellationToken: cancellationToken
		);
	}
}