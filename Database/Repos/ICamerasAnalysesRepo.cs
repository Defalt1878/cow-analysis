using Database.Models;
using Microsoft.EntityFrameworkCore;

namespace Database.Repos;

public interface ICamerasAnalysesRepo
{
	public Task<CameraAnalysis?> FindAnalysisAsync(Guid id);
	public Task<IList<CameraAnalysis>> GetLastAnalyzesAsync(Guid cameraId, TimeSpan timeSpan);
	public Task<IList<CameraAnalysis>> GetLastAnalyzesAsync(Guid cameraId, int count);
	public Task<CameraAnalysis?> FindLastAnalysisAsync(Guid cameraId);
	public Task<CameraAnalysis?> FindAnalysisByDateTimeAsync(Guid cameraId, DateTime dateTime);
	public Task<CameraAnalysis> AddAnalysisAsync(Guid cameraId, int cowCount, int calfCount);
	public Task DeleteAnalysisAsync(Guid id);
}

public class CamerasAnalysesRepo : ICamerasAnalysesRepo
{
	private readonly CowDb _db;

	public CamerasAnalysesRepo(CowDb db)
	{
		_db = db;
	}

	public async Task<CameraAnalysis?> FindAnalysisAsync(Guid id)
	{
		return await _db.CamerasAnalyses.FirstOrDefaultAsync(analysis => analysis.Id == id);
	}

	public async Task<IList<CameraAnalysis>> GetLastAnalyzesAsync(Guid cameraId, TimeSpan timeSpan)
	{
		var now = DateTime.UtcNow;
		return await _db.CamerasAnalyses
			.Where(analysis => analysis.CameraId == cameraId && now - analysis.DateTime < timeSpan)
			.ToListAsync();
	}

	public async Task<IList<CameraAnalysis>> GetLastAnalyzesAsync(Guid cameraId, int count)
	{
		if (count < 0)
		{
			throw new ArgumentException("Count cannot be less than zero", nameof(count));
		}

		return await _db.CamerasAnalyses
			.Where(analysis => analysis.CameraId == cameraId)
			.Take(count)
			.ToListAsync();
	}

	public async Task<CameraAnalysis?> FindLastAnalysisAsync(Guid cameraId)
	{
		return await _db.CamerasAnalyses
			.FirstOrDefaultAsync(analysis => analysis.CameraId == cameraId).ConfigureAwait(false);
	}

	public async Task<CameraAnalysis?> FindAnalysisByDateTimeAsync(Guid cameraId, DateTime dateTime)
	{
		return await _db.CamerasAnalyses
			.Where(analysis => analysis.CameraId == cameraId)
			.Where(analysis => Math.Abs((dateTime - analysis.DateTime).TotalMinutes) < 10)
			.OrderBy(analysis => Math.Abs((dateTime - analysis.DateTime).TotalMinutes))
			.FirstOrDefaultAsync(analysis => analysis.CameraId == cameraId);
	}

	public async Task<CameraAnalysis> AddAnalysisAsync(Guid cameraId, int cowCount, int calfCount)
	{
		var analysis = new CameraAnalysis
		{
			CameraId = cameraId,
			DateTime = DateTime.UtcNow,
			CowCount = cowCount,
			CalfCount = calfCount
		};
		_db.CamerasAnalyses.Add(analysis);
		await _db.SaveChangesAsync().ConfigureAwait(false);
		return analysis;
	}

	public async Task DeleteAnalysisAsync(Guid id)
	{
		var analysis = await FindAnalysisAsync(id).ConfigureAwait(false);
		
		if (analysis is null)
			return;

		_db.CamerasAnalyses.Remove(analysis);
		await _db.SaveChangesAsync().ConfigureAwait(false);
	}
}