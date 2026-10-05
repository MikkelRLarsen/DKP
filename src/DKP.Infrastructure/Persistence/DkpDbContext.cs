using DKP.Domain;
using Microsoft.EntityFrameworkCore;

namespace DKP.Infrastructure.Persistence;

public sealed class DkpDbContext(DbContextOptions<DkpDbContext> options) : DbContext(options)
{
	public DbSet<User> Users => Set<User>();
	public DbSet<Character> Characters => Set<Character>();
	public DbSet<DkpTransaction> DkpTransactions => Set<DkpTransaction>();
	public DbSet<SoftReservePurchase> SoftReservePurchases => Set<SoftReservePurchase>();
	public DbSet<ShopItem> ShopItems => Set<ShopItem>();
	public DbSet<ShopPurchase> ShopPurchases => Set<ShopPurchase>();
	public DbSet<DkpAwardPreset> DkpAwardPresets => Set<DkpAwardPreset>();
	public DbSet<DkpAwardPresetApplication> DkpAwardPresetApplications => Set<DkpAwardPresetApplication>();
	public DbSet<GuildSetting> GuildSettings => Set<GuildSetting>();
	public DbSet<DkpEvent> DkpEvents => Set<DkpEvent>();
	public DbSet<DkpBalanceProjection> DkpBalanceProjections => Set<DkpBalanceProjection>();
	public DbSet<ShopPurchaseProjection> ShopPurchaseProjections => Set<ShopPurchaseProjection>();

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
			entity.Property(user => user.BlockReason).HasMaxLength(500);
			entity.Property(user => user.RollBonus).IsRequired();
			entity.HasIndex(user => user.IsBlocked);
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

		modelBuilder.Entity<SoftReservePurchase>(entity =>
		{
			entity.HasKey(purchase => purchase.Id);
			entity.Property(purchase => purchase.Quantity).IsRequired();
			entity.Property(purchase => purchase.DkpCost).IsRequired();
			entity.Property(purchase => purchase.CreatedAtUtc).IsRequired();
			entity.Property(purchase => purchase.CancelledAtUtc);
			entity.HasIndex(purchase => purchase.UserId);
			entity.HasOne(purchase => purchase.User)
				.WithMany(user => user.SoftReservePurchases)
				.HasForeignKey(purchase => purchase.UserId)
				.OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<ShopItem>(entity =>
		{
			entity.HasKey(item => item.Id);
			entity.HasIndex(item => item.Key).IsUnique();
			entity.Property(item => item.Key).HasMaxLength(64).IsRequired();
			entity.Property(item => item.Name).HasMaxLength(128).IsRequired();
			entity.Property(item => item.Description).HasMaxLength(500).IsRequired();
			entity.Property(item => item.Price).IsRequired();
			entity.Property(item => item.MaxPerUser).IsRequired();
			entity.Property(item => item.CreatedAtUtc).IsRequired();
			entity.Property(item => item.UpdatedAtUtc).IsRequired();
			entity.Property(item => item.RollBonusValue);
		});

		modelBuilder.Entity<ShopPurchase>(entity =>
		{
			entity.HasKey(purchase => purchase.Id);
			entity.Property(purchase => purchase.Quantity).IsRequired();
			entity.Property(purchase => purchase.TotalDkpCost).IsRequired();
			entity.HasIndex(purchase => new { purchase.UserId, purchase.ShopItemId });
			entity.HasIndex(purchase => purchase.CreatedAtUtc);
			entity.HasOne(purchase => purchase.User).WithMany(user => user.ShopPurchases).HasForeignKey(purchase => purchase.UserId).OnDelete(DeleteBehavior.Restrict);
			entity.HasOne(purchase => purchase.ShopItem).WithMany(item => item.Purchases).HasForeignKey(purchase => purchase.ShopItemId).OnDelete(DeleteBehavior.Restrict);
			entity.HasOne(purchase => purchase.CreatedByUser).WithMany(user => user.CreatedShopPurchases).HasForeignKey(purchase => purchase.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
		});

		modelBuilder.Entity<DkpAwardPreset>(entity =>
		{
			entity.HasKey(preset => preset.Id);
			entity.HasIndex(preset => preset.Name).IsUnique();
			entity.Property(preset => preset.Name).HasMaxLength(128).IsRequired();
			entity.Property(preset => preset.Reason).HasMaxLength(500).IsRequired();
		});

		modelBuilder.Entity<DkpAwardPresetApplication>(entity =>
		{
			entity.HasKey(application => application.Id);
			entity.HasIndex(application => new { application.PresetId, application.UserId });
			entity.HasOne(application => application.Preset).WithMany().HasForeignKey(application => application.PresetId).OnDelete(DeleteBehavior.Restrict);
			entity.HasOne(application => application.User).WithMany().HasForeignKey(application => application.UserId).OnDelete(DeleteBehavior.Restrict);
			entity.HasOne(application => application.DkpTransaction).WithMany().HasForeignKey(application => application.DkpTransactionId).OnDelete(DeleteBehavior.Restrict);
			entity.HasOne(application => application.AppliedByUser).WithMany().HasForeignKey(application => application.AppliedByUserId).OnDelete(DeleteBehavior.Restrict);
		});

		modelBuilder.Entity<GuildSetting>(entity =>
		{
			entity.HasKey(setting => setting.Id);
			entity.Property(setting => setting.DefaultReserveLimit).IsRequired();
		});

		modelBuilder.Entity<DkpEvent>(entity =>
		{
			entity.HasKey(x => x.Id);
			entity.Property(x => x.AggregateType).HasMaxLength(64).IsRequired();
			entity.Property(x => x.EventType).HasMaxLength(128).IsRequired();
			entity.Property(x => x.Payload).HasColumnType("jsonb").IsRequired();
			entity.HasIndex(x => new { x.AggregateType, x.AggregateId, x.Sequence }).IsUnique();
			entity.HasIndex(x => x.CorrelationId).IsUnique();
			entity.HasIndex(x => new { x.UserId, x.OccurredAtUtc });
		});

		modelBuilder.Entity<DkpBalanceProjection>(entity =>
		{
			entity.HasKey(x => x.UserId);
			entity.Property(x => x.Balance).IsRequired();
		});

		modelBuilder.Entity<ShopPurchaseProjection>(entity =>
		{
			entity.HasKey(x => x.PurchaseId);
			entity.HasIndex(x => new { x.UserId, x.ShopItemId, x.CancelledAtUtc });
			entity.HasOne<ShopItem>().WithMany().HasForeignKey(x => x.ShopItemId).OnDelete(DeleteBehavior.Restrict);
		});
	}
}
