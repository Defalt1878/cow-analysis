using Database.Repos;
using Microsoft.Extensions.DependencyInjection;

namespace Database.Di;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddDatabaseServices(this IServiceCollection services)
	{
		services.AddScoped<ICamerasRepo, CamerasRepo>();
		services.AddScoped<ICamerasAnalysesRepo, CamerasAnalysesRepo>();
		services.AddScoped<INotificationsRepo, NotificationsRepo>();
		services.AddScoped<ITelegramUsersRepo, TelegramUsersRepo>();
		// services.AddScoped<INotificationRecipientsRepo, NotificationRecipientsRepo>();

		return services;
	}
}