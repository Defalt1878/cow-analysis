using Database.Models;
using Microsoft.EntityFrameworkCore;

namespace Database.Repos;

public interface ICamerasRepo
{
	public Task<IList<Camera>> GetCamerasAsync(CameraState? state = null);
	public Task<Camera?> FindCameraAsync(Guid id, bool includeDeleted = false);
	public Task<Camera?> FindCameraByAddressAsync(string address, bool includeDeleted = false);
	public Task<Camera> UpdateCameraStateAsync(Guid cameraId, CameraState cameraState);
	public Task<Camera> AddCameraAsync(string address, CameraState cameraState = CameraState.Disabled);
	public Task DeleteCameraAsync(Guid id);
}

public class CamerasRepo : ICamerasRepo
{
	private readonly CowDb _db;

	public CamerasRepo(CowDb db)
	{
		_db = db;
	}

	public async Task<IList<Camera>> GetCamerasAsync(CameraState? state = null)
	{
		if (state is null)
			return await _db.Cameras
				.Where(camera => !camera.IsDeleted)
				.ToListAsync();

		return await _db.Cameras
			.Where(camera => !camera.IsDeleted && camera.CameraState == state)
			.ToListAsync();
	}

	public async Task<Camera?> FindCameraAsync(Guid id, bool includeDeleted = false)
	{
		return await _db.Cameras
			.Where(camera => includeDeleted || !camera.IsDeleted)
			.FirstOrDefaultAsync(camera => camera.Id == id);
	}

	public async Task<Camera?> FindCameraByAddressAsync(string address, bool includeDeleted = false)
	{
		return await _db.Cameras
			.Where(camera => includeDeleted || !camera.IsDeleted)
			.FirstOrDefaultAsync(camera => camera.Address == address);
	}

	public async Task<Camera> UpdateCameraStateAsync(Guid cameraId, CameraState cameraState)
	{
		var camera = await FindCameraAsync(cameraId) ??
		             throw new ArgumentException($"Can't find camera with id={cameraId}", nameof(cameraId));
		camera.CameraState = cameraState;
		await _db.SaveChangesAsync().ConfigureAwait(false);
		return camera;
	}

	public async Task<Camera> AddCameraAsync(string address, CameraState cameraState = CameraState.Disabled)
	{
		var camera = await FindCameraByAddressAsync(address, true);
		if (camera is not null)
		{
			if (!camera.IsDeleted)
				return camera;

			camera.IsDeleted = false;
			camera.CameraState = cameraState;
		}
		else
		{
			camera = new Camera
			{
				Id = Guid.NewGuid(),
				Address = address,
				CameraState = cameraState
			};

			_db.Cameras.Add(camera);
		}

		await _db.SaveChangesAsync().ConfigureAwait(false);
		return camera;
	}

	public async Task DeleteCameraAsync(Guid id)
	{
		var camera = await FindCameraAsync(id);
		if (camera is null)
			return;

		camera.IsDeleted = true;
		await _db.SaveChangesAsync().ConfigureAwait(false);
	}
}