using Database.Models;
using Microsoft.EntityFrameworkCore;

namespace Database.Repos;

public interface INotificationsRepo
{
	Task<Notification?> FindNotification(Guid id);
	Task<IList<NotificationDelivery>> GetNotificationDeliveriesAsync();
	Task AddNotificationAsync(Notification notification);
	Task<Notification> MarkNotificationsAsSentAsync(Guid id);
}

public class NotificationsRepo : INotificationsRepo
{
	private readonly CowDb _db;
	private readonly IServiceProvider _serviceProvider;

	public NotificationsRepo(CowDb db, IServiceProvider serviceProvider)
	{
		_db = db;
		_serviceProvider = serviceProvider;
	}

	public async Task<Notification?> FindNotification(Guid id)
	{
		return await _db.Notifications.FirstOrDefaultAsync(notification => notification.Id == id);
	}

	public async Task<IList<NotificationDelivery>> GetNotificationDeliveriesAsync()
	{
		var notifications = await GetNotSentNotificationAsync();

		var deliveries = new NotificationDelivery[notifications.Count];
		for (var i = 0; i < notifications.Count; i++)
			deliveries[i] = await NotificationDelivery.FromNotificationAsync(notifications[i], _serviceProvider);

		return deliveries;
	}

	public async Task<IList<Notification>> GetNotSentNotificationAsync()
	{
		return await _db.Notifications
			.Where(notification => !notification.IsSent)
			.OrderBy(notification => notification.CreateTime)
			.ToListAsync();
	}

	public async Task AddNotificationAsync(Notification notification)
	{
		notification.CreateTime = DateTime.UtcNow;
		notification.IsSent = false;

		_db.Notifications.Add(notification);
		await _db.SaveChangesAsync().ConfigureAwait(false);
	}

	public async Task<Notification> MarkNotificationsAsSentAsync(Guid id)
	{
		var notification = await FindNotification(id) ??
		                   throw new ArgumentException($"Cannot find notification with id: {id}", nameof(id));
		if (notification.IsSent)
			return notification;

		notification.IsSent = true;
		await _db.SaveChangesAsync().ConfigureAwait(false);
		return notification;
	}
}