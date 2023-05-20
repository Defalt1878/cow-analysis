using System.ComponentModel.DataAnnotations;

namespace Database.Models;

public enum CameraState
{
	Disabled = 0,
	Active = 1,
	OutOfOrder = 2
}

public class Camera
{
	[Key]
	public Guid Id { get; set; }

	[Required]
	public string Address { get; set; } = null!;

	[Required]
	public CameraState CameraState { get; set; }

	[Required]
	public bool IsDeleted { get; set; }

	public override string ToString()
	{
		return $"{Id}: {Address}, {CameraState}";
	}
}