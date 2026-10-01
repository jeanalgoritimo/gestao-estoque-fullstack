using System.Data;
using GestaoEstoque.Domain.Entities;
using GestaoEstoque.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace GestaoEstoque.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/unidades-medida")]
public class UnidadesMedidaController(GestaoEstoqueDbContext db) : ControllerBase
{
    public record UnidadeRequest(string Sigla, string Nome);
    private static object Map(UnidadeMedida u) => new { u.Id, u.Sigla, u.Nome, u.Ativo, u.Sistema };
    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct) => Ok(await db.UnidadesMedida.AsNoTracking()
        .OrderBy(u => u.Sigla).Select(u => new { u.Id, u.Sigla, u.Nome, u.Ativo, u.Sistema }).ToListAsync(ct));
    [HttpPost]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Criar(UnidadeRequest request, CancellationToken ct)
    {
        try { var u = new UnidadeMedida(request.Sigla, request.Nome); db.UnidadesMedida.Add(u); await db.SaveChangesAsync(ct); return StatusCode(201, Map(u)); }
        catch (ArgumentException ex) { return BadRequest(new { erro = ex.Message }); }
        catch (DbUpdateException) { return Conflict(new { erro = "Já existe uma unidade com essa sigla." }); }
    }
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Editar(int id, UnidadeRequest request, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var u = await db.UnidadesMedida.FindAsync([id], ct); if (u is null) return NotFound();
        try
        {
            u.Alterar(request.Sigla, request.Nome, await db.Produtos.AnyAsync(p => p.UnidadeMedidaId == id, ct));
            await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return Ok(Map(u));
        }
        catch (ArgumentException ex) { return BadRequest(new { erro = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { erro = ex.Message }); }
        catch (DbUpdateException) { return Conflict(new { erro = "Não foi possível atualizar. Confira a sigla e recarregue." }); }
    }
    [HttpPatch("{id:int}/ativo")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> DefinirAtivo(int id, [FromBody] bool ativo, CancellationToken ct)
    {
        var u = await db.UnidadesMedida.FindAsync([id], ct); if (u is null) return NotFound();
        try { u.DefinirAtivo(ativo); await db.SaveChangesAsync(ct); return Ok(Map(u)); }
        catch (InvalidOperationException ex) { return Conflict(new { erro = ex.Message }); }
    }
}
