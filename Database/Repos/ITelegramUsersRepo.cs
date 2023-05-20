using Database.Models;
using Microsoft.EntityFrameworkCore;

namespace Database.Repos;

public interface ITelegramUsersRepo
{
	Task<TelegramUser?> FindUserAsync(long id, TelegramUserStatus minLevel = TelegramUserStatus.ApprovedUser);
	Task<TelegramUser?> FindUserAsync(string username, TelegramUserStatus minLevel = TelegramUserStatus.ApprovedUser);
	Task<IList<long>> GetUsersIdsAsync(TelegramUserStatus minLevel = TelegramUserStatus.ApprovedUser);
	Task<IList<TelegramUser>> GetUsersAsync(TelegramUserStatus minLevel = TelegramUserStatus.ApprovedUser);
	Task<TelegramUser> AddUserAsync(long id, string username);
	Task<TelegramUser> UpdateUsernameAsync(long userId, string newUsername);
	Task<TelegramUser> ApproveUserAsync(long userId);
	Task<TelegramUser> RefuseUserAsync(long userId);
	Task<TelegramUser> GrantAdminPermissions(long userId);
	Task<TelegramUser> RemoveAdminPermissions(long userId);
}

public class TelegramUsersRepo : ITelegramUsersRepo
{
	private readonly CowDb _db;

	public TelegramUsersRepo(CowDb db)
	{
		_db = db;
	}

	public async Task<TelegramUser?> FindUserAsync(long id,
		TelegramUserStatus minLevel = TelegramUserStatus.ApprovedUser)
	{
		return await _db.Users
			.Where(user => user.Status >= minLevel)
			.FirstOrDefaultAsync(user => user.Id == id);
	}

	public async Task<TelegramUser?> FindUserAsync(string username,
		TelegramUserStatus minLevel = TelegramUserStatus.ApprovedUser)
	{
		return await _db.Users
			.Where(user => user.Status >= minLevel)
			.FirstOrDefaultAsync(user => user.Username == username);
	}

	public async Task<IList<long>> GetUsersIdsAsync(TelegramUserStatus minLevel = TelegramUserStatus.ApprovedUser)
	{
		return await _db.Users
			.Where(user => user.Status >= minLevel)
			.Select(user => user.Id)
			.ToListAsync();
	}

	public async Task<IList<TelegramUser>> GetUsersAsync(TelegramUserStatus minLevel = TelegramUserStatus.ApprovedUser)
	{
		return await _db.Users
			.Where(user => user.Status >= minLevel)
			.ToListAsync();
	}

	public async Task<TelegramUser> AddUserAsync(long id, string username)
	{
		var user = await FindUserAsync(id, TelegramUserStatus.Refused);
		if (user is not null)
			return user;

		user = new TelegramUser
		{
			Id = id,
			Username = username,
			Status = TelegramUserStatus.PendingApprove
		};
		_db.Users.Add(user);
		await _db.SaveChangesAsync().ConfigureAwait(false);
		return user;
	}

	public async Task<TelegramUser> UpdateUsernameAsync(long userId, string newUsername)
	{
		var user = await FindUserAsync(userId, TelegramUserStatus.Refused) ??
		           throw new ArgumentException($"Can't find user with id ${userId}");

		user.Username = newUsername;
		await _db.SaveChangesAsync().ConfigureAwait(false);
		return user;
	}

	public async Task<TelegramUser> ApproveUserAsync(long userId)
	{
		var user = await FindUserAsync(userId, TelegramUserStatus.Refused) ??
		           throw new ArgumentException($"Can't find user with id {userId}");

		if (user.Status > TelegramUserStatus.PendingApprove)
			return user;

		user.Status = TelegramUserStatus.ApprovedUser;
		await _db.SaveChangesAsync().ConfigureAwait(false);
		return user;
	}

	public async Task<TelegramUser> RefuseUserAsync(long userId)
	{
		var user = await FindUserAsync(userId, TelegramUserStatus.Refused) ??
		           throw new ArgumentException($"Can't find user with id ${userId}");

		if (user.Status == TelegramUserStatus.Refused)
			return user;

		user.Status = TelegramUserStatus.Refused;
		await _db.SaveChangesAsync().ConfigureAwait(false);
		return user;
	}

	public async Task<TelegramUser> GrantAdminPermissions(long userId)
	{
		var user = await FindUserAsync(userId, TelegramUserStatus.Refused) ??
		           throw new ArgumentException($"Can't find user with id ${userId}");

		if (user.Status == TelegramUserStatus.Administrator)
			return user;

		user.Status = TelegramUserStatus.Administrator;
		await _db.SaveChangesAsync().ConfigureAwait(false);
		return user;
	}

	public async Task<TelegramUser> RemoveAdminPermissions(long userId)
	{
		var user = await FindUserAsync(userId, TelegramUserStatus.Refused) ??
		           throw new ArgumentException($"Can't find user with id ${userId}");

		if (user.Status != TelegramUserStatus.Administrator)
			return user;

		user.Status = TelegramUserStatus.ApprovedUser;
		await _db.SaveChangesAsync().ConfigureAwait(false);
		return user;
	}
}