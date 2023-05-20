using Core;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Vostok.Logging.Abstractions;

namespace TelegramBot.Telegram;

public interface ITelegramBot
{
	void InitializeBot(CancellationToken cancellationToken = default);
	void StartReceiving(CancellationToken cancellationToken = default);

	Task SendMessageAsync(
		long chatId,
		string message,
		ParseMode? parseMode = null,
		InlineKeyboardMarkup? replyMarkup = null,
		CancellationToken cancellationToken = default
	);
}

public class TelegramBot : ITelegramBot
{
	private readonly IUpdateHandler _updateHandler;
	private readonly ITelegramBotClient _bot;
	private static ILog Log => LogProvider.Get().ForContext(typeof(TelegramBot));

	public TelegramBot(
		IUpdateHandler updateHandler,
		IOptions<CowConfiguration> options
	)
	{
		_updateHandler = updateHandler;

		var botToken = options.Value.Telegram.BotToken;
		try
		{
			_bot = new TelegramBotClient(botToken);
		}
		catch (Exception e)
		{
			Log.Error(e, $"Can\'t initialize telegram bot with token \"{botToken.MaskAsSecret()}\"");
			throw;
		}

		Log.Info($"Initialized telegram bot with token \"{botToken.MaskAsSecret()}\"");
	}

	public void InitializeBot(CancellationToken cancellationToken = default)
	{
		 
	}

	public void StartReceiving(CancellationToken cancellationToken = default)
	{
		var receiverOptions = new ReceiverOptions
		{
			AllowedUpdates = new[] {UpdateType.Message, UpdateType.CallbackQuery}
		};

		_bot.StartReceiving(
			_updateHandler,
			receiverOptions,
			cancellationToken: cancellationToken
		);
	}

	public async Task SendMessageAsync(
		long chatId,
		string message,
		ParseMode? parseMode = null,
		InlineKeyboardMarkup? replyMarkup = null,
		CancellationToken cancellationToken = default
	)
	{
		await _bot
			.SendTextMessageAsync(
				chatId,
				message,
				parseMode: parseMode,
				replyMarkup: replyMarkup,
				cancellationToken: cancellationToken
			);
	}
}