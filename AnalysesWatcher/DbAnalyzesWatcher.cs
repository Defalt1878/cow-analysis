using Database.Models;
using Database.Repos;
using Vostok.Logging.Abstractions;

namespace AnalysesWatcher;

public interface IDbAnalyzesWatcher
{
	Task BuildCamerasNotificationsAsync(TimeSpan minInterval);
}

public class DbAnalyzesWatcher : IDbAnalyzesWatcher
{
	private readonly ICameraWatchersFactory _watchersFactory;
	private readonly ICamerasRepo _camerasRepo;
	private readonly Dictionary<Guid, ICameraAnalysesWatcher> _watchersByCameraId = new();

	private static ILog Log => LogProvider.Get().ForContext<DbAnalyzesWatcher>();

	public DbAnalyzesWatcher(ICameraWatchersFactory watchersFactory, ICamerasRepo camerasRepo)
	{
		_watchersFactory = watchersFactory;
		_camerasRepo = camerasRepo;
	}

	public async Task BuildCamerasNotificationsAsync(TimeSpan minInterval)
	{
		var existingWatchers = new HashSet<Guid>(_watchersByCameraId.Keys);
		Log.Info("Updating cameras");
		var cameras = await _camerasRepo.GetCamerasAsync(CameraState.Active);
		Log.Info($"Current active cameras count: {cameras.Count}");
		foreach (var camera in cameras)
		{
			if (!existingWatchers.Remove(camera.Id))
			{
				Log.Info($"Initializing new watcher for camera: {camera.Id}");
				_watchersByCameraId[camera.Id] = _watchersFactory.GetWatcher(camera);
			}

			await _watchersByCameraId[camera.Id].BuildNotificationsAsync(minInterval);
		}

		foreach (var cameraId in existingWatchers)
		{
			Log.Info($"Removing watcher for camera: {cameraId}");
			_watchersByCameraId.Remove(cameraId);
		}
	}
}