namespace SabemiTec.Api.Models;

/// <summary>
/// Tabela de Status do Contrato: reflete o estado mais recente conhecido
/// de cada contrato/seguro apos o processamento dos eventos.
/// </summary>
public class StatusContrato
{
    public Guid Id { get; set; }

    public string IdContrato { get; set; } = string.Empty;

    public string UltimoIdTransacao { get; set; } = string.Empty;
    public decimal UltimoValor { get; set; }
    public DateTime UltimaDataPagamento { get; set; }

    public string StatusAtual { get; set; } = string.Empty;

    public DateTime AtualizadoEm { get; set; }
}
