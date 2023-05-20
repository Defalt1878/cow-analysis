using Database.Models;
using Database.Repos;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBot.Utils;
using Vostok.Logging.Abstractions;

namespace TelegramBot.Telegram.Commands.Users;

public class ManageUserMenuCommand : ITelegramCommand
{
	private readonly ITelegramUsersRepo _usersRepo;

	private static ILog Log => LogProvider.Get().ForContext<AddAdminCommand>();

	public ManageUserMenuCommand(ITelegramUsersRepo usersRepo)
	{
		_usersRepo = usersRepo;
	}

	public bool CanProcessUpdate(Update update) =>
		update is {Type: UpdateType.CallbackQuery, CallbackQuery.Data: not null} &&
		update.CallbackQuery.Data.StartsWith("/manageUserMenu");

	public async Task ExecuteAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
	{
		Log.Info("Manage user menu command received.");

		var callbackQuery = update.CallbackQuery;
		var sender = callbackQuery!.From;

		var user = await _usersRepo.FindUserAsync(sender.Id, TelegramUserStatus.Administrator);
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
		if (commandParts.Length != 2 || !long.TryParse(commandParts[1], out var userToApproveId))
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

		var userToManage = await _usersRepo.FindUserAsync(userToApproveId, TelegramUserStatus.Refused);
		if (userToManage is null)
		{
			await AnswerQuery(
				botClient,
				callbackQuery,
				"Пользователь не найден.",
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
			TelegramUtils.BuildShortUserInfo(userToManage),
			cancellationToken: cancellationToken
		);

		await botClient.EditMessageReplyMarkupAsync(
			callbackQuery.From.Id,
			callbackQuery.Message.MessageId,
			replyMarkup: BuildUserReplyMarkup(userToManage),
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

	private static InlineKeyboardMarkup BuildUserReplyMarkup(TelegramUser user)
	{
		var buttons = user.Status switch
		{
			TelegramUserStatus.Refused => new[]
			{
				new InlineKeyboardButton("Подтвердить пользователя")
				{
					CallbackData = $"/approveUser {user.Id}"
				}
			},
			TelegramUserStatus.PendingApprove => new[]
			{
				new InlineKeyboardButton("Подтвердить пользователя")
				{
					CallbackData = $"/approveUser {user.Id}"
				},
				new InlineKeyboardButton("Заблокировать пользователя")
				{
					CallbackData = $"/refuseUser {user.Id}"
				}
			},
			TelegramUserStatus.ApprovedUser => new[]
			{
				new InlineKeyboardButton("Сделать администратором")
				{
					CallbackData = $"/addAdmin {user.Id}"
				},
				new InlineKeyboardButton("Заблокировать пользователя")
				{
					CallbackData = $"/refuseUser {user.Id}"
				}
			},
			TelegramUserStatus.Administrator => new[]
			{
				new InlineKeyboardButton("Забрать права администратора")
				{
					CallbackData = $"/removeAdmin {user.Id}"
				},
				new InlineKeyboardButton("Заблокировать пользователя")
				{
					CallbackData = $"/refuseUser {user.Id}"
				}
			},
			_ => throw new ArgumentOutOfRangeException()
		};


		return new InlineKeyboardMarkup(buttons);
	}
}