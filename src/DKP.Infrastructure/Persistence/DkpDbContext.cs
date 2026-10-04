using DKP.Domain;
using Microsoft.EntityFrameworkCore;

namespace DKP.Infrastructure.Persistence;

public sealed class DkpDbContext(DbContextOptions<DkpDbContext> options) : DbContext(options)
{
	public DbSet<User> Users => Set<User>();
	public DbSet<Character> Characters => Set<Character>();
	public DbSet<DkpTransaction> DkpTransactions => Set<DkpTransaction>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<User>(entity =>
		{
			entity.HasKey(user => user.Id);
			entity.HasIndex(user => user.DiscordId).IsUnique();
			entity.Property(user => user.DiscordId).HasMaxLength(32).IsRequired();
			entity.Property(user => user.DiscordName).HasMaxLength(128).IsRequired();
			entity.Property(user => user.AvatarUrl).HasMaxLength(512);
			entity.Property(user => user.Role).HasConversion<string>().HasMaxLength(32).IsRequired();
			entity.HasMany(user => user.Characters)
				.WithOne(character => character.User)
				.HasForeignKey(character => character.UserId)
				.OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<Character>(entity =>
		{
			entity.HasKey(character => character.Id);
			entity.Property(character => character.FirstName).HasMaxLength(64).IsRequired();
			entity.Property(character => character.LastName).HasMaxLength(64).IsRequired();
			entity.HasIndex(character => new { character.UserId, character.FirstName, character.LastName }).IsUnique();
			entity.HasIndex(character => new { character.UserId, character.IsMain })
				.IsUnique()
				.HasFilter("\"IsMain\" = TRUE");
		});

		modelBuilder.Entity<DkpTransaction>(entity =>
		{
			entity.HasKey(transaction => transaction.Id);
			entity.Property(transaction => transaction.Amount).IsRequired();
			entity.Property(transaction => transaction.Reason).HasMaxLength(500).IsRequired();
			entity.Property(transaction => transaction.CreatedAtUtc).IsRequired();
			entity.HasIndex(transaction => new { transaction.UserId, transaction.CreatedAtUtc });
			entity.HasIndex(transaction => transaction.CreatedByUserId);
			entity.HasOne(transaction => transaction.User)
				.WithMany(user => user.DkpTransactions)
				.HasForeignKey(transaction => transaction.UserId)
				.OnDelete(DeleteBehavior.Restrict);
			entity.HasOne(transaction => transaction.CreatedByUser)
				.WithMany(user => user.CreatedDkpTransactions)
				.HasForeignKey(transaction => transaction.CreatedByUserId)
				.OnDelete(DeleteBehavior.Restrict);
		});
	}
}
