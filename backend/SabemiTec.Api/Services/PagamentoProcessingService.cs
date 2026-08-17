using Microsoft.EntityFrameworkCore;
using SabemiTec.Api.Data;
using SabemiTec.Api.Models;

namespace SabemiTec.Api.Services;

/// <summary>
/// BackgroundService que consome a fila de eventos recebidos e executa a regra
/// de negocio "pesada" (simulada com um delay de 2s), fora do ciclo de request/response.
/// </summary>
public class PagamentoProcessingService : BackgroundService
{
    private readonly IBackgroundTaskQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PagamentoProcessingService> _logger;

    public PagamentoProcessingService(
        IBackgroundTaskQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<PagamentoProcessingService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var eventoLogId in _queue.ConsumirAsync(stoppingToken))
        {
            try
            {
                await ProcessarAsync(eventoLogId, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao processar evento {EventoLogId}", eventoLogId);
            }
        }
    }

    private async Task ProcessarAsync(Guid eventoLogId, CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var evento = await db.EventosLog.FirstOrDefaultAsync(e => e.Id == eventoLogId, stoppingToken);
        if (evento is null) return;

        // Simula regra de negocio pesada (ex: chamada a sistema de apolices/emprestimos).
        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

        var statusBancoValido = string.Equals(evento.StatusRecebido, "Sucesso", StringComparison.OrdinalIgnoreCase);

        if (!statusBancoValido || evento.Valor is null or <= 0 || string.IsNullOrWhiteSpace(evento.IdContrato))
        {
            evento.StatusProcessamento = StatusProcessamento.Erro;
            evento.MensagemErro = !statusBancoValido
                ? $"Banco reportou status '{evento.StatusRecebido}' para a transacao."
                : "Payload invalido: valor ou id_contrato ausente/invalido.";
            evento.ProcessadoEm = DateTime.UtcNow;
            await db.SaveChangesAsync(stoppingToken);
            return;
        }

        var contrato = await db.StatusContratos.FirstOrDefaultAsync(c => c.IdContrato == evento.IdContrato, stoppingToken);
        if (contrato is null)
        {
            contrato = new StatusContrato
            {
                Id = Guid.NewGuid(),
                IdContrato = evento.IdContrato!,
            };
            db.StatusContratos.Add(contrato);
        }

        contrato.UltimoIdTransacao = evento.IdTransacao;
        contrato.UltimoValor = evento.Valor!.Value;
        contrato.UltimaDataPagamento = evento.DataPagamento ?? DateTime.UtcNow;
        contrato.StatusAtual = "Sucesso";
        contrato.AtualizadoEm = DateTime.UtcNow;

        evento.StatusProcessamento = StatusProcessamento.Processado;
        evento.ProcessadoEm = DateTime.UtcNow;

        await db.SaveChangesAsync(stoppingToken);
    }
}
