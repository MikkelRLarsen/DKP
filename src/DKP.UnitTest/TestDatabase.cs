using DKP.Application.Authentication;
using DKP.Application.Characters;
using DKP.Application.DkpAwardRequests;
using DKP.Application.DkpTransactions;
using DKP.Application.LootReserve;
using DKP.Application.Persistence;
using DKP.Application.Presets;
using DKP.Application.Shop;
using DKP.Application.Users;
using DKP.Domain;
using DKP.Facade.Contracts;
using DKP.Infrastructure.Persistence;
using DKP.Infrastructure.Queries;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace DKP.UnitTest;

/// <summary>Every test gets its own migrated PostgreSQL database, never the application database.</summary>
public abstract class DatabaseTest : IAsyncLifetime
{
    private readonly string databaseName = "dkp_slice12a_" + Guid.NewGuid().ToString("N");
    private string adminConnection = "";
    private bool created;
    public string ConnectionString { get; private set; } = "";
    public TestDbFactory Factory { get; private set; } = null!;
    public User Officer { get; private set; } = null!;
    public User Member { get; private set; } = null!;
    public User Other { get; private set; } = null!;
    public static Guid SoftReserveId => Guid.Parse("00000000-0000-0000-0000-000000000008");
    public static Guid BonusId(int bonus) => Guid.Parse($"00000000-0000-0000-0000-{bonus:D12}");

    public async Task InitializeAsync()
    {
        adminConnection = Environment.GetEnvironmentVariable("DKP_TEST_ADMIN_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres";
        var builder = new NpgsqlConnectionStringBuilder(adminConnection) { Database = databaseName, Pooling = false };
        ConnectionString = builder.ConnectionString;
        await using var admin = new NpgsqlConnection(adminConnection);
        await admin.OpenAsync();
        await using (var command = new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\"", admin))
            await command.ExecuteNonQueryAsync();
        created = true;
        try
        {
            Factory = new(ConnectionString);
            await using var db = Factory.CreateDbContext();
            await db.Database.MigrateAsync();
            Officer = new("officer", "Officer Discord", null, Domain.UserRole.Officer, DateTime.UtcNow);
            Member = new("member", "Member Discord", null, Domain.UserRole.Member, DateTime.UtcNow);
            Other = new("other", "Other Discord", null, Domain.UserRole.Member, DateTime.UtcNow);
            db.Users.AddRange(Officer, Member, Other);
            await db.SaveChangesAsync();
        }
        catch { await DisposeAsync(); throw; }
    }

    public async Task DisposeAsync()
    {
        if (!created) return;
        // Only the exact generated database owned by this test can be removed.
        if (!System.Text.RegularExpressions.Regex.IsMatch(databaseName, "^dkp_slice12a_[a-f0-9]{32}$"))
            throw new InvalidOperationException("Refusing to drop an unrecognized database.");
        await using var admin = new NpgsqlConnection(adminConnection);
        await admin.OpenAsync();
        await using var command = new NpgsqlCommand($"DROP DATABASE \"{databaseName}\" WITH (FORCE)", admin);
        await command.ExecuteNonQueryAsync();
        created = false;
    }
    public TestRig As(string? discordId) => new(Factory, discordId);
    public async Task FundAsync(Guid userId, int amount = 100)
        => await As("officer").Dkp.AddAsync(new(userId, amount, "Starting DKP"));
    public async Task<int> BalanceAsync(Guid id)
    {
        await using var db = Factory.CreateDbContext();
        return LedgerReplayState.Replay(await db.DkpEvents.Where(x => x.UserId == id).ToArrayAsync()).Balance;
    }
}

public sealed class TestDbFactory(string connection) : IDbContextFactory<DkpDbContext>
{
    public DkpDbContext CreateDbContext() => new(new DbContextOptionsBuilder<DkpDbContext>().UseNpgsql(connection).Options);
    public Task<DkpDbContext> CreateDbContextAsync(CancellationToken ct = default) => Task.FromResult(CreateDbContext());
}
public sealed class TestIdentity(string? id) : ICurrentUser
{
    public Task<string?> GetDiscordIdAsync(CancellationToken cancellationToken = default) => Task.FromResult(id);
}
public sealed class BootstrapPolicy : IOfficerIdentityPolicy
{
    public bool IsOfficer(string discordId) => discordId == "officer";
}
public sealed class FixedTime : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => new(2026, 10, 6, 12, 0, 0, TimeSpan.Zero);
}
public sealed class TestRig
{
    public TestRig(TestDbFactory factory, string? id)
    {
        Factory = factory;
        Session = new(factory);
        Users = new(Session);
        Context = new(new TestIdentity(id), Users, Session);
        Ledger = new(Session);
        Dkp = new(Context, Ledger, new FixedTime());
        Shop = new(Context, new ShopRepository(Session), Ledger, new FixedTime());
        Presets = new(Context, new PresetRepository(Session), Ledger, new FixedTime());
        AwardRequests = new(Context, new DkpAwardRequestRepository(Session), new PresetRepository(Session), new AchievementRepository(Session), Ledger, new FixedTime());
        Characters = new(Context, new CharacterRepository(Session));
        Roles = new(Context, Users, new BootstrapPolicy());
        Blocks = new(Context, Users, new BootstrapPolicy(), new FixedTime());
        Provisioning = new(Users, new BootstrapPolicy(), new FixedTime(), Session);
        Queries = new(factory, new TestIdentity(id));
        Settings = new(Context, new GuildSettingsRepository(Session));
    }
    public CommandUnitOfWork Session { get; }
    public TestDbFactory Factory { get; }
    public UserRepository Users { get; }
    public CommandContext Context { get; }
    public EventLedgerRepository Ledger { get; }
    public DkpTransactionCommandService Dkp { get; }
    public ShopCommandService Shop { get; }
    public DkpPresetCommandService Presets { get; }
    public DkpAwardRequestCommandService AwardRequests { get; }
    public CharacterCommandService Characters { get; }
    public UserRoleCommandService Roles { get; }
    public UserBlockCommandService Blocks { get; }
    public UserProvisioningService Provisioning { get; }
    public LootReserveCommandService Settings { get; }
    public QuerySession Queries { get; }
}
