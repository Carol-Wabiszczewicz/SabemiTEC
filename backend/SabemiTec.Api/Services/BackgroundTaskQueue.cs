using System.Threading.Channels;

namespace SabemiTec.Api.Services;

/// <summary>
/// Fila em memoria (Channel) usada para desacoplar o recebimento do webhook
/// (que precisa responder rapido ao banco) do processamento pesado da regra de negocio.
/// </summary>
public class BackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>();

    public void Enfileirar(Guid eventoLogId)
    {
        _channel.Writer.TryWrite(eventoLogId);
    }

    public IAsyncEnumerable<Guid> ConsumirAsync(CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}
