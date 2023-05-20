using System.ComponentModel.DataAnnotations;
using Database.Models;
using Microsoft.EntityFrameworkCore;

namespace Database;

public class CowDb : DbContext
{
	public DbSet<Camera> Cameras { get; set; } = null!;
	public DbSet<CameraAnalysis> CamerasAnalyses { get; set; } = null!;
	public DbSet<Notification> Notifications { get; set; } = null!;
	public DbSet<TelegramUser> Users { get; set; } = null!;

	public CowDb(DbContextOptions<CowDb> options) : base(options)
	{
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		modelBuilder.HasCollation("case_insensitive", locale: "und@colStrength=secondary", provider: "icu",
			deterministic: false);
		modelBuilder.UseCollation("case_insensitive");

		modelBuilder.Entity<CameraAnalysis>()
			.HasOne(x => x.Camera)
			.WithMany()
			.HasForeignKey(x => x.CameraId)
			.OnDelete(DeleteBehavior.Restrict);

		var notificationClasses = GetNonAbstractSubclasses(typeof(Notification));
		foreach (var notificationClass in notificationClasses)
			modelBuilder.Entity(notificationClass);

		modelBuilder.Entity<Notification>()
			.HasIndex(x => x.IsSent)
			.IsUnique(false);

		modelBuilder.Entity<Camera>()
			.HasIndex(x => x.Address)
			.IsUnique();

		modelBuilder.Entity<TelegramUser>()
			.HasIndex(x => x.Username)
			.IsUnique();
	}

	public void MigrateToLatestVersion()
	{
		Database.SetCommandTimeout(TimeSpan.FromMinutes(5));
		Database.Migrate();
		Database.SetCommandTimeout(TimeSpan.FromSeconds(30));
	}

	private static List<Type> GetNonAbstractSubclasses(Type type)
	{
		return type.Assembly.GetTypes()
			.Where(t => t.IsSubclassOf(type) && !t.IsAbstract && t != type)
			.ToList();
	}

	public override int SaveChanges()
	{
		ValidateChanges();
		return base.SaveChanges();
	}

	public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
		CancellationToken cancellationToken = new())
	{
		ValidateChanges();
		return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
	}

	private void ValidateChanges()
	{
		var entities = ChangeTracker.Entries()
			.Where(e => e.State is EntityState.Added or EntityState.Modified)
			.Select(e => e.Entity);
		foreach (var entity in entities)
		{
			var validationContext = new ValidationContext(entity);
			Validator.ValidateObject(entity, validationContext);
		}
	}
}