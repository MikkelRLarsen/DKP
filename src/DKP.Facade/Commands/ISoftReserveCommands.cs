using DKP.Facade.Contracts;

namespace DKP.Facade.Commands;

public interface ISoftReserveCommands
{
	Task<SoftReservePurchaseDto> PurchaseAsync(
		string authenticatedDiscordId,
		PurchaseSoftReserveRequest request,
		CancellationToken cancellationToken = default);

	Task<SoftReservePurchaseDto> CancelAsync(
		string authenticatedDiscordId,
		Guid purchaseId,
		CancellationToken cancellationToken = default);
}
