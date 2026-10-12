namespace DKP.Application.Persistence;
public interface ICommandUnitOfWork
{
    Task<T> ExecuteAsync<T>(IReadOnlyCollection<Guid> userIds, Func<Task<T>> action, CancellationToken cancellationToken = default);
}
