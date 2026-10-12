using DKP.Facade.Contracts;

namespace DKP.Facade.Queries;

public interface IGuildMemberQueries
{
	Task<IReadOnlyList<GuildMemberDto>> GetAllAsync(CancellationToken cancellationToken = default);
}
