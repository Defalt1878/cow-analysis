using Database.Models;
using Database.Repos;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Vostok.Logging.Abstractions;

namespace TelegramBot.Telegram.Commands.Users;

public class ApproveUserCommand : ITelegramCommand
{
	private readonly ITelegramUsersRepo _usersRepo;
	private readonly INotificationsRepo _notificationsRepo;

	private static ILog Log => LogProvider.Get().ForContext<ApproveUserCommand>();

	public ApproveUserCommand(
		ITelegramUsersRepo usersRepo,
		INotificationsRepo notificationsRepo
	)
	{
		_usersRepo = usersRepo;
		_notificationsRepo = notificationsRepo;
	}

	public bool CanProcessUpdate(Update update) =>
		update is {Type: UpdateType.CallbackQuery, CallbackQuery.Data: not null} &&
		update.CallbackQuery.Data.StartsWith("/approveUser");

	public async Task ExecuteAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
	{
		Log.Info("Approve user command received.");

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
				var userToApprove = await _usersRepo.FindUserAsync(userToApproveId, TelegramUserStatus.Refused);
				if (userToApprove is null)
				{
					message = "Пользователь не найден.";
				}
				else
				{
					var previousStatus = userToApprove.Status;
					if (previousStatus < TelegramUserStatus.ApprovedUser)
					{
						userToApprove = await _usersRepo.ApproveUserAsync(userToApproveId);
						Log.Info($"User @{userToApprove.Username} approved by @{user.Username}.");
						await _notificationsRepo.AddNotificationAsync(new UserStatusChangeNotification
						{
							UserId = userToApprove.Id,
							PreviousStatus = previousStatus,
							NewStatus = TelegramUserStatus.ApprovedUser
						});
					}

					message = $"Пользователь {userToApprove.Username} был успешно подтвержден.";
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