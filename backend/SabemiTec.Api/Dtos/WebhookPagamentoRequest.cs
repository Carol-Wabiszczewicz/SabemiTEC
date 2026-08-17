using System.Text.Json.Serialization;

namespace SabemiTec.Api.Dtos;

public class WebhookPagamentoRequest
{
    [JsonPropertyName("id_transacao")]
    public string? IdTransacao { get; set; }

    [JsonPropertyName("id_contrato")]
    public string? IdContrato { get; set; }

    [JsonPropertyName("valor")]
    public decimal? Valor { get; set; }

    [JsonPropertyName("data_pagamento")]
    public DateTime? DataPagamento { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }
}
