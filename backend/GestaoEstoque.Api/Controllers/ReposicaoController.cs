using GestaoEstoque.Domain.Entities;
using GestaoEstoque.Infrastructure.Persistence;
using GestaoEstoque.Infrastructure.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace GestaoEstoque.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/reposicao")]
public class ReposicaoController(GestaoEstoqueDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Consultar(CancellationToken ct)
    {
        var linhas = await ReposicaoQueries.Consultar(db).ToListAsync(ct);
        return Ok(linhas.Select(l => new { l.ProdutoId, l.Nome, l.Categoria, l.FornecedorId, l.Fornecedor,
            l.FornecedorAtivo, l.Estoque, l.Reservado, l.Disponivel, l.Minimo, l.ComprasPendentes, l.EmRascunhos,
            Sugerida = PlanejamentoReposicao.Sugerir(l.Minimo, l.Disponivel, l.ComprasPendentes) }));
    }
}
