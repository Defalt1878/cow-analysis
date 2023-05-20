﻿using Database.Models;
using Database.Repos;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Vostok.Logging.Abstractions;

namespace TelegramBot.Telegram.Commands.Users;

public class AddAdminCommand : ITelegramCommand
{
	private readonly ITelegramUsersRepo _usersRepo;
	private readonly INotificationsRepo _notificationsRepo;

	private static ILog Log => LogProvider.Get().ForContext<AddAdminCommand>();

	public AddAdminCommand(
		ITelegramUsersRepo usersRepo,
		INotificationsRepo notificationsRepo
	)
	{
		_usersRepo = usersRepo;
		_notificationsRepo = notificationsRepo;
	}

	public bool CanProcessUpdate(Update update) =>
		update is {Type: UpdateType.CallbackQuery, CallbackQuery.Data: not null} &&
		update.CallbackQuery.Data.StartsWith("/addAdmin");

	public async Task ExecuteAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
	{
		Log.Info("Add admin command received.");

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
				var userToAdmin = await _usersRepo.FindUserAsync(userToApproveId, TelegramUserStatus.Refused);
				if (userToAdmin is null)
				{
					message = "Пользователь не найден.";
				}
				else
				{
					var previousStatus = userToAdmin.Status;
					if (previousStatus < TelegramUserStatus.Administrator)
					{
						userToAdmin = await _usersRepo.GrantAdminPermissions(userToApproveId);
						Log.Info($"User @{userToAdmin.Username} admin permissions granted by @{user.Username}.");
						await _notificationsRepo.AddNotificationAsync(new UserStatusChangeNotification
						{
							UserId = userToAdmin.Id,
							PreviousStatus = previousStatus,
							NewStatus = TelegramUserStatus.Administrator
						});
					}

					message = $"Пользователю {userToAdmin.Username} были выданы права администратора.";
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