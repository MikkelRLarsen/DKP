namespace DKP.Domain;

public sealed class AchievementDefinition
{
    private AchievementDefinition() { }
    public AchievementDefinition(string key, string name, string description, int dkpAmount, DateTime now)
    { Id = Guid.NewGuid(); Key = key; Name = name; Description = description; DkpAmount = dkpAmount; IsActive = true; CreatedAtUtc = now; UpdatedAtUtc = now; }
    public Guid Id { get; private set; }
    public string Key { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public int DkpAmount { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public void Update(string key, string name, string description, int amount, DateTime now) { Key = key; Name = name; Description = description; DkpAmount = amount; UpdatedAtUtc = now; }
    public void SetActive(bool active, DateTime now) { IsActive = active; UpdatedAtUtc = now; }
}
