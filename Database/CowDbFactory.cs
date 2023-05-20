using Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Database;

public class CowDbFactory : IDesignTimeDbContextFactory<CowDb>
{
	public CowDb CreateDbContext(string[] args)
	{
		var optionsBuilder = new DbContextOptionsBuilder<CowDb>();
		optionsBuilder
			.UseLazyLoadingProxies() 
			.UseNpgsql(
			ApplicationConfiguration.Read<DatabaseConfiguration>()!.Database,
			o => o.SetPostgresVersion(13, 2)
		);

		return new CowDb(optionsBuilder.Options);
	}
}