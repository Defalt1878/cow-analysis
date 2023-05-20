using Database.Repos;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBot.Utils;
using Vostok.Logging.Abstractions;

namespace TelegramBot.Telegram.Commands.Cameras;

public class AddCameraCommand : ITelegramCommand
{
	private readonly ITelegramUsersRepo _usersRepo;
	private readonly ICamerasRepo _camerasRepo;

	private static ILog Log => LogProvider.Get().ForContext<AddCameraCommand>();

	public AddCameraCommand(ITelegramUsersRepo usersRepo, ICamerasRepo camerasRepo)
	{
		_usersRepo = usersRepo;
		_camerasRepo = camerasRepo;
	}

	public bool CanProcessUpdate(Update update) =>
		update is {Type: UpdateType.Message, Message.Text: not null} &&
		update.Message.Text.StartsWith("/addcamera");

	public async Task ExecuteAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
	{
		Log.Info("Add camera command received.");

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

		var commandParts = update.Message.Text!.Split(" ");
		if (commandParts.Length != 2)
		{
			await botClient.SendTextMessageAsync(
				sender.Id,
				"Некорректная команда.\nИспользуйте формат:\n/addcamera {address}",
				cancellationToken: cancellationToken
			);
			return;
		}

		var camera = await _camerasRepo.AddCameraAsync(commandParts[1]);

		await botClient.SendTextMessageAsync(
			sender.Id,
			$"Камера добавлена: {TelegramUtils.BuildShortCameraInfo(camera)}.",
			cancellationToken: cancellationToken
		);
	}
}