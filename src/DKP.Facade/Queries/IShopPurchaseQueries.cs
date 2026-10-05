using DKP.Facade.Contracts;

namespace DKP.Facade.Queries;

public interface IShopPurchaseQueries
{
	Task<ActivePurchaseOverviewDto?> GetActiveOverviewAsync(string authenticatedDiscordId, CancellationToken cancellationToken = default);
}
