using Database.Models;
using Database.Repos;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBot.Utils;
using Vostok.Logging.Abstractions;

namespace TelegramBot.Telegram.Commands.Cameras;

public class CamerasListCommand : ITelegramCommand
{
	private readonly ITelegramUsersRepo _usersRepo;
	private readonly ICamerasRepo _camerasRepo;

	private static ILog Log => LogProvider.Get().ForContext<CamerasListCommand>();

	public CamerasListCommand(ITelegramUsersRepo usersRepo, ICamerasRepo camerasRepo)
	{
		_usersRepo = usersRepo;
		_camerasRepo = camerasRepo;
	}

	public bool CanProcessUpdate(Update update) =>
		update is {Type: UpdateType.Message, Message.Text: not null} &&
		update.Message.Text.TrimEnd().ToLower() is "/cameras";

	public async Task ExecuteAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
	{
		Log.Info("Cameras list command received.");
		var sender = update.Message!.From!;

		var user = await _usersRepo.FindUserAsync(sender.Id);
		if (user is null)
		{
			await botClient.SendTextMessageAsync(
				sender.Id,
				"Недостаточно прав для совершения операции.",
				cancellationToken: cancellationToken
			);
			return;
		}


		var cameras = await _camerasRepo.GetCamerasAsync();
		if (cameras.Count == 0)
			await botClient.SendTextMessageAsync(
				sender.Id,
				"Нет ни одной камеры.",
				cancellationToken: cancellationToken,
				parseMode: ParseMode.Html
			);
		else
			await botClient.SendTextMessageAsync(
				sender.Id,
				"Выберите камеру из списка ниже:",
				cancellationToken: cancellationToken,
				parseMode: ParseMode.Html,
				replyMarkup: BuildCamerasListReplyMarkup(cameras)
			);
	}

	private static InlineKeyboardMarkup BuildCamerasListReplyMarkup(IEnumerable<Camera> cameras)
	{
		var buttons = cameras
			.Select(camera => new[]
				{
					new InlineKeyboardButton(TelegramUtils.BuildShortCameraInfo(camera))
					{
						CallbackData = $"/manageCameraMenu {camera.Id}"
					}
				}
			);
		return new InlineKeyboardMarkup(buttons);
	}
}