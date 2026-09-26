using GestaoEstoque.Domain.Entities;
using GestaoEstoque.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GestaoEstoque.Api.Security;

namespace GestaoEstoque.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/categorias")]
public class CategoriasController(GestaoEstoqueDbContext db) : ControllerBase
{
    public record CategoriaRequest(string Nome);
    public record CategoriaResponse(int Id, string Nome, bool Ativo);
    private static CategoriaResponse Map(CategoriaProduto c) => new(c.Id, c.Nome, c.Ativo);

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct) =>
        Ok((await db.CategoriasProduto.AsNoTracking().OrderBy(c => c.Nome).ToListAsync(ct)).Select(Map));

    [HttpPost]
    [Authorize(Policy = Permissoes.GerenciarCategorias)]
    public async Task<IActionResult> Criar(CategoriaRequest request, CancellationToken ct)
    {
        try
        {
            var categoria = new CategoriaProduto(request.Nome);
            if (await db.CategoriasProduto.AnyAsync(c => c.NomeNormalizado == categoria.NomeNormalizado, ct))
                return Conflict(new { erro = "Essa categoria já está cadastrada." });
            db.CategoriasProduto.Add(categoria);
            await db.SaveChangesAsync(ct);
            return Created($"/api/categorias/{categoria.Id}", Map(categoria));
        }
        catch (ArgumentException ex) { return BadRequest(new { erro = ex.Message }); }
        catch (DbUpdateException) { return Conflict(new { erro = "Essa categoria já está cadastrada." }); }
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = Permissoes.GerenciarCategorias)]
    public async Task<IActionResult> Renomear(int id, CategoriaRequest request, CancellationToken ct)
    {
        var categoria = await db.CategoriasProduto.FindAsync([id], ct);
        if (categoria is null) return NotFound();
        if (!categoria.Ativo) return Conflict(new { erro = "Categoria inativa." });
        try
        {
            categoria.Renomear(request.Nome);
            if (await db.CategoriasProduto.AnyAsync(c => c.Id != id && c.NomeNormalizado == categoria.NomeNormalizado, ct))
                return Conflict(new { erro = "Essa categoria já está cadastrada." });
            await db.SaveChangesAsync(ct);
            return Ok(Map(categoria));
        }
        catch (ArgumentException ex) { return BadRequest(new { erro = ex.Message }); }
        catch (DbUpdateException) { return Conflict(new { erro = "Essa categoria já está cadastrada." }); }
    }

    [HttpPatch("{id:int}/ativo")]
    [Authorize(Policy = Permissoes.GerenciarCategorias)]
    public async Task<IActionResult> DefinirAtivo(int id, [FromBody] bool ativo, CancellationToken ct)
    {
        var categoria = await db.CategoriasProduto.FindAsync([id], ct);
        if (categoria is null) return NotFound();
        if (!ativo && await db.Produtos.AnyAsync(p => p.CategoriaId == id && p.Ativo, ct))
            return Conflict(new { erro = "Desative ou recategorize os produtos ativos antes de desativar a categoria." });
        if (ativo) categoria.Ativar(); else categoria.Desativar();
        await db.SaveChangesAsync(ct);
        return Ok(Map(categoria));
    }
}
