using GestaoEstoque.Infrastructure.Reports;
using GestaoEstoque.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace GestaoEstoque.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/relatorios/consumo")]
public class ConsumoController(GestaoEstoqueDbContext db) : ControllerBase
{
    public record Linha(int? CentroCustoId, string CentroCusto, int ProdutoId, string Produto, decimal Solicitado, decimal Entregue, decimal Devolvido, string Unidade)
    { public decimal ConsumoLiquido => Entregue - Devolvido; }
    [HttpGet]
    public async Task<IActionResult> Consultar([FromQuery] DateOnly inicio, [FromQuery] DateOnly fim,
        [FromQuery] int? centroCustoId, [FromQuery] bool semCentro, CancellationToken ct)
    {
        if (inicio == default || fim == default || fim < inicio || fim == DateOnly.MaxValue ||
            fim.DayNumber - inicio.DayNumber > 365 || centroCustoId <= 0 || (semCentro && centroCustoId is not null))
            return BadRequest(new { erro = "Informe um período de até 366 dias e um filtro de centro válido." });
        var zona = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
        var inicioUtc = TimeZoneInfo.ConvertTimeToUtc(inicio.ToDateTime(TimeOnly.MinValue), zona);
        var fimUtc = TimeZoneInfo.ConvertTimeToUtc(fim.AddDays(1).ToDateTime(TimeOnly.MinValue), zona);
        var solicitadas = await ConsumoQueries.Solicitadas(db, inicioUtc, fimUtc, centroCustoId, semCentro).ToListAsync(ct);
        var entregues = await ConsumoQueries.Entregues(db, inicioUtc, fimUtc, centroCustoId, semCentro).ToListAsync(ct);
        var devolvidas = await ConsumoQueries.Devolvidas(db, inicioUtc, fimUtc, centroCustoId, semCentro).ToListAsync(ct);
        var linhas = solicitadas.Select(x => new Linha(x.CentroCustoId, x.Centro, x.ProdutoId, x.Produto, x.Quantidade, 0, 0, x.Unidade))
            .Concat(entregues.Select(x => new Linha(x.CentroCustoId, x.Centro, x.ProdutoId, x.Produto, 0, x.Quantidade, 0, x.Unidade)))
            .Concat(devolvidas.Select(x => new Linha(x.CentroCustoId, x.Centro, x.ProdutoId, x.Produto, 0, 0, x.Quantidade, x.Unidade)))
            .GroupBy(x => new { x.CentroCustoId, x.ProdutoId })
            .Select(g => new Linha(g.Key.CentroCustoId, g.First().CentroCusto, g.Key.ProdutoId, g.First().Produto,
                g.Sum(x => x.Solicitado), g.Sum(x => x.Entregue), g.Sum(x => x.Devolvido), g.First().Unidade))
            .OrderBy(x => x.CentroCusto).ThenBy(x => x.Produto).ToList();
        return Ok(linhas);
    }
}
