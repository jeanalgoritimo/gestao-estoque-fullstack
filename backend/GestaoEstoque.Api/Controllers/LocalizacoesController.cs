using GestaoEstoque.Domain.Entities;
using GestaoEstoque.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace GestaoEstoque.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/localizacoes")]
public class LocalizacoesController(GestaoEstoqueDbContext db) : ControllerBase
{
    public record AlmoxarifadoRequest(string Nome);
    public record PosicaoRequest(int AlmoxarifadoId, string Corredor, string Estante, string Prateleira);
    private static object Map(Almoxarifado a) => new { a.Id, a.Nome, a.Ativo };
    [HttpGet("almoxarifados")]
    public async Task<IActionResult> Almoxarifados(CancellationToken ct) => Ok(await db.Almoxarifados.AsNoTracking()
        .OrderBy(a => a.Nome).Select(a => new { a.Id, a.Nome, a.Ativo }).ToListAsync(ct));
    [HttpGet("posicoes")]
    public async Task<IActionResult> Posicoes(CancellationToken ct) => Ok(await db.PosicoesEstoque.AsNoTracking()
        .OrderBy(p => p.Almoxarifado.Nome).ThenBy(p => p.Corredor).ThenBy(p => p.Estante).ThenBy(p => p.Prateleira)
        .Select(p => new { p.Id, p.AlmoxarifadoId, Almoxarifado = p.Almoxarifado.Nome, p.Corredor, p.Estante, p.Prateleira,
            p.Ativo, AlmoxarifadoAtivo = p.Almoxarifado.Ativo,
            Descricao = p.Almoxarifado.Nome + " · Corredor " + p.Corredor + " · Estante " + p.Estante + " · Prateleira " + p.Prateleira }).ToListAsync(ct));
    [HttpPost("almoxarifados")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> CriarAlmoxarifado(AlmoxarifadoRequest request, CancellationToken ct)
    {
        try { var a = new Almoxarifado(request.Nome); db.Almoxarifados.Add(a); await db.SaveChangesAsync(ct); return StatusCode(201, Map(a)); }
        catch (ArgumentException ex) { return BadRequest(new { erro = ex.Message }); }
        catch (DbUpdateException) { return Conflict(new { erro = "Já existe um almoxarifado com esse nome." }); }
    }
    [HttpPut("almoxarifados/{id:int}")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> EditarAlmoxarifado(int id, AlmoxarifadoRequest request, CancellationToken ct)
    {
        var a = await db.Almoxarifados.FindAsync([id], ct); if (a is null) return NotFound();
        try { a.AlterarNome(request.Nome); await db.SaveChangesAsync(ct); return Ok(Map(a)); }
        catch (ArgumentException ex) { return BadRequest(new { erro = ex.Message }); }
        catch (DbUpdateException) { return Conflict(new { erro = "Já existe um almoxarifado com esse nome." }); }
    }
    [HttpPatch("almoxarifados/{id:int}/ativo")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> AtivarAlmoxarifado(int id, [FromBody] bool ativo, CancellationToken ct)
    {
        var a = await db.Almoxarifados.FindAsync([id], ct); if (a is null) return NotFound();
        a.DefinirAtivo(ativo); await db.SaveChangesAsync(ct); return Ok();
    }
    [HttpPost("posicoes")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> CriarPosicao(PosicaoRequest request, CancellationToken ct)
    {
        if (!await db.Almoxarifados.AnyAsync(a => a.Id == request.AlmoxarifadoId && a.Ativo, ct)) return BadRequest(new { erro = "Selecione um almoxarifado ativo." });
        try
        {
            var p = new PosicaoEstoque(request.AlmoxarifadoId, request.Corredor, request.Estante, request.Prateleira);
            db.PosicoesEstoque.Add(p); await db.SaveChangesAsync(ct); return StatusCode(201, new { p.Id });
        }
        catch (ArgumentException ex) { return BadRequest(new { erro = ex.Message }); }
        catch (DbUpdateException) { return Conflict(new { erro = "Essa posição já existe no almoxarifado." }); }
    }
    [HttpPut("posicoes/{id:int}")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> EditarPosicao(int id, PosicaoRequest request, CancellationToken ct)
    {
        var p = await db.PosicoesEstoque.FindAsync([id], ct); if (p is null) return NotFound();
        if (request.AlmoxarifadoId != p.AlmoxarifadoId) return BadRequest(new { erro = "O almoxarifado da posição não pode ser alterado." });
        try { p.AlterarEndereco(request.Corredor, request.Estante, request.Prateleira); await db.SaveChangesAsync(ct); return Ok(); }
        catch (ArgumentException ex) { return BadRequest(new { erro = ex.Message }); }
        catch (DbUpdateException) { return Conflict(new { erro = "Essa posição já existe no almoxarifado." }); }
    }
    [HttpPatch("posicoes/{id:int}/ativo")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> AtivarPosicao(int id, [FromBody] bool ativo, CancellationToken ct)
    {
        var p = await db.PosicoesEstoque.FindAsync([id], ct); if (p is null) return NotFound();
        if (ativo && !await db.Almoxarifados.AnyAsync(a => a.Id == p.AlmoxarifadoId && a.Ativo, ct)) return Conflict(new { erro = "Reative o almoxarifado primeiro." });
        p.DefinirAtivo(ativo); await db.SaveChangesAsync(ct); return Ok();
    }
}
