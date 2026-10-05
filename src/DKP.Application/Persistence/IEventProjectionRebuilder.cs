namespace DKP.Application.Persistence;
public interface IEventProjectionRebuilder
{
	Task RebuildAsync(CancellationToken cancellationToken = default);
}
