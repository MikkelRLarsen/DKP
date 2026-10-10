using DKP.Facade.Contracts;
using DKP.Infrastructure.Queries;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace DKP.UnitTest;

public sealed class AccountAndAuthorizationTests : DatabaseTest
{
    [Fact]
    public async Task Provisioning_updates_profile_without_duplicates_and_bootstrap_role_is_preserved()
    {
        var service = As(null).Provisioning;
        var first = await service.ProvisionAsync(new("new", "First name", null));
        var second = await service.ProvisionAsync(new("new", "Second name", "https://example.com/avatar"));
        Assert.Equal(first.Id, second.Id);
        Assert.Equal("Second name", second.DiscordName);
        Assert.Equal("https://example.com/avatar", second.AvatarUrl);
        var officer = await service.ProvisionAsync(new("officer", "Renamed officer", null));
        Assert.Equal(Domain.UserRole.Officer, officer.Role);
        await using var db = Factory.CreateDbContext();
        Assert.Equal(4, await db.Users.CountAsync());
    }

    [Fact]
    public async Task Characters_enforce_ownership_and_atomic_main_switch()
    {
        var own = As("member").Characters;
        var first = await own.CreateAsync(new("First", "Player"));
        var second = await own.CreateAsync(new("Main", "Player"));
        await own.SetMainCharacterAsync(first.Id);
        await own.SetMainCharacterAsync(second.Id);
        var other = As("other").Characters;
        Assert.Null(await other.UpdateAsync(first.Id, new("Hacked", "Player")));
        Assert.False(await other.SetMainCharacterAsync(first.Id));
        Assert.False(await other.DeleteAsync(first.Id));
        var dashboard = (await new AccountQueries(As("member").Queries).GetDashboardAsync())!;
        Assert.Equal(second.Id, dashboard.Characters[0].Id);
        Assert.Single(dashboard.Characters, x => x.IsMain);
        Assert.Equal(2, dashboard.Characters.Count);
        var guild = await new GuildMemberQueries(As("member").Queries).GetAllAsync();
        Assert.Equal(3, guild.Count);
        Assert.Empty(guild.Single(x => x.UserId == Other.Id).Characters);
        Assert.Equal($"{Member.DiscordName} / Main Player", (await new DkpQueries(As("officer").Queries).GetUsersAsync()).Single(x => x.Id == Member.Id).DisplayName);
    }

    [Fact]
    public async Task LootReserve_counts_only_active_SR_uses_settings_and_main_first()
    {
        await FundAsync(Member.Id);
        var member = As("member");
        var first = await member.Characters.CreateAsync(new("First", "Zebra"));
        var main = await member.Characters.CreateAsync(new("Main", "Alpha"));
        await member.Characters.SetMainCharacterAsync(main.Id);
        var purchase = Assert.Single(await member.Shop.PurchaseAsync(new(SoftReserveId, 2)));
        var admin = As("officer");
        var query = new LootReserveQueries(admin.Queries);
        var rows = await query.GetMembersAsync();
        var row = rows.Single(x => x.UserId == Member.Id);
        Assert.Equal(2, row.ExtraReserve);
        Assert.Equal(main.Id, row.Characters[0].Id);
        Assert.True(row.IsReady);
        Assert.False(rows.Single(x => x.UserId == Other.Id).IsReady);
        await member.Shop.CancelAsync(purchase.Id);
        Assert.Equal(0, (await query.GetMembersAsync()).Single(x => x.UserId == Member.Id).ExtraReserve);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("unknown")]
    [InlineData("member")]
    public async Task Non_officers_cannot_call_officer_commands_or_queries(string? identity)
    {
        var rig = As(identity);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => rig.Dkp.AddAsync(new(Member.Id, 1, "Attempt")));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => rig.Presets.CreateAsync(new("Preset", 1, "Reason", 1)));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => rig.Presets.ApplyAsync(Member.Id, Guid.NewGuid()));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => rig.Shop.CreateItemAsync(new("item", "Item", "", 1, 1)));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => rig.Shop.PurchaseForUsersAsync(new(SoftReserveId, 1, [Member.Id])));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => rig.Roles.SetRoleAsync(new(Other.Id, UserRole.Officer)));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => rig.Blocks.BlockAsync(new(Other.Id, null)));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => new LootReserveQueries(rig.Queries).GetMembersAsync());
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => new ShopQueries(rig.Queries).GetAllPurchasesAsync());
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => new UserAdministrationQueries(rig.Queries).GetAllAsync());
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => new DkpPresetQueries(rig.Queries).GetAllAsync());
    }

    [Fact]
    public async Task Anonymous_queries_are_rejected()
    {
        var query = As(null).Queries;
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => new AccountQueries(query).GetDashboardAsync());
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => new DkpQueries(query).GetHistoryAsync());
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => new GuildMemberQueries(query).GetAllAsync());
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => new GuildActivityQueries(query).GetAsync(new()));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => new PlayerDetailsQueries(query).GetAsync(Member.Id));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => new ShopQueries(query).GetActiveItemsAsync());
    }

    [Fact]
    public async Task Blocking_is_revalidated_for_existing_services_and_targets()
    {
        await FundAsync(Member.Id);
        var staleCircuit = As("member");
        await As("officer").Blocks.BlockAsync(new(Member.Id, "Blocked"));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => staleCircuit.Shop.PurchaseAsync(new(SoftReserveId, 1)));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => staleCircuit.Characters.CreateAsync(new("Blocked", "Player")));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => staleCircuit.Provisioning.ProvisionAsync(new("member", "Name", null)));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => new GuildActivityQueries(staleCircuit.Queries).GetAsync(new()));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => new DkpQueries(staleCircuit.Queries).GetHistoryAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(() => As("officer").Shop.PurchaseForUsersAsync(new(SoftReserveId, 1, [Member.Id])));
        await As("officer").Blocks.UnblockAsync(Member.Id);
        await staleCircuit.Shop.PurchaseAsync(new(SoftReserveId, 1));
    }

    [Fact]
    public async Task Role_changes_and_bootstrap_protection_are_enforced()
    {
        var admin = As("officer");
        await Assert.ThrowsAsync<InvalidOperationException>(() => admin.Blocks.BlockAsync(new(Officer.Id, null)));
        await Assert.ThrowsAsync<InvalidOperationException>(() => admin.Roles.SetRoleAsync(new(Officer.Id, UserRole.Member)));
        await admin.Roles.SetRoleAsync(new(Member.Id, UserRole.Officer));
        Assert.Equal(Domain.UserRole.Officer, (await As("member").Provisioning.ProvisionAsync(new("member", "Renamed", null))).Role);
        await As("member").Dkp.AddAsync(new(Other.Id, 1, "Promoted officer"));
        await admin.Roles.SetRoleAsync(new(Member.Id, UserRole.Member));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => As("member").Dkp.AddAsync(new(Other.Id, 1, "Former officer")));
    }

    [Fact]
    public async Task Member_cannot_cancel_another_members_purchase()
    {
        await FundAsync(Member.Id);
        var purchase = Assert.Single(await As("member").Shop.PurchaseAsync(new(SoftReserveId, 1)));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => As("other").Shop.CancelAsync(purchase.Id));
        Assert.Equal(90, await BalanceAsync(Member.Id));
    }
}
