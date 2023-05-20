using Microsoft.Extensions.DependencyInjection;
using Vostok.Hosting.Abstractions;
using Vostok.Logging.Microsoft;

namespace Core;

public abstract class BaseApplication : IVostokApplication
{
	protected static CowConfiguration Configuration = null!;
	protected IServiceProvider ServiceProvider = null!;
	
	public virtual Task InitializeAsync(IVostokHostingEnvironment environment)
	{
		var services = new ServiceCollection();
		ConfigureServices(services, environment);
		ServiceProvider = services.BuildServiceProvider();
		environment.HostExtensions.AsMutable().Add(ServiceProvider);
		return Task.CompletedTask;
	}

	protected virtual void ConfigureServices(IServiceCollection services, IVostokHostingEnvironment hostingEnvironment)
	{
		services.AddLogging(builder => builder.AddVostok(hostingEnvironment.Log));
		Configuration = hostingEnvironment.SecretConfigurationProvider
			.Get<CowConfiguration>(hostingEnvironment.SecretConfigurationSource);

		services.Configure<CowConfiguration>(options =>
			options.SetFrom(hostingEnvironment.SecretConfigurationProvider
				.Get<CowConfiguration>(hostingEnvironment.SecretConfigurationSource))
		);

		ConfigureDi(services);
	}

	protected virtual void ConfigureDi(IServiceCollection services)
	{
	}

	public abstract Task RunAsync(IVostokHostingEnvironment environment);
}