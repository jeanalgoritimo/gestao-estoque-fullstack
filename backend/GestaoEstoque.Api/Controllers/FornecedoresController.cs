using GestaoEstoque.Domain.Entities;
using GestaoEstoque.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestaoEstoque.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/fornecedores")]
public class FornecedoresController(GestaoEstoqueDbContext db) : ControllerBase
{
    public record FornecedorRequest(string Nome, string? Contato, string? Email, string? Telefone);
    public record FornecedorResponse(int Id, string Nome, string? Contato, string? Email, string? Telefone, bool Ativo);
    private static FornecedorResponse Map(Fornecedor f) => new(f.Id, f.Nome, f.Contato, f.Email, f.Telefone, f.Ativo);

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct) =>
        Ok((await db.Fornecedores.AsNoTracking().OrderBy(f => f.Nome).ToListAsync(ct)).Select(Map));

    [HttpPost]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Criar(FornecedorRequest request, CancellationToken ct)
    {
        try
        {
            var fornecedor = new Fornecedor(request.Nome, request.Contato, request.Email, request.Telefone);
            if (await db.Fornecedores.AnyAsync(f => f.NomeNormalizado == fornecedor.NomeNormalizado, ct))
                return Conflict(new { erro = "Fornecedor já cadastrado." });
            db.Fornecedores.Add(fornecedor);
            await db.SaveChangesAsync(ct);
            return Created($"/api/fornecedores/{fornecedor.Id}", Map(fornecedor));
        }
        catch (ArgumentException ex) { return BadRequest(new { erro = ex.Message }); }
        catch (DbUpdateException) { return Conflict(new { erro = "Fornecedor já cadastrado." }); }
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Alterar(int id, FornecedorRequest request, CancellationToken ct)
    {
        var fornecedor = await db.Fornecedores.FindAsync([id], ct);
        if (fornecedor is null) return NotFound();
        try
        {
            fornecedor.Alterar(request.Nome, request.Contato, request.Email, request.Telefone);
            if (await db.Fornecedores.AnyAsync(f => f.Id != id && f.NomeNormalizado == fornecedor.NomeNormalizado, ct))
                return Conflict(new { erro = "Fornecedor já cadastrado." });
            await db.SaveChangesAsync(ct);
            return Ok(Map(fornecedor));
        }
        catch (ArgumentException ex) { return BadRequest(new { erro = ex.Message }); }
        catch (DbUpdateException) { return Conflict(new { erro = "Fornecedor já cadastrado." }); }
    }

    [HttpPatch("{id:int}/ativo")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> DefinirAtivo(int id, [FromBody] bool ativo, CancellationToken ct)
    {
        var fornecedor = await db.Fornecedores.FindAsync([id], ct);
        if (fornecedor is null) return NotFound();
        if (!ativo && await db.Produtos.AnyAsync(p => p.FornecedorId == id && p.Ativo, ct))
            return Conflict(new { erro = "Desvincule os produtos ativos antes de desativar o fornecedor." });
        if (ativo) fornecedor.Ativar(); else fornecedor.Desativar();
        await db.SaveChangesAsync(ct);
        return Ok(Map(fornecedor));
    }
}
