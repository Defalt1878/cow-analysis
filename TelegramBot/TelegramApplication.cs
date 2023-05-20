using System.Reflection;
using Core;
using Core.Metrics;
using Database;
using Database.Di;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Polling;
using TelegramBot.Telegram;
using TelegramBot.Telegram.Commands;
using TelegramBot.Telegram.Commands.Users;
using TelegramBot.Telegram.Notifications;
using Vostok.Hosting.Abstractions;
using Vostok.Logging.Abstractions;

namespace TelegramBot;

public class TelegramApplication : BaseApplication
{
	private ServiceKeepAliver _keepAliver = null!;
	private TimeSpan _keepAliveInterval;
	private TimeSpan _notificationsSendDelay;
	private CancellationToken _token;

	private static ILog Log => LogProvider.Get().ForContext(typeof(TelegramApplication));

	public override async Task InitializeAsync(IVostokHostingEnvironment environment)
	{
		await base.InitializeAsync(environment);

		_keepAliver = new ServiceKeepAliver(Configuration.GraphiteServiceName);
		_keepAliveInterval = TimeSpan.FromSeconds(Configuration.KeepAliveInterval ?? 30);
		_notificationsSendDelay = TimeSpan.FromSeconds(Configuration.NotificationsUpdateDelay ?? 5);
		_token = environment.ShutdownToken;
	}

	public override Task RunAsync(IVostokHostingEnvironment environment)
	{
		var bot = ServiceProvider.GetRequiredService<ITelegramBot>();
		bot.InitializeBot(_token);
		bot.StartReceiving(_token);

		MainLoop().Wait(_token);
		return Task.CompletedTask;
	}

	protected override void ConfigureServices(IServiceCollection services, IVostokHostingEnvironment hostingEnvironment)
	{
		base.ConfigureServices(services, hostingEnvironment);

		services.AddDbContext<CowDb>(options => options
			.UseLazyLoadingProxies()
			.UseNpgsql(Configuration.Database, o => o.SetPostgresVersion(13, 2))
		);
	}

	protected override void ConfigureDi(IServiceCollection services)
	{
		base.ConfigureDi(services);

		services.AddSingleton<ITelegramBot, Telegram.TelegramBot>();
		services.AddSingleton<IUpdateHandler, TelegramUpdateHandler>();
		
		var tgCommandBase = typeof(ITelegramCommand);
		foreach (var command in DerivedTypesHelper.GetDerivedTypes(tgCommandBase)) 
			services.AddSingleton(tgCommandBase, command);

		services.AddScoped<INotificationsSender, NotificationsSender>();

		services.AddDatabaseServices();
	}

	public async Task MainLoop()
	{
		var scopeFactory = ServiceProvider.GetRequiredService<IServiceScopeFactory>();

		await using var scope = scopeFactory.CreateAsyncScope();
		var notificationsSender = scope.ServiceProvider.GetRequiredService<INotificationsSender>();

		while (true)
		{
			_token.ThrowIfCancellationRequested();
			_keepAliver.Ping(_keepAliveInterval);
			await notificationsSender.SendAllAsync(_token);
			_keepAliver.Ping(_keepAliveInterval);
			await Task.Delay(_notificationsSendDelay, _token);
		}
		// ReSharper disable once FunctionNeverReturns
	}
}