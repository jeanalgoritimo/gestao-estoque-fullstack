using GestaoEstoque.Domain.Entities;
using GestaoEstoque.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace GestaoEstoque.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/centros-custo")]
public class CentrosCustoController(GestaoEstoqueDbContext db) : ControllerBase
{
    public record CentroRequest(string Nome);
    public record CentroResponse(int Id, string Nome, bool Ativo);
    private static CentroResponse Map(CentroCusto c) => new(c.Id, c.Nome, c.Ativo);
    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct) =>
        Ok((await db.CentrosCusto.AsNoTracking().OrderBy(c => c.Nome).ToListAsync(ct)).Select(Map));
    [HttpPost]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Criar(CentroRequest request, CancellationToken ct)
    {
        try
        {
            var c = new CentroCusto(request.Nome); db.CentrosCusto.Add(c); await db.SaveChangesAsync(ct);
            return StatusCode(201, Map(c));
        }
        catch (ArgumentException ex) { return BadRequest(new { erro = ex.Message }); }
        catch (DbUpdateException) { return Conflict(new { erro = "Já existe um centro de custo com esse nome." }); }
    }
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Editar(int id, CentroRequest request, CancellationToken ct)
    {
        var c = await db.CentrosCusto.FindAsync([id], ct); if (c is null) return NotFound();
        try { c.AlterarNome(request.Nome); await db.SaveChangesAsync(ct); return Ok(Map(c)); }
        catch (ArgumentException ex) { return BadRequest(new { erro = ex.Message }); }
        catch (DbUpdateException) { return Conflict(new { erro = "Já existe um centro de custo com esse nome." }); }
    }
    [HttpPatch("{id:int}/ativo")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> DefinirAtivo(int id, [FromBody] bool ativo, CancellationToken ct)
    {
        var c = await db.CentrosCusto.FindAsync([id], ct); if (c is null) return NotFound();
        c.DefinirAtivo(ativo); await db.SaveChangesAsync(ct); return Ok(Map(c));
    }
}
