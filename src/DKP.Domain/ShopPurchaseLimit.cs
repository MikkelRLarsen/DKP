namespace DKP.Domain;

public sealed class ShopPurchaseLimit
{
    private ShopPurchaseLimit() { }

    public ShopPurchaseLimit(string sharedKey, int? maxPerUser, int? maxActivePerUser, int? maxPerDay, int? maxPerWeek, int? maxPerMonth, DateTime nowUtc)
    {
        Id = Guid.NewGuid();
        SharedKey = sharedKey;
        MaxPerUser = maxPerUser;
        MaxActivePerUser = maxActivePerUser;
        MaxPerDay = maxPerDay;
        MaxPerWeek = maxPerWeek;
        MaxPerMonth = maxPerMonth;
        IsActive = true;
        CreatedAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
    }

    public Guid Id { get; private set; }
    public string SharedKey { get; private set; } = string.Empty;
    public int? MaxPerUser { get; private set; }
    public int? MaxActivePerUser { get; private set; }
    public int? MaxPerDay { get; private set; }
    public int? MaxPerWeek { get; private set; }
    public int? MaxPerMonth { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public void Update(string sharedKey, int? maxPerUser, int? maxActivePerUser, int? maxPerDay, int? maxPerWeek, int? maxPerMonth, DateTime nowUtc)
    {
        SharedKey = sharedKey;
        MaxPerUser = maxPerUser;
        MaxActivePerUser = maxActivePerUser;
        MaxPerDay = maxPerDay;
        MaxPerWeek = maxPerWeek;
        MaxPerMonth = maxPerMonth;
        UpdatedAtUtc = nowUtc;
    }

    public void SetActive(bool active, DateTime nowUtc)
    {
        IsActive = active;
        UpdatedAtUtc = nowUtc;
    }
}
