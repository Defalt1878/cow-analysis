using Database.Models;
using Database.Repos;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Vostok.Logging.Abstractions;

namespace TelegramBot.Telegram.Notifications;

public interface INotificationsSender
{
	public Task SendAllAsync(CancellationToken cancellationToken = default);
}

public class NotificationsSender : INotificationsSender
{
	private readonly ITelegramBot _telegramBot;
	private readonly INotificationsRepo _notificationsRepo;

	private static readonly ILog Log = LogProvider.Get().ForContext<NotificationsSender>();

	public NotificationsSender(
		ITelegramBot telegramBot,
		INotificationsRepo notificationsRepo
	)
	{
		_telegramBot = telegramBot;
		_notificationsRepo = notificationsRepo;
	}

	public async Task SendAllAsync(CancellationToken cancellationToken = default)
	{
		Log.Info("Requesting deliveries...");
		var deliveries = await _notificationsRepo.GetNotificationDeliveriesAsync();
		var recipientsCount = deliveries.Sum(delivery => delivery.RecipientsIds.Count);
		Log.Info($"Deliveries count: {deliveries.Count}. Recipients count: {recipientsCount}");

		var sent = 0;
		foreach (var delivery in deliveries)
		{
			cancellationToken.ThrowIfCancellationRequested();
			foreach (var recipientId in delivery.RecipientsIds)
			{
				try
				{
					await _telegramBot.SendMessageAsync(
						recipientId,
						delivery.HtmlContent,
						replyMarkup: BuildMarkup(delivery.ButtonsMarkup),
						parseMode: ParseMode.Html,
						cancellationToken: cancellationToken
					);

					sent++;
				}
				catch (Exception e)
				{
					sent++;
					Log.Error(
						e,
						$"Error while sending notification {delivery.NotificationId} to {delivery.RecipientsIds}."
					);
				}
			}

			await _notificationsRepo.MarkNotificationsAsSentAsync(delivery.NotificationId);
		}

		Log.Info($"Sent {deliveries.Count} deliveries to {sent} users. Errors: {recipientsCount - sent}");
	}

	private static InlineKeyboardMarkup? BuildMarkup(NotificationButton[][]? buttonsMarkup)
	{
		if (buttonsMarkup is null)
			return null;

		var telegramButtons = buttonsMarkup
			.Select(line => line
				.Select(button => new InlineKeyboardButton(button.Text)
				{
					Url = button.Url,
					CallbackData = button.CallbackData,
				})
			);

		return new InlineKeyboardMarkup(telegramButtons);
	}
}