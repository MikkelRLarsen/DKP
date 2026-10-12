using DKP.Facade.Contracts;
namespace DKP.Facade.Queries;
public interface IGuildActivityQueries
{
    Task<GuildActivityPageDto> GetAsync(GuildActivityRequest request, CancellationToken cancellationToken = default);
}
