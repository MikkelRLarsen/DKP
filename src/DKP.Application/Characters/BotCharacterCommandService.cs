using DKP.Application.Persistence;
using DKP.Domain;
using DKP.Facade.Commands;
using DKP.Facade.Contracts;

namespace DKP.Application.Characters;

public sealed class BotCharacterCommandService(
    IUserRepository users,
    ICharacterRepository characters,
    ICommandUnitOfWork unitOfWork) : IBotCharacterCommands
{
    public Task<CharacterDto> CreateAsync(string discordId, CharacterInput input, CancellationToken ct = default)
        => unitOfWork.ExecuteAsync([], async () =>
        {
            var actor = await ActiveUserAsync(discordId, ct);
            CharacterRules.Validate(input);
            var character = new Character(actor.Id, input.FirstName.Trim(), input.LastName.Trim());
            await characters.AddAsync(character, ct);
            return ToDto(character);
        }, ct);

    public Task<CharacterDto?> UpdateAsync(string discordId, Guid characterId, CharacterInput input, CancellationToken ct = default)
        => unitOfWork.ExecuteAsync([], async () =>
        {
            var actor = await ActiveUserAsync(discordId, ct);
            CharacterRules.Validate(input);
            var character = await characters.FindForUserAsync(characterId, actor.Id, ct);
            if (character is null) return null;
            character.Update(input.FirstName.Trim(), input.LastName.Trim());
            return ToDto(character);
        }, ct);

    public Task<bool> DeleteAsync(string discordId, Guid characterId, CancellationToken ct = default)
        => unitOfWork.ExecuteAsync([], async () =>
        {
            var actor = await ActiveUserAsync(discordId, ct);
            var character = await characters.FindForUserAsync(characterId, actor.Id, ct);
            if (character is null) return false;
            characters.Remove(character);
            return true;
        }, ct);

    public Task<bool> SetMainCharacterAsync(string discordId, Guid characterId, CancellationToken ct = default)
        => unitOfWork.ExecuteAsync([], async () =>
        {
            var actor = await ActiveUserAsync(discordId, ct);
            var owned = await characters.FindAllForUserAsync(actor.Id, ct);
            var selected = owned.SingleOrDefault(x => x.Id == characterId);
            if (selected is null) return false;
            if (selected.IsMain) return true;
            foreach (var character in owned.Where(x => x.IsMain)) character.ClearMain();
            await characters.SaveChangesAsync(ct);
            selected.SetAsMain();
            return true;
        }, ct);

    private async Task<User> ActiveUserAsync(string discordId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(discordId)) throw new UnauthorizedAccessException("Login is required.");
        var user = await users.FindByDiscordIdAsync(discordId, ct);
        return user is null || user.IsBlocked
            ? throw new UnauthorizedAccessException("An active member is required.")
            : user;
    }

    private static CharacterDto ToDto(Character character) => new(character.Id, character.FirstName, character.LastName, character.IsMain);
}

internal static class CharacterRules
{
    public static void Validate(CharacterInput input)
    {
        if (string.IsNullOrWhiteSpace(input.FirstName) || input.FirstName.Trim().Length > 64 ||
            string.IsNullOrWhiteSpace(input.LastName) || input.LastName.Trim().Length > 64)
            throw new ArgumentException("First and last name are required (maximum 64 characters each).");
    }
}
