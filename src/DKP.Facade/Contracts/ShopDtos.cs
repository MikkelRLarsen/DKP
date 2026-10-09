namespace DKP.Facade.Contracts;

public sealed record ShopItemAchievementRequirementDto(Guid AchievementId, string AchievementName);
public sealed record ShopItemDto(Guid Id, string Key, string Name, string Description, int Price, int MaxPerUser, bool IsActive, IReadOnlyList<ShopItemAchievementRequirementDto> AchievementRequirements = null!);
public sealed record ShopPurchaseDto(Guid Id, Guid UserId, string UserName, string? MainCharacterName, Guid ShopItemId, string ItemName, int Quantity, int TotalDkpCost, DateTime CreatedAtUtc, DateTime? CancelledAtUtc, bool IsUsed = false, bool IsManuallyUsed = false)
{
	public bool IsCancelled => CancelledAtUtc is not null;
	public string Status => IsCancelled ? "Cancelled" : IsUsed ? "Used" : "Active";
}
public sealed record ShopItemInput(string Key, string Name, string Description, int Price, int MaxPerUser, IReadOnlyList<Guid>? AchievementIds = null);
public sealed record ShopPurchaseRequest(Guid ShopItemId, int Quantity);
public sealed record AdminShopPurchaseRequest(Guid ShopItemId, int Quantity, IReadOnlyList<Guid> TargetUserIds);
public sealed record ActivePurchaseItemDto(string Key, string Name, int Quantity, int MaxPerUser, int RemainingQuantity, int Price, int? RollBonusValue)
{
	public bool IsRollBonus => RollBonusValue is not null;
}
public sealed record ActivePurchaseOverviewDto(IReadOnlyList<ActivePurchaseItemDto> Items, int RollBonus);
