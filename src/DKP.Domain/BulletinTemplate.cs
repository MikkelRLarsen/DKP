namespace DKP.Domain;

public sealed class BulletinTemplate
{
    private BulletinTemplate() { }

    public BulletinTemplate(string name, string title, string description, int dkpAmount, int maxCompletions,
        bool uniquePerUser, TimeSpan? defaultDeadlineOffset, DateTime now)
    {
        Id = Guid.NewGuid();
        Update(name, title, description, dkpAmount, maxCompletions, uniquePerUser, defaultDeadlineOffset, now);
        IsActive = true;
        CreatedAtUtc = now;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public int DkpAmount { get; private set; }
    public int MaxCompletions { get; private set; }
    public bool UniquePerUser { get; private set; }
    public TimeSpan? DefaultDeadlineOffset { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public void Update(string name, string title, string description, int dkpAmount, int maxCompletions,
        bool uniquePerUser, TimeSpan? defaultDeadlineOffset, DateTime now)
    {
        Name = Required(name, 128, nameof(name));
        Title = Required(title, 128, nameof(title));
        Description = Required(description, 2000, nameof(description));
        if (dkpAmount <= 0) throw new ArgumentException("DKP amount must be greater than zero.");
        if (maxCompletions <= 0) throw new ArgumentException("Maximum completions must be greater than zero.");
        if (defaultDeadlineOffset is TimeSpan offset && offset <= TimeSpan.Zero)
            throw new ArgumentException("Default deadline offset must be positive.");
        DkpAmount = dkpAmount;
        MaxCompletions = maxCompletions;
        UniquePerUser = uniquePerUser;
        DefaultDeadlineOffset = defaultDeadlineOffset;
        UpdatedAtUtc = now;
    }

    public void SetActive(bool active, DateTime now) { IsActive = active; UpdatedAtUtc = now; }

    private static string Required(string value, int max, string name)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException($"{name} is required.");
        var result = value.Trim();
        if (result.Length > max) throw new ArgumentException($"{name} must be at most {max} characters.");
        return result;
    }
}
