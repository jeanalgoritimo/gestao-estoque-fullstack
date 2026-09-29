using GestaoEstoque.Api.Security;
using GestaoEstoque.Domain.Entities;
using GestaoEstoque.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GestaoEstoque.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/inventarios")]
public class InventariosController(GestaoEstoqueDbContext db) : ControllerBase
{
    public record AbrirRequest(int ProdutoId);
    public record ContagemRequest(int Quantidade);
    public record ConfirmarRequest(string Motivo, int QuantidadeEsperada);
    public record InventarioResponse(long Id, int ProdutoId, string Produto, int SaldoInicial,
        int? QuantidadeContada, int? Diferenca, SituacaoInventario Situacao, DateTime AbertoUtc,
        string AbertoPorNome, DateTime? EncerradoUtc, string? EncerradoPorNome, string? Motivo);

    private static InventarioResponse Map(InventarioFisico i, string produto) =>
        new(i.Id, i.ProdutoId, produto, i.SaldoInicial, i.QuantidadeContada, i.Diferenca,
            i.Situacao, i.AbertoUtc, i.AbertoPorNome, i.EncerradoUtc, i.EncerradoPorNome, i.Motivo);

    private bool TryUsuario(out int id, out string nome)
    {
        nome = User.Identity?.Name ?? "";
        return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out id) && !string.IsNullOrWhiteSpace(nome);
    }

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct)
    {
        var inventarios = await (from i in db.InventariosFisicos.AsNoTracking()
            join p in db.Produtos.AsNoTracking() on i.ProdutoId equals p.Id
            orderby i.AbertoUtc descending, i.Id descending
            select new { Inventario = i, Produto = p.Nome }).Take(100).ToListAsync(ct);
        return Ok(inventarios.Select(x => Map(x.Inventario, x.Produto)));
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Obter(long id, CancellationToken ct)
    {
        var inventario = await db.InventariosFisicos.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id, ct);
        if (inventario is null) return NotFound();
        var produto = await db.Produtos.AsNoTracking().FirstAsync(p => p.Id == inventario.ProdutoId, ct);
        return Ok(Map(inventario, produto.Nome));
    }

    [HttpPost]
    [Authorize(Policy = Permissoes.MovimentarEstoque)]
    public async Task<IActionResult> Abrir(AbrirRequest request, CancellationToken ct)
    {
        if (!TryUsuario(out var usuarioId, out var nome)) return Unauthorized();
        var produto = await db.Produtos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == request.ProdutoId, ct);
        if (produto is null) return NotFound();
        if (!produto.Ativo) return Conflict(new { erro = "Produto inativo." });
        if (await db.InventariosFisicos.AnyAsync(i => i.ProdutoId == produto.Id && i.Situacao == SituacaoInventario.Aberto, ct))
            return Conflict(new { erro = "Já existe uma contagem aberta para este produto." });
        var inventario = new InventarioFisico(produto.Id, produto.Estoque, produto.Versao, usuarioId, nome);
        db.InventariosFisicos.Add(inventario);
        try
        {
            await db.SaveChangesAsync(ct);
            return CreatedAtAction(nameof(Obter), new { id = inventario.Id }, Map(inventario, produto.Nome));
        }
        catch (DbUpdateException) { return Conflict(new { erro = "Não foi possível abrir a contagem. Recarregue a lista e tente novamente." }); }
    }

    [HttpPut("{id:long}/contagem")]
    [Authorize(Policy = Permissoes.MovimentarEstoque)]
    public async Task<IActionResult> Registrar(long id, ContagemRequest request, CancellationToken ct)
    {
        var inventario = await db.InventariosFisicos.FindAsync([id], ct);
        if (inventario is null) return NotFound();
        try
        {
            inventario.RegistrarContagem(request.Quantidade);
            await db.SaveChangesAsync(ct);
            return Ok(Map(inventario, (await db.Produtos.AsNoTracking().FirstAsync(p => p.Id == inventario.ProdutoId, ct)).Nome));
        }
        catch (ArgumentOutOfRangeException) { return BadRequest(new { erro = "A quantidade contada não pode ser negativa." }); }
        catch (InvalidOperationException ex) { return Conflict(new { erro = ex.Message }); }
        catch (DbUpdateConcurrencyException) { return Conflict(new { erro = "Contagem alterada por outra operação. Recarregue." }); }
    }

    [HttpPost("{id:long}/confirmar")]
    [Authorize(Policy = Permissoes.MovimentarEstoque)]
    public async Task<IActionResult> Confirmar(long id, ConfirmarRequest request, CancellationToken ct)
    {
        if (!TryUsuario(out var usuarioId, out var nome)) return Unauthorized();
        var inventario = await db.InventariosFisicos.FindAsync([id], ct);
        if (inventario is null) return NotFound();
        if (inventario.QuantidadeContada != request.QuantidadeEsperada)
            return Conflict(new { erro = "A contagem registrada mudou. Recarregue antes de confirmar." });
        var produto = await db.Produtos.FindAsync([inventario.ProdutoId], ct);
        if (produto is null) return NotFound();
        if (!produto.Ativo || produto.Estoque != inventario.SaldoInicial ||
            !produto.Versao.SequenceEqual(inventario.VersaoProduto))
            return Conflict(new { erro = "O produto mudou após a abertura da contagem. Cancele e inicie outra contagem." });
        try
        {
            inventario.Confirmar(request.Motivo, usuarioId, nome);
            var diferenca = inventario.Diferenca!.Value;
            if (diferenca > 0) produto.RegistrarEntrada(diferenca);
            else if (diferenca < 0) produto.RegistrarSaida(-diferenca);
            // Atualiza o rowversion do produto mesmo quando a diferença é zero.
            db.Entry(produto).Property(p => p.Estoque).IsModified = true;
            if (diferenca != 0)
                db.MovimentosEstoque.Add(new MovimentoEstoque(produto.Id,
                    diferenca > 0 ? TipoMovimento.Entrada : TipoMovimento.Saida,
                    Math.Abs(diferenca), null, DateTime.UtcNow, $"INV-{inventario.Id}",
                    request.Motivo, usuarioId, nome, null, produto.Estoque));
            // SaveChanges faz o ajuste, o movimento e o fechamento na mesma transação.
            await db.SaveChangesAsync(ct);
            return Ok(Map(inventario, produto.Nome));
        }
        catch (ArgumentException ex) { return BadRequest(new { erro = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { erro = ex.Message }); }
        catch (DbUpdateConcurrencyException) { return Conflict(new { erro = "Saldo ou contagem alterados por outra operação. Recarregue." }); }
    }

    [HttpPost("{id:long}/cancelar")]
    [Authorize(Policy = Permissoes.MovimentarEstoque)]
    public async Task<IActionResult> Cancelar(long id, CancellationToken ct)
    {
        if (!TryUsuario(out var usuarioId, out var nome)) return Unauthorized();
        var inventario = await db.InventariosFisicos.FindAsync([id], ct);
        if (inventario is null) return NotFound();
        try
        {
            inventario.Cancelar(usuarioId, nome);
            await db.SaveChangesAsync(ct);
            return Ok();
        }
        catch (InvalidOperationException ex) { return Conflict(new { erro = ex.Message }); }
        catch (DbUpdateConcurrencyException) { return Conflict(new { erro = "Contagem alterada por outra operação. Recarregue." }); }
    }
}
