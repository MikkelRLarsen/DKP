using DKP.Domain;
using Microsoft.EntityFrameworkCore;
namespace DKP.Infrastructure.Persistence;

public sealed class DkpDbContext(DbContextOptions<DkpDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Character> Characters => Set<Character>();
    public DbSet<ShopItem> ShopItems => Set<ShopItem>();
    public DbSet<DkpAwardPreset> DkpAwardPresets => Set<DkpAwardPreset>();
    public DbSet<DkpAwardRequest> DkpAwardRequests => Set<DkpAwardRequest>();
    public DbSet<GuildSetting> GuildSettings => Set<GuildSetting>();
    public DbSet<DkpEvent> DkpEvents => Set<DkpEvent>();

    private void GuardEvents()
    {
        if (ChangeTracker.Entries<DkpEvent>().Any(e => e.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Ledger events are append-only.");
    }
    public override int SaveChanges(bool acceptAllChangesOnSuccess) { GuardEvents(); return base.SaveChanges(acceptAllChangesOnSuccess); }
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    { GuardEvents(); return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken); }

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<User>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.DiscordId).IsUnique();
            e.Property(x => x.DiscordId).HasMaxLength(32);
            e.Property(x => x.DiscordName).HasMaxLength(128);
            e.Property(x => x.AvatarUrl).HasMaxLength(512);
            e.Property(x => x.Role).HasConversion<string>().HasMaxLength(32);
            e.Property(x => x.BlockReason).HasMaxLength(500);
            e.HasMany(x => x.Characters).WithOne(x => x.User).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
        model.Entity<Character>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.FirstName).HasMaxLength(64);
            e.Property(x => x.LastName).HasMaxLength(64);
            e.HasIndex(x => new { x.UserId, x.FirstName, x.LastName }).IsUnique();
            e.HasIndex(x => new { x.UserId, x.IsMain }).IsUnique().HasFilter("\"IsMain\" = TRUE");
        });
        model.Entity<ShopItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Key).IsUnique();
            e.Property(x => x.Key).HasMaxLength(64);
            e.Property(x => x.Name).HasMaxLength(128);
            e.Property(x => x.Description).HasMaxLength(500);
        });
        model.Entity<DkpAwardPreset>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Name).IsUnique();
            e.Property(x => x.Name).HasMaxLength(128);
            e.Property(x => x.Reason).HasMaxLength(500);
        });
        model.Entity<DkpAwardRequest>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            e.Property(x => x.Comment).HasMaxLength(500);
            e.Property(x => x.ReviewComment).HasMaxLength(500);
            e.Property(x => x.DkpEventIdsJson).HasColumnType("jsonb");
            e.HasIndex(x => new { x.UserId, x.PresetId, x.Status });
            e.HasIndex(x => new { x.UserId, x.PresetId }).IsUnique().HasFilter("\"Status\" = 'Pending'");
            e.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<DkpAwardPreset>().WithMany().HasForeignKey(x => x.PresetId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.ReviewedByUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<DkpEvent>().WithMany().HasForeignKey(x => x.DkpEventId).OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<DkpEvent>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.AggregateType).HasMaxLength(64);
            e.Property(x => x.EventType).HasMaxLength(128);
            e.Property(x => x.Payload).HasColumnType("jsonb");
            e.HasIndex(x => new { x.AggregateType, x.AggregateId, x.Sequence }).IsUnique();
            e.HasIndex(x => x.CorrelationId);
            e.HasIndex(x => new { x.UserId, x.OccurredAtUtc });
            e.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<GuildSetting>().HasKey(x => x.Id);
        model.Entity<GuildSetting>().HasData(new { Id = 1, DefaultReserveLimit = 0 });
        var seedTime = new DateTime(2026, 10, 5, 0, 0, 0, DateTimeKind.Utc);
        model.Entity<ShopItem>().HasData(new { Id = Guid.Parse("00000000-0000-0000-0000-000000000008"), Key = "soft-reserve", Name = "Soft Reserve", Description = "Additional Soft Reserve", Price = 10, MaxPerUser = 2, IsActive = true, CreatedAtUtc = seedTime, UpdatedAtUtc = seedTime, RollBonusValue = (int?)null });
        foreach (var (bonus, price) in new[] { (10, 10), (20, 30), (30, 60), (40, 120) })
            model.Entity<ShopItem>().HasData(new { Id = Guid.Parse($"00000000-0000-0000-0000-{bonus:D12}"), Key = $"roll-bonus-{bonus}", Name = $"RollBonus {bonus}", Description = $"Roll bonus +{bonus}", Price = price, MaxPerUser = 1, IsActive = true, CreatedAtUtc = seedTime, UpdatedAtUtc = seedTime, RollBonusValue = (int?)bonus });
    }
}
