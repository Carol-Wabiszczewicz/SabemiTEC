namespace SabemiTec.Api.Services;

public interface IBackgroundTaskQueue
{
    void Enfileirar(Guid eventoLogId);
    IAsyncEnumerable<Guid> ConsumirAsync(CancellationToken cancellationToken);
}
