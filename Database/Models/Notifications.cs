using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Database.Repos;
using Microsoft.Extensions.DependencyInjection;

namespace Database.Models;

public abstract class Notification
{
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public Guid Id { get; set; }

	[Required]
	public DateTime CreateTime { get; set; }

	[Required]
	public bool IsSent { get; set; }

	public abstract string GetMessageHtmlContentForDelivery();
	public abstract NotificationButton[][]? GetButtonsMarkupForDelivery();
	public abstract Task<IList<long>> GetRecipientsIdsAsync(IServiceProvider serviceProvider);
}

public class NewUserNotification : Notification
{
	[Required]
	public long UserId { get; set; }

	public virtual TelegramUser User { get; set; } = null!;

	public override string GetMessageHtmlContentForDelivery()
	{
		return $"Новая заявка от пользователя @{User.Username} на пользование сервисом.";
	}

	public override NotificationButton[][] GetButtonsMarkupForDelivery()
	{
		return new[]
		{
			new[]
			{
				new NotificationButton {Text = "Принять", CallbackData = $"/approveUser {UserId}"},
				new NotificationButton {Text = "Отклонить", CallbackData = $"/refuseUser {UserId}"},
			}
		};
	}

	public override async Task<IList<long>> GetRecipientsIdsAsync(IServiceProvider serviceProvider)
	{
		var usersRepo = serviceProvider.GetRequiredService<ITelegramUsersRepo>();
		return await usersRepo.GetUsersIdsAsync(TelegramUserStatus.Administrator);
	}
}

public class UserStatusChangeNotification : Notification
{
	[Required]
	public long UserId { get; set; }

	public virtual TelegramUser User { get; set; } = null!;

	[Required]
	public TelegramUserStatus PreviousStatus { get; set; }

	[Required]
	public TelegramUserStatus NewStatus { get; set; }


	public override string GetMessageHtmlContentForDelivery()
	{
		if (NewStatus is TelegramUserStatus.Refused && PreviousStatus <= TelegramUserStatus.PendingApprove)
			return "Ваша заявка была отклонена.";
		if (NewStatus is TelegramUserStatus.Refused && PreviousStatus > TelegramUserStatus.PendingApprove)
			return "Вы были заблокированы администратором.";

		if (NewStatus is TelegramUserStatus.ApprovedUser && PreviousStatus <= TelegramUserStatus.ApprovedUser)
			return "Ваша заявка была одобрена администратором.";
		if (NewStatus is TelegramUserStatus.ApprovedUser && PreviousStatus == TelegramUserStatus.Administrator)
			return "Ваши права администратора были удалены.";

		if (NewStatus is TelegramUserStatus.Administrator)
			return "Вам были выданы права администратора.";

		throw new ArgumentOutOfRangeException();
	}

	public override NotificationButton[][]? GetButtonsMarkupForDelivery()
	{
		return null;
	}

	public override Task<IList<long>> GetRecipientsIdsAsync(IServiceProvider? serviceProvider)
	{
		return Task.FromResult<IList<long>>(new[] {UserId});
	}
}

public class NotificationDelivery
{
	public Guid NotificationId { get; }
	public string HtmlContent { get; }
	public NotificationButton[][]? ButtonsMarkup { get; }
	public IList<long> RecipientsIds { get; }

	public NotificationDelivery(
		Guid notificationId,
		string htmlContent,
		NotificationButton[][]? buttonsMarkup,
		IList<long> recipientsIds
	)
	{
		HtmlContent = htmlContent;
		ButtonsMarkup = buttonsMarkup;
		RecipientsIds = recipientsIds;
		NotificationId = notificationId;
	}

	public static async Task<NotificationDelivery> FromNotificationAsync(
		Notification notification,
		IServiceProvider serviceProvider
	)
	{
		return new NotificationDelivery(
			notification.Id,
			notification.GetMessageHtmlContentForDelivery(),
			notification.GetButtonsMarkupForDelivery(),
			await notification.GetRecipientsIdsAsync(serviceProvider)
		);
	}
}

public class NotificationButton
{
	public string Text { get; set; } = null!;
	public string? CallbackData { get; set; }
	public string? Url { get; set; }
}