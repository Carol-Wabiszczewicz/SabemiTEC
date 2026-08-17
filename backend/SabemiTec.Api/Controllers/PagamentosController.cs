using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SabemiTec.Api.Data;
using SabemiTec.Api.Models;

namespace SabemiTec.Api.Controllers;

[ApiController]
[Route("pagamentos")]
public class PagamentosController : ControllerBase
{
    private readonly AppDbContext _db;

    public PagamentosController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Lista os eventos recebidos (Log de Eventos Brutos) para o dashboard,
    /// com filtros opcionais por status de processamento e id do contrato.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? status,
        [FromQuery] string? idContrato,
        CancellationToken cancellationToken)
    {
        var query = _db.EventosLog.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<StatusProcessamento>(status, ignoreCase: true, out var statusEnum))
            {
                return BadRequest(new { erro = $"Status invalido: '{status}'." });
            }
            query = query.Where(e => e.StatusProcessamento == statusEnum);
        }

        if (!string.IsNullOrWhiteSpace(idContrato))
        {
            query = query.Where(e => e.IdContrato != null && e.IdContrato.Contains(idContrato));
        }

        var itens = await query
            .OrderByDescending(e => e.RecebidoEm)
            .Select(e => new
            {
                e.Id,
                e.IdTransacao,
                e.IdContrato,
                e.Valor,
                e.DataPagamento,
                e.StatusRecebido,
                StatusProcessamento = e.StatusProcessamento.ToString(),
                e.MensagemErro,
                e.RecebidoEm,
                e.ProcessadoEm
            })
            .Take(200)
            .ToListAsync(cancellationToken);

        return Ok(itens);
    }

    /// <summary>
    /// Lista o status atual de cada contrato (Status do Contrato).
    /// </summary>
    [HttpGet("contratos")]
    public async Task<IActionResult> ListarContratos(CancellationToken cancellationToken)
    {
        var itens = await _db.StatusContratos
            .AsNoTracking()
            .OrderByDescending(c => c.AtualizadoEm)
            .ToListAsync(cancellationToken);

        return Ok(itens);
    }
}
