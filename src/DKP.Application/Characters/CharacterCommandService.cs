using DKP.Application.Authentication;
using DKP.Application.Persistence;
using DKP.Domain;
using DKP.Facade.Commands;
using DKP.Facade.Contracts;
namespace DKP.Application.Characters;

public sealed class CharacterCommandService(CommandContext context, ICharacterRepository characters) : ICharacterCommands
{
    public Task<CharacterDto> CreateAsync(CharacterInput input, CancellationToken ct = default)
        => context.ExecuteAsync([], false, async actor =>
        {
            CharacterRules.Validate(input);
            var character = new Character(actor.Id, input.FirstName.Trim(), input.LastName.Trim());
            await characters.AddAsync(character, ct);
            return ToDto(character);
        }, ct);

    public Task<CharacterDto?> UpdateAsync(Guid characterId, CharacterInput input, CancellationToken ct = default)
        => context.ExecuteAsync<CharacterDto?>([], false, async actor =>
        {
            CharacterRules.Validate(input);
            var character = await characters.FindForUserAsync(characterId, actor.Id, ct);
            if (character is null) return null;
            character.Update(input.FirstName.Trim(), input.LastName.Trim());
            return ToDto(character);
        }, ct);

    public Task<bool> DeleteAsync(Guid characterId, CancellationToken ct = default)
        => context.ExecuteAsync([], false, async actor =>
        {
            var character = await characters.FindForUserAsync(characterId, actor.Id, ct);
            if (character is null) return false;
            characters.Remove(character);
            return true;
        }, ct);

    public Task<bool> SetMainCharacterAsync(Guid characterId, CancellationToken ct = default)
        => context.ExecuteAsync([], false, async actor =>
        {
            var owned = await characters.FindAllForUserAsync(actor.Id, ct);
            var selected = owned.SingleOrDefault(x => x.Id == characterId);
            if (selected is null) return false;
            if (selected.IsMain) return true;
            foreach (var character in owned.Where(x => x.IsMain)) character.ClearMain();
            // Flush the unique main-character index, but keep both writes in the same transaction.
            await characters.SaveChangesAsync(ct);
            selected.SetAsMain();
            return true;
        }, ct);

    private static CharacterDto ToDto(Character c) => new(c.Id, c.FirstName, c.LastName, c.IsMain);
}
