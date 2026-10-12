using DKP.Application.Characters;
using DKP.Infrastructure.Queries;
using Xunit;

namespace DKP.UnitTest;

public sealed class BotCharacterTests : DatabaseTest
{
    [Fact]
    public async Task Bot_can_manage_own_characters_and_switch_main()
    {
        var first = await As("member").BotCharacters.CreateAsync("member", new("First", "Character"));
        var second = await As("member").BotCharacters.CreateAsync("member", new("Second", "Character"));

        Assert.False(first.IsMain);
        Assert.False(second.IsMain);
        Assert.True(await As("member").BotCharacters.SetMainCharacterAsync("member", second.Id));

        var characters = await new BotCharacterQueries(Factory).GetAsync("member");
        Assert.NotNull(characters);
        Assert.Equal(second.Id, Assert.Single(characters!, x => x.IsMain).Id);
        Assert.False(characters!.Single(x => x.Id == first.Id).IsMain);
    }

    [Fact]
    public async Task Bot_cannot_update_another_users_character_or_use_blocked_account()
    {
        var character = await As("member").BotCharacters.CreateAsync("member", new("Owned", "Character"));

        Assert.Null(await As("other").BotCharacters.UpdateAsync("other", character.Id, new("No", "Access")));

        await As("officer").Blocks.BlockAsync(new(Member.Id, "Test block"));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            As("member").BotCharacters.CreateAsync("member", new("Blocked", "Character")));
    }
}
