namespace DKP.Domain;

public sealed class GuildSetting
{
	private GuildSetting() { }
	public GuildSetting(int defaultReserveLimit) { Id = 1; DefaultReserveLimit = defaultReserveLimit; }
	public int Id { get; private set; }
	public int DefaultReserveLimit { get; private set; }
	public void UpdateReserveLimit(int value) { if (value < 0) throw new ArgumentOutOfRangeException(nameof(value)); DefaultReserveLimit = value; }
}
