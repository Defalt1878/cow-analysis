using Core;
using Core.Metrics;
using Database;
using Database.Di;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vostok.Hosting.Abstractions;
using Vostok.Logging.Abstractions;

namespace AnalysesWatcher;

public class AnalysesWatcherApplication : BaseApplication
{
	private ServiceKeepAliver _keepAliver = null!;
	private TimeSpan _keepAliveInterval;
	private TimeSpan _updateInterval;
	private CancellationToken _token;

	private static ILog Log => LogProvider.Get().ForContext<AnalysesWatcherApplication>();

	public override async Task InitializeAsync(IVostokHostingEnvironment environment)
	{
		await base.InitializeAsync(environment);

		_keepAliver = new ServiceKeepAliver(Configuration.GraphiteServiceName);
		_keepAliveInterval = TimeSpan.FromSeconds(Configuration.KeepAliveInterval ?? 30);
		_updateInterval = TimeSpan.FromSeconds(Configuration.AnalysesUpdateIntervalInSeconds);
		_token = environment.ShutdownToken;
	}

	public override async Task RunAsync(IVostokHostingEnvironment environment)
	{
		await MainLoop();
	}

	protected override void ConfigureServices(IServiceCollection services, IVostokHostingEnvironment hostingEnvironment)
	{
		base.ConfigureServices(services, hostingEnvironment);

		services.AddSingleton<ICameraWatchersFactory, CameraWatchersFactory>();
		services.AddSingleton<IDbAnalyzesWatcher, DbAnalyzesWatcher>();
		services.AddDbContext<CowDb>(options => options
			.UseLazyLoadingProxies()
			.UseNpgsql(Configuration.Database, o => o.SetPostgresVersion(13, 2))
		);
	}

	protected override void ConfigureDi(IServiceCollection services)
	{
		base.ConfigureDi(services);

		services.AddDatabaseServices();
	}

	private async Task MainLoop()
	{
		var analyzesWatcher = ServiceProvider.GetRequiredService<IDbAnalyzesWatcher>();
		
		while (true)
		{
			await Task.Delay(10000, _token);
			_token.ThrowIfCancellationRequested();
			_keepAliver.Ping(_keepAliveInterval);
			try
			{
				await analyzesWatcher.BuildCamerasNotificationsAsync(_updateInterval);
			}
			catch (Exception e)
			{
				Log.Error(e, "Unable to build notifications.");
			}
			_keepAliver.Ping(_keepAliveInterval);
		}
		// ReSharper disable once FunctionNeverReturns
	}
}