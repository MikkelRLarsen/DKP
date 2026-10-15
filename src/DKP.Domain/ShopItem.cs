namespace DKP.Domain;

public sealed class ShopItem
{
	private ShopItem() { }

	public ShopItem(string key, string name, string description, int price, int maxPerUser, DateTime createdAtUtc, int? rollBonusValue = null,
		string? purchaseLimitKey = null, int? maxActivePerUser = null, int? maxPerDay = null, int? maxPerWeek = null, int? maxPerMonth = null)
	{
		Id = Guid.NewGuid(); Key = key; Name = name; Description = description;
		Price = price; MaxPerUser = maxPerUser; IsActive = true; PurchaseLimitKey = purchaseLimitKey;
		MaxActivePerUser = maxActivePerUser; MaxPerDay = maxPerDay; MaxPerWeek = maxPerWeek; MaxPerMonth = maxPerMonth;
		CreatedAtUtc = createdAtUtc; UpdatedAtUtc = createdAtUtc;
		RollBonusValue = rollBonusValue;
	}

	public Guid Id { get; private set; }
	public string Key { get; private set; } = string.Empty;
	public string Name { get; private set; } = string.Empty;
	public string Description { get; private set; } = string.Empty;
	public int Price { get; private set; }
	public int MaxPerUser { get; private set; }
	public bool IsActive { get; private set; }
	public bool IsHidden { get; private set; }
	public DateTime CreatedAtUtc { get; private set; }
	public DateTime UpdatedAtUtc { get; private set; }
	public int? RollBonusValue { get; private set; }
	public string? PurchaseLimitKey { get; private set; }
	public int? MaxActivePerUser { get; private set; }
	public int? MaxPerDay { get; private set; }
	public int? MaxPerWeek { get; private set; }
	public int? MaxPerMonth { get; private set; }
	public Guid? PurchaseLimitId { get; private set; }

	public void Update(string name, string description, int price, int maxPerUser, DateTime now,
		string? purchaseLimitKey = null, int? maxActivePerUser = null, int? maxPerDay = null, int? maxPerWeek = null, int? maxPerMonth = null)
	{
		Name = name; Description = description; Price = price; MaxPerUser = maxPerUser; PurchaseLimitKey = purchaseLimitKey;
		MaxActivePerUser = maxActivePerUser; MaxPerDay = maxPerDay; MaxPerWeek = maxPerWeek; MaxPerMonth = maxPerMonth; UpdatedAtUtc = now;
	}

	public void SetActive(bool active, DateTime now) { IsActive = active; UpdatedAtUtc = now; }
	public void SetHidden(bool hidden, DateTime now) { IsHidden = hidden; UpdatedAtUtc = now; }
	public void SetPurchaseLimit(Guid? purchaseLimitId, DateTime now) { PurchaseLimitId = purchaseLimitId; UpdatedAtUtc = now; }
}
