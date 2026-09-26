using GestaoEstoque.Domain.Entities;
using GestaoEstoque.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestaoEstoque.Api.Controllers;

[ApiController]
[Authorize(Roles = Perfis.Administrador)]
[Route("api/perfis")]
public class PerfisController(GestaoEstoqueDbContext db) : ControllerBase
{
    public record PerfilRequest(string Nome, bool GerenciarProdutos, bool GerenciarCategorias, bool MovimentarEstoque);
    public record PerfilResponse(int Id, string Nome, bool Sistema, bool Ativo,
        bool GerenciarProdutos, bool GerenciarCategorias, bool MovimentarEstoque);
    private static PerfilResponse Map(PerfilAcesso p) => new(p.Id, p.Nome, p.Sistema, p.Ativo,
        p.GerenciarProdutos, p.GerenciarCategorias, p.MovimentarEstoque);

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct) =>
        Ok((await db.PerfisAcesso.AsNoTracking().OrderBy(p => p.Nome).ToListAsync(ct)).Select(Map));

    [HttpPost]
    public async Task<IActionResult> Criar(PerfilRequest request, CancellationToken ct)
    {
        try
        {
            var perfil = new PerfilAcesso(request.Nome, request.GerenciarProdutos, request.GerenciarCategorias, request.MovimentarEstoque);
            if (await db.PerfisAcesso.AnyAsync(p => p.NomeNormalizado == perfil.NomeNormalizado, ct))
                return Conflict(new { erro = "Perfil já cadastrado." });
            db.PerfisAcesso.Add(perfil);
            await db.SaveChangesAsync(ct);
            return Created($"/api/perfis/{perfil.Id}", Map(perfil));
        }
        catch (ArgumentException ex) { return BadRequest(new { erro = ex.Message }); }
        catch (DbUpdateException) { return Conflict(new { erro = "Perfil já cadastrado." }); }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Atualizar(int id, PerfilRequest request, CancellationToken ct)
    {
        var perfil = await db.PerfisAcesso.FindAsync([id], ct);
        if (perfil is null) return NotFound();
        if (perfil.Sistema || !perfil.Ativo) return Conflict(new { erro = "Perfil do sistema ou inativo não pode ser editado." });
        try
        {
            perfil.Atualizar(request.Nome, request.GerenciarProdutos, request.GerenciarCategorias, request.MovimentarEstoque);
            if (await db.PerfisAcesso.AnyAsync(p => p.Id != id && p.NomeNormalizado == perfil.NomeNormalizado, ct))
                return Conflict(new { erro = "Perfil já cadastrado." });
            await db.SaveChangesAsync(ct);
            return Ok(Map(perfil));
        }
        catch (ArgumentException ex) { return BadRequest(new { erro = ex.Message }); }
        catch (DbUpdateException) { return Conflict(new { erro = "Perfil já cadastrado." }); }
    }

    [HttpPatch("{id:int}/ativo")]
    public async Task<IActionResult> DefinirAtivo(int id, [FromBody] bool ativo, CancellationToken ct)
    {
        var perfil = await db.PerfisAcesso.FindAsync([id], ct);
        if (perfil is null) return NotFound();
        if (perfil.Sistema) return Conflict(new { erro = "Perfil do sistema não pode ser desativado." });
        if (!ativo && await db.Usuarios.AnyAsync(u => u.PerfilId == id && u.Ativo, ct))
            return Conflict(new { erro = "Desative ou altere os usuários ativos deste perfil antes de desativá-lo." });
        perfil.DefinirAtivo(ativo);
        await db.SaveChangesAsync(ct);
        return Ok(Map(perfil));
    }
}
