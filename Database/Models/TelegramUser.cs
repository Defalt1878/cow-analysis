using System.ComponentModel.DataAnnotations;

namespace Database.Models;

public enum TelegramUserStatus
{
	Refused = 0,
	PendingApprove = 1,
	ApprovedUser = 2,
	Administrator = 3
}

public class TelegramUser
{
	[Key]
	public long Id { get; set; }

	[Required]
	public string Username { get; set; } = null!;

	[Required]
	public TelegramUserStatus Status { get; set; }
}