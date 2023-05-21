using Database.Models;
using Database.Repos;
using Vostok.Logging.Abstractions;

namespace AnalysesWatcher;

public interface ICameraAnalysesWatcher
{
	Task BuildNotificationsAsync(TimeSpan minInterval);
}

public class CameraAnalysesWatcher : ICameraAnalysesWatcher
{
	private readonly ICamerasAnalysesRepo _analysesRepo;
	private readonly INotificationsRepo _notificationsRepo;
	private readonly Camera _camera;

	private DateTime _lastTimeCheck;
	private AnalysisState? _lastAnalysisState;

	private static ILog Log => LogProvider.Get().ForContext<CameraAnalysesWatcher>();

	public CameraAnalysesWatcher(
		ICamerasAnalysesRepo analysesRepo,
		INotificationsRepo notificationsRepo,
		Camera camera
	)
	{
		_analysesRepo = analysesRepo;
		_notificationsRepo = notificationsRepo;
		_camera = camera;
		_lastTimeCheck = DateTime.UtcNow;
	}

	public async Task BuildNotificationsAsync(TimeSpan minInterval)
	{
		var currentTime = DateTime.UtcNow;
		var elapsed = currentTime - _lastTimeCheck;
		if (elapsed < minInterval)
			return;

		Log.Info($"Requesting analyzes for camera: {_camera.Id}");
		var currentState = await GetCurrentAnalysisStateAsync(elapsed);
		if (currentState is null)
		{
			_lastAnalysisState = currentState;
			_lastTimeCheck = currentTime;
			return;
		}

		if (_lastAnalysisState is not null && (
			    _lastAnalysisState.CowCount != currentState.CowCount ||
			    _lastAnalysisState.CalfCount != currentState.CalfCount
		    ))
		{
			Log.Info($"Camera: {_camera.Id}. State changed. Creating notification.");
			await _notificationsRepo.AddNotificationAsync(new AnalysisNotification
			{
				Camera = _camera,
				PreviousCowCount = _lastAnalysisState.CowCount,
				PreviousCalfCount = _lastAnalysisState.CalfCount,
				NewCowCount = currentState.CowCount,
				NewCalfCount = currentState.CalfCount
			});
		}

		_lastAnalysisState = currentState;
		_lastTimeCheck = currentTime;
	}

	private async Task<AnalysisState?> GetCurrentAnalysisStateAsync(TimeSpan interval)
	{
		var analyses = await _analysesRepo.GetLastAnalyzesAsync(_camera.Id, interval);
		Log.Info($"Camera: {_camera.Id}. Analyzes count: {analyses.Count} during {interval}.");

		if (analyses.Count == 0)
			return null;

		var cowCount = 0;
		var calfCount = 0;
		foreach (var cameraAnalysis in analyses)
		{
			cowCount += cameraAnalysis.CowCount;
			calfCount += cameraAnalysis.CalfCount;
		}

		cowCount /= analyses.Count;
		calfCount /= analyses.Count;
		return new AnalysisState(cowCount, calfCount);
	}
}