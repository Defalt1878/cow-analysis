using Database.Models;
using Database.Repos;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBot.Utils;
using Vostok.Logging.Abstractions;

namespace TelegramBot.Telegram.Commands.Cameras;

public class ManageCameraMenu : ITelegramCommand
{
	private readonly ITelegramUsersRepo _usersRepo;
	private readonly ICamerasRepo _camerasRepo;

	private static ILog Log => LogProvider.Get().ForContext<ManageCameraMenu>();

	public ManageCameraMenu(ITelegramUsersRepo usersRepo, ICamerasRepo camerasRepo)
	{
		_usersRepo = usersRepo;
		_camerasRepo = camerasRepo;
	}

	public bool CanProcessUpdate(Update update) =>
		update is {Type: UpdateType.CallbackQuery, CallbackQuery.Data: not null} &&
		update.CallbackQuery.Data.StartsWith("/manageCameraMenu");

	public async Task ExecuteAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
	{
		Log.Info("Manage user menu command received.");

		var callbackQuery = update.CallbackQuery;
		var sender = callbackQuery!.From;

		var user = await _usersRepo.FindUserAsync(sender.Id);
		if (user is null)
		{
			await AnswerQuery(
				botClient,
				callbackQuery,
				"Недостаточно прав для совершения операции.",
				null,
				cancellationToken
			);
			return;
		}

		var commandParts = callbackQuery.Data!.Split(" ");
		if (commandParts.Length != 2 || !Guid.TryParse(commandParts[1], out var cameraId))
		{
			Log.Warn($"Invalid CallbackQuery data: {callbackQuery.Data}");
			await AnswerQuery(
				botClient,
				callbackQuery,
				"Произошла ошибка.",
				null,
				cancellationToken
			);
			return;
		}

		var cameraToManage = await _camerasRepo.FindCameraAsync(cameraId);
		if (cameraToManage is null)
		{
			await AnswerQuery(
				botClient,
				callbackQuery,
				"Камера не найдена.",
				null,
				cancellationToken
			);
			return;
		}
		
		if (callbackQuery is {Message: null})
			return;
		
		await botClient.EditMessageTextAsync(
			callbackQuery.From.Id,
			callbackQuery.Message.MessageId,
			TelegramUtils.BuildShortCameraInfo(cameraToManage),
			cancellationToken: cancellationToken
		);

		await botClient.EditMessageReplyMarkupAsync(
			callbackQuery.From.Id,
			callbackQuery.Message.MessageId,
			replyMarkup: BuildCameraReplyMarkup(cameraToManage),
			cancellationToken: cancellationToken
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

	private static InlineKeyboardMarkup BuildCameraReplyMarkup(Camera camera)
	{
		var buttons = camera.CameraState switch
		{
			CameraState.Active => new[]
			{
				new InlineKeyboardButton("Отключить камеру")
				{
					CallbackData = $"/disableCamera {camera.Id}"
				},
				new InlineKeyboardButton("Удалить камеру")
				{
					CallbackData = $"/removeCamera {camera.Id}"
				}
			},
			CameraState.Disabled => new[]
			{
				new InlineKeyboardButton("Включить камеру")
				{
					CallbackData = $"/enableCamera {camera.Id}"
				},
				new InlineKeyboardButton("Удалить камеру")
				{
					CallbackData = $"/removeCamera {camera.Id}"
				}
			},
			CameraState.OutOfOrder => new[]
			{
				new InlineKeyboardButton("Удалить камеру")
				{
					CallbackData = $"/removeCamera {camera.Id}"
				}
			},
			_ => throw new ArgumentOutOfRangeException()
		};


		return new InlineKeyboardMarkup(buttons);
	}
}