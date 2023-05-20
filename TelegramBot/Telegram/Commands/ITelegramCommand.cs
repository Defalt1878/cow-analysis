using Telegram.Bot;
using Telegram.Bot.Types;

namespace TelegramBot.Telegram.Commands;

public interface ITelegramCommand
{
	bool CanProcessUpdate(Update update);

	Task ExecuteAsync(
		ITelegramBotClient botClient,
		Update update,
		CancellationToken cancellationToken
	);
}