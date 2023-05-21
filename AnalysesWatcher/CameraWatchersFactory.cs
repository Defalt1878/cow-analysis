using Database.Models;
using Database.Repos;
using Microsoft.Extensions.DependencyInjection;

namespace AnalysesWatcher;

public interface ICameraWatchersFactory
{
	ICameraAnalysesWatcher GetWatcher(Camera camera);
}

public class CameraWatchersFactory : ICameraWatchersFactory
{
	private readonly IServiceProvider _serviceProvider;

	public CameraWatchersFactory(IServiceProvider serviceProvider)
	{
		_serviceProvider = serviceProvider;
	}

	public ICameraAnalysesWatcher GetWatcher(Camera camera) =>
		new CameraAnalysesWatcher(
			_serviceProvider.GetRequiredService<ICamerasAnalysesRepo>(),
			_serviceProvider.GetRequiredService<INotificationsRepo>(),
			camera
		);
}