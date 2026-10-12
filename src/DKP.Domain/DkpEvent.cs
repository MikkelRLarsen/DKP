namespace DKP.Domain;

public sealed class DkpEvent
{
	private DkpEvent() { }
	public DkpEvent(string aggregateType, Guid aggregateId, long sequence, string eventType, Guid userId, Guid actorUserId, DateTime occurredAtUtc, Guid correlationId, string payload, int version = 1)
	{
		Version = version; Id = Guid.NewGuid(); AggregateType = aggregateType; AggregateId = aggregateId; Sequence = sequence; EventType = eventType; UserId = userId; ActorUserId = actorUserId; OccurredAtUtc = occurredAtUtc; CorrelationId = correlationId; Payload = payload;
	}
	public int Version { get; private set; }
	public Guid Id { get; private set; }
	public string AggregateType { get; private set; } = string.Empty;
	public Guid AggregateId { get; private set; }
	public long Sequence { get; private set; }
	public string EventType { get; private set; } = string.Empty;
	public Guid UserId { get; private set; }
	public Guid ActorUserId { get; private set; }
	public DateTime OccurredAtUtc { get; private set; }
	public Guid CorrelationId { get; private set; }
	public string Payload { get; private set; } = string.Empty;
}
