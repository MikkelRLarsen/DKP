using DKP.Application.Persistence;
using DKP.Domain;
using DKP.Facade.Commands;
using DKP.Facade.Contracts;

namespace DKP.Application.SoftReserves;

public sealed class SoftReserveCommandService(
	IUserRepository users,
	ISoftReservePurchaseRepository purchases,
	IDkpTransactionRepository transactions,
	ISoftReserveSettings settings,
	TimeProvider timeProvider) : ISoftReserveCommands
{
	public async Task<SoftReservePurchaseDto> PurchaseAsync(
		string authenticatedDiscordId,
		PurchaseSoftReserveRequest request,
		CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(authenticatedDiscordId))
		{
			throw new UnauthorizedAccessException("An authenticated user is required.");
		}

		var user = await users.FindByDiscordIdAsync(authenticatedDiscordId, cancellationToken)
			?? throw new UnauthorizedAccessException("The authenticated user does not exist.");

		if (settings.DkpCost <= 0 || settings.MaxReserves <= 0)
		{
			throw new InvalidOperationException("Soft Reserve configuration is invalid.");
		}

		if (request.Quantity < 1)
		{
			throw new ArgumentException("Quantity must be greater than zero.", nameof(request.Quantity));
		}

		var activeQuantity = await purchases.GetActiveQuantityAsync(user.Id, cancellationToken);
		if (activeQuantity + request.Quantity > settings.MaxReserves)
		{
			throw new InvalidOperationException($"You can purchase at most {settings.MaxReserves} Soft Reserves.");
		}

		var totalCost = checked(settings.DkpCost * request.Quantity);
		var balance = await transactions.GetBalanceAsync(user.Id, cancellationToken);
		if (balance < totalCost)
		{
			throw new InvalidOperationException($"Insufficient DKP. The purchase costs {totalCost} DKP.");
		}

		var now = timeProvider.GetUtcNow().UtcDateTime;
		var purchase = new SoftReservePurchase(user.Id, request.Quantity, totalCost, now);
		var transaction = new DkpTransaction(
			user.Id,
			-totalCost,
			$"Soft Reserve purchase x{request.Quantity}",
			user.Id,
			now);

		await purchases.AddAsync(purchase, cancellationToken);
		await transactions.AddAsync(transaction, cancellationToken);
		await purchases.SaveChangesAsync(cancellationToken);

		return new SoftReservePurchaseDto(purchase.Id, purchase.Quantity, purchase.DkpCost, purchase.CreatedAtUtc, purchase.CancelledAtUtc);
	}

	public async Task<SoftReservePurchaseDto> CancelAsync(
		string authenticatedDiscordId,
		Guid purchaseId,
		CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(authenticatedDiscordId))
		{
			throw new UnauthorizedAccessException("An authenticated user is required.");
		}

		var user = await users.FindByDiscordIdAsync(authenticatedDiscordId, cancellationToken)
			?? throw new UnauthorizedAccessException("The authenticated user does not exist.");
		var purchase = await purchases.FindForUserAsync(purchaseId, user.Id, cancellationToken)
			?? throw new KeyNotFoundException("The Soft Reserve purchase does not exist.");

		var now = timeProvider.GetUtcNow().UtcDateTime;
		purchase.Cancel(now);
		var refund = new DkpTransaction(
			user.Id,
			purchase.DkpCost,
			$"Soft Reserve cancellation x{purchase.Quantity}",
			user.Id,
			now);
		await transactions.AddAsync(refund, cancellationToken);
		await purchases.SaveChangesAsync(cancellationToken);

		return new SoftReservePurchaseDto(purchase.Id, purchase.Quantity, purchase.DkpCost, purchase.CreatedAtUtc, purchase.CancelledAtUtc);
	}
}
