using Database.Models;
using Database.Repos;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Vostok.Logging.Abstractions;

namespace TelegramBot.Telegram.Commands.Users;

public class RemoveAdminCommand : ITelegramCommand
{
	private readonly ITelegramUsersRepo _usersRepo;
	private readonly INotificationsRepo _notificationsRepo;

	private static ILog Log => LogProvider.Get().ForContext<RemoveAdminCommand>();

	public RemoveAdminCommand(
		ITelegramUsersRepo usersRepo,
		INotificationsRepo notificationsRepo
	)
	{
		_usersRepo = usersRepo;
		_notificationsRepo = notificationsRepo;
	}

	public bool CanProcessUpdate(Update update) =>
		update is {Type: UpdateType.CallbackQuery, CallbackQuery.Data: not null} &&
		update.CallbackQuery.Data.StartsWith("/removeAdmin");

	public async Task ExecuteAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
	{
		Log.Info("Remove admin command received.");

		var sender = update.CallbackQuery!.From;

		string message;

		var user = await _usersRepo.FindUserAsync(sender.Id, TelegramUserStatus.Administrator);
		if (user is null)
		{
			message = "Недостаточно прав для совершения операции.";
		}
		else
		{
			var commandParts = update.CallbackQuery.Data!.Split(" ");
			if (commandParts.Length != 2 || !long.TryParse(commandParts[1], out var userToApproveId))
			{
				Log.Warn($"Invalid CallbackQuery data: {update.CallbackQuery.Data}");
				message = "Произошла ошибка.";
			}
			else
			{
				var userToRefuseAdmin = await _usersRepo.FindUserAsync(userToApproveId, TelegramUserStatus.Refused);
				if (userToRefuseAdmin is null)
				{
					message = "Пользователь не найден.";
				}
				else
				{
					var previousStatus = userToRefuseAdmin.Status;
					if (previousStatus == TelegramUserStatus.Administrator)
					{
						userToRefuseAdmin = await _usersRepo.RemoveAdminPermissions(userToApproveId);
						Log.Info($"User @{userToRefuseAdmin.Username} admin permissions removed by @{user.Username}.");
						await _notificationsRepo.AddNotificationAsync(new UserStatusChangeNotification
						{
							UserId = userToRefuseAdmin.Id,
							PreviousStatus = previousStatus,
							NewStatus = TelegramUserStatus.ApprovedUser
						});
					}

					message = $"Пользователь {userToRefuseAdmin.Username} больше не администратор.";
				}
			}
		}

		await botClient.AnswerCallbackQueryAsync(
			update.CallbackQuery.Id,
			message,
			cancellationToken: cancellationToken
		);

		if (update.CallbackQuery is {Message: not null})
		{
			await botClient.EditMessageTextAsync(
				update.CallbackQuery.From.Id,
				update.CallbackQuery.Message.MessageId,
				message,
				cancellationToken: cancellationToken
			);

			if (update.CallbackQuery.Message.ReplyMarkup is not null)
				await botClient.EditMessageReplyMarkupAsync(
					update.CallbackQuery.From.Id,
					update.CallbackQuery.Message.MessageId,
					replyMarkup: null,
					cancellationToken: cancellationToken
				);
		}
	}
}