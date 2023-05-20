﻿using Database.Models;
using Database.Repos;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBot.Utils;
using Vostok.Logging.Abstractions;

namespace TelegramBot.Telegram.Commands.Cameras;

public class EnableCameraCommand : ITelegramCommand
{
	private readonly ITelegramUsersRepo _usersRepo;
	private readonly ICamerasRepo _camerasRepo;

	private static ILog Log => LogProvider.Get().ForContext<EnableCameraCommand>();

	public EnableCameraCommand(ITelegramUsersRepo usersRepo, ICamerasRepo camerasRepo)
	{
		_usersRepo = usersRepo;
		_camerasRepo = camerasRepo;
	}

	public bool CanProcessUpdate(Update update) =>
		update is {Type: UpdateType.CallbackQuery, CallbackQuery.Data: not null} &&
		update.CallbackQuery.Data.StartsWith("/enableCamera");

	public async Task ExecuteAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
	{
		Log.Info("Enable camera command received.");

		var sender = update.CallbackQuery!.From;

		var user = await _usersRepo.FindUserAsync(sender.Id);
		if (user is null)
		{
			await AnswerQuery(
				botClient,
				update.CallbackQuery,
				"Недостаточно прав для совершения операции.",
				null,
				cancellationToken
			);
			return;
		}

		var commandParts = update.CallbackQuery.Data!.Split(" ");
		if (commandParts.Length != 2 || !Guid.TryParse(commandParts[1], out var cameraId))
		{
			Log.Warn($"Invalid CallbackQuery data: {update.CallbackQuery.Data}");
			await AnswerQuery(
				botClient,
				update.CallbackQuery,
				"Произошла ошибка.",
				null,
				cancellationToken
			);
			return;
		}

		var cameraToEnable = await _camerasRepo.FindCameraAsync(cameraId);
		if (cameraToEnable is null)
		{
			await AnswerQuery(
				botClient,
				update.CallbackQuery,
				"Камера не найдена.",
				null,
				cancellationToken
			);
			return;
		}

		if (cameraToEnable.CameraState == CameraState.OutOfOrder)
		{
			await AnswerQuery(
				botClient,
				update.CallbackQuery,
				"Камера не исправна.",
				null,
				cancellationToken
			);
			return;
		}

		if (cameraToEnable.CameraState == CameraState.Disabled)
			await _camerasRepo.UpdateCameraStateAsync(cameraToEnable.Id, CameraState.Active);

		await AnswerQuery(
			botClient,
			update.CallbackQuery,
			"Камера включена.",
			null,
			cancellationToken
		);
	}

	private static async Task AnswerQuery(
		ITelegramBotClient botClient,
		CallbackQuery callbackQuery,
		string message,
		InlineKeyboardMarkup? replyMarkup,
		CancellationToken cancellationToken
	)
	{
		await botClient.AnswerCallbackQueryAsync(
			callbackQuery.Id,
			message,
			cancellationToken: cancellationToken
		);

		if (callbackQuery is {Message: null})
			return;

		await botClient.EditMessageTextAsync(
			callbackQuery.From.Id,
			callbackQuery.Message.MessageId,
			message,
			cancellationToken: cancellationToken
		);

		if (callbackQuery.Message.ReplyMarkup is null && replyMarkup is null)
			return;

		await botClient.EditMessageReplyMarkupAsync(
			callbackQuery.From.Id,
			callbackQuery.Message.MessageId,
			replyMarkup: replyMarkup,
			cancellationToken: cancellationToken
		);
	}
}