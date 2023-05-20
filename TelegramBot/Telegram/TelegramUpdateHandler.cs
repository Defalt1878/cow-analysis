using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBot.Telegram.Commands;
using Vostok.Logging.Abstractions;

namespace TelegramBot.Telegram;

public class TelegramUpdateHandler : IUpdateHandler
{
	private const string UnableToProcessMessage = "Не могу обработать сообщение.";
	private readonly IList<ITelegramCommand> _commands;
	private static ILog Log => LogProvider.Get().ForContext<TelegramUpdateHandler>();

	public TelegramUpdateHandler(IEnumerable<ITelegramCommand> commands)
	{
		_commands = commands.ToList();
	}

	public async Task HandleUpdateAsync(
		ITelegramBotClient botClient,
		Update update,
		CancellationToken cancellationToken
	)
	{
		try
		{
			await TryHandleUpdateAsync(botClient, update, cancellationToken);
		}
		catch (Exception e)
		{
			Log.Error(e, "Error while processing update.");
		}
	}

	public Task HandlePollingErrorAsync(
		ITelegramBotClient botClient,
		Exception exception,
		CancellationToken cancellationToken
	)
	{
		Log.Error(exception, $"Telegram bot error.");
		return Task.CompletedTask;
	}

	private async Task TryHandleUpdateAsync(
		ITelegramBotClient botClient,
		Update update,
		CancellationToken cancellationToken
	)
	{
		var chatId = GetChatId(update);
		Log.Info($"Received update with type: {update.Type}" + (chatId is null ? "" : $" ChatId: {chatId}"));

		var command = _commands.FirstOrDefault(cmd => cmd.CanProcessUpdate(update));
		if (command is null)
		{
			Log.Warn("Unprocessable update. No handler was found.");

			if (chatId is not null)
				await botClient.SendTextMessageAsync(
					chatId.Value,
					UnableToProcessMessage,
					cancellationToken: cancellationToken
				);

			return;
		}

		await command.ExecuteAsync(botClient, update, cancellationToken);
	}

	private static long? GetChatId(Update update)
	{
		return update.Type switch
		{
			UpdateType.Message => update.Message?.From?.Id,
			UpdateType.CallbackQuery => update.CallbackQuery?.From.Id,
			_ => null
		};
	}
}