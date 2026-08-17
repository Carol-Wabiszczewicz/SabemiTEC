using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SabemiTec.Api.Data;
using SabemiTec.Api.Dtos;
using SabemiTec.Api.Models;
using SabemiTec.Api.Services;

namespace SabemiTec.Api.Controllers;

[ApiController]
[Route("webhooks")]
public class WebhooksController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IBackgroundTaskQueue _queue;
    private readonly IConfiguration _config;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(
        AppDbContext db,
        IBackgroundTaskQueue queue,
        IConfiguration config,
        ILogger<WebhooksController> logger)
    {
        _db = db;
        _queue = queue;
        _config = config;
        _logger = logger;
    }

    [HttpPost("pagamento")]
    public async Task<IActionResult> ReceberPagamento(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var rawBody = await reader.ReadToEndAsync(cancellationToken);

        var apiKeyEsperada = _config["Webhook:ApiKey"];
        var apiKeyRecebida = Request.Headers["X-Api-Key"].FirstOrDefault();

        if (string.IsNullOrEmpty(apiKeyEsperada) || apiKeyRecebida != apiKeyEsperada)
        {
            _logger.LogWarning("Requisicao de webhook rejeitada: ApiKey ausente ou invalida.");
            await RegistrarEventoInvalidoAsync(rawBody, "ApiKey ausente ou invalida.", cancellationToken);
            return Unauthorized(new { erro = "ApiKey ausente ou invalida." });
        }

        WebhookPagamentoRequest? payload;
        try
        {
            payload = JsonSerializer.Deserialize<WebhookPagamentoRequest>(rawBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (JsonException)
        {
            await RegistrarEventoInvalidoAsync(rawBody, "JSON malformado.", cancellationToken);
            return BadRequest(new { erro = "JSON malformado." });
        }

        if (payload is null || string.IsNullOrWhiteSpace(payload.IdTransacao))
        {
            await RegistrarEventoInvalidoAsync(rawBody, "Campo id_transacao e obrigatorio.", cancellationToken);
            return BadRequest(new { erro = "Campo id_transacao e obrigatorio." });
        }

        // Idempotencia: se a transacao ja foi recebida antes, nao reprocessa nem duplica o log.
        var jaExiste = await _db.EventosLog.AnyAsync(e => e.IdTransacao == payload.IdTransacao, cancellationToken);
        if (jaExiste)
        {
            _logger.LogInformation("Transacao {IdTransacao} ja recebida anteriormente. Ignorando reprocessamento.", payload.IdTransacao);
            return Ok(new { mensagem = "Evento ja recebido e processado anteriormente.", id_transacao = payload.IdTransacao });
        }

        var evento = new EventoLog
        {
            Id = Guid.NewGuid(),
            IdTransacao = payload.IdTransacao,
            IdContrato = payload.IdContrato,
            Valor = payload.Valor,
            DataPagamento = payload.DataPagamento,
            StatusRecebido = payload.Status,
            PayloadBruto = rawBody,
            StatusProcessamento = StatusProcessamento.Pendente,
            RecebidoEm = DateTime.UtcNow
        };

        _db.EventosLog.Add(evento);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Corrida entre duas notificacoes concorrentes com o mesmo id_transacao:
            // o indice unico no banco garante que apenas uma seja persistida.
            return Ok(new { mensagem = "Evento ja recebido e processado anteriormente.", id_transacao = payload.IdTransacao });
        }

        // Responde rapido ao banco; o processamento pesado acontece em background.
        _queue.Enfileirar(evento.Id);

        return Accepted(new { mensagem = "Evento recebido, processamento em andamento.", id_transacao = payload.IdTransacao });
    }

    private async Task RegistrarEventoInvalidoAsync(string rawBody, string mensagemErro, CancellationToken cancellationToken)
    {
        var evento = new EventoLog
        {
            Id = Guid.NewGuid(),
            IdTransacao = $"INVALIDO-{Guid.NewGuid()}",
            PayloadBruto = rawBody,
            StatusProcessamento = StatusProcessamento.Erro,
            MensagemErro = mensagemErro,
            RecebidoEm = DateTime.UtcNow,
            ProcessadoEm = DateTime.UtcNow
        };

        _db.EventosLog.Add(evento);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
