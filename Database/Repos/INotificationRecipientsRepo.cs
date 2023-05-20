// using Database.Models;
// using Microsoft.EntityFrameworkCore;
//
// namespace Database.Repos;
//
// public interface INotificationRecipientsRepo
// {
// 	Task<TelegramNotificationRecipient?> FindRecipient(long chatId, bool includeDisabled = false);
// 	Task<TelegramNotificationRecipient?> FindRecipient(string name);
// 	Task<IList<TelegramNotificationRecipient>> GetRecipients();
// 	Task<TelegramNotificationRecipient> AddRecipient(long chatId, string name);
// 	Task DeleteRecipient(long chatId);
// }
//
// public class NotificationRecipientsRepo : INotificationRecipientsRepo
// {
// 	private readonly CowDb _db;
//
// 	public NotificationRecipientsRepo(CowDb db)
// 	{
// 		_db = db;
// 	}
//
// 	public async Task<TelegramNotificationRecipient?> FindRecipient(long chatId, bool includeDisabled = false)
// 	{
// 		return await _db.NotificationRecipients
// 			.Where(recipient => includeDisabled || !recipient.Disabled)
// 			.FirstOrDefaultAsync(recipient => recipient.ChatId == chatId);
// 	}
//
// 	public async Task<TelegramNotificationRecipient?> FindRecipient(string name, bool includeDisabled = false)
// 	{
// 		return await _db.NotificationRecipients
// 			.Where(recipient => includeDisabled || !recipient.Disabled)
// 			.FirstOrDefaultAsync(recipient => recipient.Name == name);
// 	}
//
// 	public async Task<IList<TelegramNotificationRecipient>> GetRecipients()
// 	{
// 		return await _db.NotificationRecipients
// 			.Where(recipient => !recipient.Disabled)
// 			.ToListAsync();
// 	}
//
// 	public async Task<TelegramNotificationRecipient> AddRecipient(long chatId, string name)
// 	{
// 		var recipient = await FindRecipient(chatId);
// 		if (recipient is {Disabled: false})
// 			return recipient;
//
// 		if (recipient is {Disabled: true})
// 		{
// 			recipient.Disabled = false;
// 		}
// 		else
// 		{
// 			recipient = new TelegramNotificationRecipient
// 			{
// 				ChatId = chatId,
// 				Name = name
// 			};
// 			await _db.NotificationRecipients.Add(recipient);
// 		}
//
// 		await _db.SaveChangesAsync().ConfigureAwait(false);
// 		return recipient;
// 	}
//
// 	public async Task DeleteRecipient(long chatId)
// 	{
// 		var recipient = await FindRecipient(chatId);
// 		if (recipient is null or {Disabled: true})
// 			return;
//
// 		recipient.Disabled = true;
// 		await _db.SaveChangesAsync().ConfigureAwait(false);
// 	}
// }