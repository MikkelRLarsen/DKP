using Discord.Interactions;
using Discord.WebSocket;

namespace DKP.DiscordBot;

[Group("characters", "Manage your DKP characters")]
public sealed class CharactersModule(DkpApiClient api) : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("list", "List your characters", runMode: RunMode.Async)]
    public async Task ListAsync()
    {
        await DeferAsync(ephemeral: true);
        var characters = await api.GetCharactersAsync(UserId(), CancellationToken.None);
        if (characters is null)
        {
            await ModifyOriginalResponseAsync(properties => properties.Content = "Your characters could not be loaded.");
            return;
        }

        var content = characters.Count == 0
            ? "You do not have any characters yet. Use `/characters create` to add one."
            : string.Join('\n', characters.Select(CharacterLine));
        await ModifyOriginalResponseAsync(properties => properties.Content = content);
    }

    [SlashCommand("create", "Create a character", runMode: RunMode.Async)]
    public Task CreateAsync() => RespondWithModalAsync<CharacterModal>("characters-create");

    [ModalInteraction("characters-create", true)]
    public async Task CreateModalAsync(CharacterModal modal)
    {
        await DeferAsync(ephemeral: true);
        var character = await api.CreateCharacterAsync(UserId(), new(modal.FirstName, modal.LastName), CancellationToken.None);
        await ModifyOriginalResponseAsync(properties => properties.Content = character is null
            ? "The character could not be created."
            : $"Created **{character.FirstName} {character.LastName}**.");
    }

    [SlashCommand("edit", "Edit a character using its ID from /characters list", runMode: RunMode.Async)]
    public async Task EditAsync(string characterId)
    {
        if (!Guid.TryParse(characterId, out var id))
        {
            await RespondAsync("Please provide a valid character ID from `/characters list`.", ephemeral: true);
            return;
        }

        var characters = await api.GetCharactersAsync(UserId(), CancellationToken.None);
        var character = characters?.SingleOrDefault(x => x.Id == id);
        if (character is null)
        {
            await RespondAsync("That character was not found in your character list.", ephemeral: true);
            return;
        }

        await RespondWithModalAsync($"characters-edit-{id:D}", new CharacterModal
        {
            FirstName = character.FirstName,
            LastName = character.LastName
        });
    }

    [ModalInteraction("characters-edit-*", true)]
    public async Task EditModalAsync(CharacterModal modal)
    {
        var customId = (Context.Interaction as SocketModal)?.Data.CustomId;
        var idText = customId?.Split('-', 3).LastOrDefault();
        if (!Guid.TryParse(idText, out var id))
        {
            await RespondAsync("The character edit request was invalid.", ephemeral: true);
            return;
        }

        await DeferAsync(ephemeral: true);
        var character = await api.UpdateCharacterAsync(UserId(), id, new(modal.FirstName, modal.LastName), CancellationToken.None);
        await ModifyOriginalResponseAsync(properties => properties.Content = character is null
            ? "The character could not be updated."
            : $"Updated **{character.FirstName} {character.LastName}**.");
    }

    [SlashCommand("delete", "Delete a character; run with confirm=true", runMode: RunMode.Async)]
    public async Task DeleteAsync(string characterId, bool confirm = false)
    {
        if (!confirm)
        {
            await RespondAsync("Deletion is permanent. Run this command again with `confirm: true`.", ephemeral: true);
            return;
        }
        if (!Guid.TryParse(characterId, out var id))
        {
            await RespondAsync("Please provide a valid character ID from `/characters list`.", ephemeral: true);
            return;
        }

        await DeferAsync(ephemeral: true);
        var deleted = await api.DeleteCharacterAsync(UserId(), id, CancellationToken.None);
        await ModifyOriginalResponseAsync(properties => properties.Content = deleted
            ? "Character deleted."
            : "That character was not found in your character list.");
    }

    [SlashCommand("main", "Set a character as your main; run with confirm=true", runMode: RunMode.Async)]
    public async Task MainAsync(string characterId, bool confirm = false)
    {
        if (!confirm)
        {
            await RespondAsync("Run this command again with `confirm: true` to set the main character.", ephemeral: true);
            return;
        }
        if (!Guid.TryParse(characterId, out var id))
        {
            await RespondAsync("Please provide a valid character ID from `/characters list`.", ephemeral: true);
            return;
        }

        await DeferAsync(ephemeral: true);
        var changed = await api.SetMainCharacterAsync(UserId(), id, CancellationToken.None);
        await ModifyOriginalResponseAsync(properties => properties.Content = changed
            ? "Main character updated."
            : "That character was not found in your character list.");
    }

    private string UserId() => Context.User.Id.ToString();

    private static string CharacterLine(BotCharacterDto character)
        => $"{(character.IsMain ? "⭐ " : string.Empty)}**{character.FirstName} {character.LastName}** — `{character.Id:D}`";
}

public sealed class CharacterModal : IModal
{
    public string Title => "Character";

    [InputLabel("First name")]
    [ModalTextInput("first-name", Discord.TextInputStyle.Short, "Your character's first name", maxLength: 64)]
    public string FirstName { get; set; } = string.Empty;

    [InputLabel("Last name")]
    [ModalTextInput("last-name", Discord.TextInputStyle.Short, "Your character's last name", maxLength: 64)]
    public string LastName { get; set; } = string.Empty;
}
