namespace DKP.Domain;

public sealed class ShopItem
{
	private ShopItem() { }

	public ShopItem(string key, string name, string description, int price, int maxPerUser, DateTime createdAtUtc, int? rollBonusValue = null)
	{
		Id = Guid.NewGuid(); Key = key; Name = name; Description = description;
		Price = price; MaxPerUser = maxPerUser; IsActive = true;
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
	public DateTime CreatedAtUtc { get; private set; }
	public DateTime UpdatedAtUtc { get; private set; }
	public int? RollBonusValue { get; private set; }

	public void Update(string name, string description, int price, int maxPerUser, DateTime now)
	{
		Name = name; Description = description; Price = price; MaxPerUser = maxPerUser; UpdatedAtUtc = now;
	}

	public void SetActive(bool active, DateTime now) { IsActive = active; UpdatedAtUtc = now; }
}
