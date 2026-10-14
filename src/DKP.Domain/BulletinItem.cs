namespace DKP.Domain;

public sealed class BulletinItem
{
    private BulletinItem() { }

    public BulletinItem(string title, string description, int dkpAmount, int maxCompletions, bool uniquePerUser, DateTime? deadlineUtc, DateTime now)
    {
        Id = Guid.NewGuid();
        Update(title, description, dkpAmount, maxCompletions, uniquePerUser, deadlineUtc, now, 0);
        IsActive = true;
        CreatedAtUtc = now;
    }

    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public int DkpAmount { get; private set; }
    public int MaxCompletions { get; private set; }
    public bool UniquePerUser { get; private set; }
    public DateTime? DeadlineUtc { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsHidden { get; private set; }
    public bool IsAutoDeactivated { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public bool IsExpired(DateTime now) => DeadlineUtc is DateTime deadline && deadline <= now;
    public bool IsEffectivelyActive(DateTime now, int reservedQuantity) => IsActive && !IsExpired(now) && reservedQuantity < MaxCompletions;

    public void Update(string title, string description, int dkpAmount, int maxCompletions, bool uniquePerUser, DateTime? deadlineUtc, DateTime now, int reservedQuantity)
    {
        Title = Required(title, 128, nameof(title));
        Description = Required(description, 2000, nameof(description));
        if (dkpAmount <= 0) throw new ArgumentException("DKP amount must be greater than zero.");
        if (maxCompletions <= 0) throw new ArgumentException("Maximum completions must be greater than zero.");
        if (maxCompletions < reservedQuantity) throw new InvalidOperationException("Maximum completions cannot be lower than already reserved or approved claims.");
        if (deadlineUtc is DateTime deadline && deadline.Kind != DateTimeKind.Utc) throw new ArgumentException("Deadline must be UTC.");
        DkpAmount = dkpAmount; MaxCompletions = maxCompletions; UniquePerUser = uniquePerUser; DeadlineUtc = deadlineUtc;
        UpdatedAtUtc = now;
    }

    public void SetActive(bool active, DateTime now) { IsActive = active; IsAutoDeactivated = false; UpdatedAtUtc = now; }
    public void DeactivateAutomatically(DateTime now) { IsActive = false; IsAutoDeactivated = true; UpdatedAtUtc = now; }
    public void RestoreAfterDeadlineExtension(DateTime now, int reservedQuantity)
    {
        if (IsAutoDeactivated && !IsExpired(now) && reservedQuantity < MaxCompletions)
        {
            IsActive = true;
            IsAutoDeactivated = false;
            UpdatedAtUtc = now;
        }
    }
    public void SetHidden(bool hidden, DateTime now) { IsHidden = hidden; UpdatedAtUtc = now; }
    private static string Required(string value, int max, string name) { if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException($"{name} is required."); var result = value.Trim(); if (result.Length > max) throw new ArgumentException($"{name} must be at most {max} characters."); return result; }
}
