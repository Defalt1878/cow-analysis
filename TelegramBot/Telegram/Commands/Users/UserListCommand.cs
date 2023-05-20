using Database.Models;
using Database.Repos;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBot.Utils;
using Vostok.Logging.Abstractions;

namespace TelegramBot.Telegram.Commands.Users;

public class UserListCommand : ITelegramCommand
{
	private readonly ITelegramUsersRepo _usersRepo;

	private static ILog Log => LogProvider.Get().ForContext<UserListCommand>();

	public UserListCommand(ITelegramUsersRepo usersRepo)
	{
		_usersRepo = usersRepo;
	}

	public bool CanProcessUpdate(Update update) =>
		update is {Type: UpdateType.Message, Message.Text: not null} &&
		update.Message.Text.TrimEnd().ToLower() is "/users";

	public async Task ExecuteAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
	{
		Log.Info("User list command received.");
		var sender = update.Message!.From!;

		var user = await _usersRepo.FindUserAsync(sender.Id, TelegramUserStatus.Administrator);
		if (user is null)
		{
			await botClient.SendTextMessageAsync(
				sender.Id,
				"Недостаточно прав для совершения операции.",
				cancellationToken: cancellationToken
			);
			return;
		}


		var users = (await _usersRepo.GetUsersAsync(TelegramUserStatus.Refused))
			.Where(u => u.Id != user.Id)
			.ToList();
		if (users.Count == 0)
			await botClient.SendTextMessageAsync(
				sender.Id,
				"Нет ни одного пользователя.",
				cancellationToken: cancellationToken,
				parseMode: ParseMode.Html
			);
		else
			await botClient.SendTextMessageAsync(
				sender.Id,
				"Выберите пользователя из списка ниже:",
				cancellationToken: cancellationToken,
				parseMode: ParseMode.Html,
				replyMarkup: BuildUsersListReplyMarkup(users)
			);
	}

	private static InlineKeyboardMarkup BuildUsersListReplyMarkup(IEnumerable<TelegramUser> users)
	{
		var buttons = users
			.Select(user => new[]
				{
					new InlineKeyboardButton(TelegramUtils.BuildShortUserInfo(user))
					{
						CallbackData = $"/manageUserMenu {user.Id}"
					}
				}
			);
		return new InlineKeyboardMarkup(buttons);
	}
}