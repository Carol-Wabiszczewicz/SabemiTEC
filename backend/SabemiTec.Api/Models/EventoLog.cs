namespace SabemiTec.Api.Models;

public enum StatusProcessamento
{
    Pendente,
    Processado,
    Erro,
    Duplicado
}

/// <summary>
/// Tabela de Log de Eventos Brutos: guarda cada notificacao recebida do banco,
/// exatamente como chegou, mais o resultado do processamento assincrono.
/// </summary>
public class EventoLog
{
    public Guid Id { get; set; }

    public string IdTransacao { get; set; } = string.Empty;
    public string? IdContrato { get; set; }
    public decimal? Valor { get; set; }
    public DateTime? DataPagamento { get; set; }
    public string? StatusRecebido { get; set; }

    public string PayloadBruto { get; set; } = string.Empty;

    public StatusProcessamento StatusProcessamento { get; set; } = StatusProcessamento.Pendente;
    public string? MensagemErro { get; set; }

    public DateTime RecebidoEm { get; set; }
    public DateTime? ProcessadoEm { get; set; }
}
