using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DKP.Infrastructure.Persistence;

public sealed class DkpDbContextFactory : IDesignTimeDbContextFactory<DkpDbContext>
{
	public DkpDbContext CreateDbContext(string[] args)
	{
		var options = new DbContextOptionsBuilder<DkpDbContext>()
			.UseNpgsql("Host=localhost;Port=5432;Database=dkp;Username=postgres;Password=postgres")
			.Options;

		return new DkpDbContext(options);
	}
}
