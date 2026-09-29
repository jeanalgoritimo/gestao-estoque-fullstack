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
[Route("api/pedidos-compra")]
public class PedidosCompraController(GestaoEstoqueDbContext db) : ControllerBase
{
    public record ItemRequest(int ProdutoId, int Quantidade);
    public record CriarRequest(int FornecedorId, List<ItemRequest> Itens);
    public record ItemResponse(int ProdutoId, string Produto, int Quantidade);
    public record PedidoResponse(long Id, int FornecedorId, string Fornecedor, SituacaoPedidoCompra Situacao,
        DateTime CriadoUtc, string CriadoPorNome, DateTime? EncerradoUtc, string? EncerradoPorNome,
        List<ItemResponse> Itens);

    private static PedidoResponse Map(PedidoCompra p) => new(p.Id, p.FornecedorId, p.Fornecedor.Nome,
        p.Situacao, p.CriadoUtc, p.CriadoPorNome, p.EncerradoUtc, p.EncerradoPorNome,
        p.Itens.Select(i => new ItemResponse(i.ProdutoId, i.Produto.Nome, i.Quantidade)).ToList());
    private IQueryable<PedidoCompra> Consulta() => db.PedidosCompra.AsSplitQuery()
        .Include(p => p.Fornecedor).Include(p => p.Itens).ThenInclude(i => i.Produto);
    private bool Usuario(out int id, out string nome)
    {
        nome = User.Identity?.Name ?? "";
        return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out id) && !string.IsNullOrWhiteSpace(nome);
    }

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct) =>
        Ok((await Consulta().AsNoTracking().OrderByDescending(p => p.Id).Take(100).ToListAsync(ct)).Select(Map));

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Obter(long id, CancellationToken ct)
    {
        var pedido = await Consulta().AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
        return pedido is null ? NotFound() : Ok(Map(pedido));
    }

    [HttpPost]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Criar(CriarRequest request, CancellationToken ct)
    {
        if (!Usuario(out var usuarioId, out var nome)) return Unauthorized();
        if (request.Itens is not { Count: > 0 and <= 100 } ||
            request.Itens.Any(i => i.ProdutoId <= 0 || i.Quantidade <= 0) ||
            request.Itens.Select(i => i.ProdutoId).Distinct().Count() != request.Itens.Count)
            return BadRequest(new { erro = "Informe de 1 a 100 produtos distintos com quantidades positivas." });
        var fornecedor = await db.Fornecedores.FirstOrDefaultAsync(f => f.Id == request.FornecedorId && f.Ativo, ct);
        if (fornecedor is null) return BadRequest(new { erro = "Fornecedor ativo obrigatório." });
        var ids = request.Itens.Select(i => i.ProdutoId).ToList();
        var produtos = await db.Produtos.AsNoTracking().Where(p => ids.Contains(p.Id) && p.Ativo && p.FornecedorId == fornecedor.Id)
            .ToDictionaryAsync(p => p.Id, ct);
        if (produtos.Count != ids.Count)
            return BadRequest(new { erro = "Todos os produtos devem estar ativos e vinculados ao fornecedor escolhido." });
        var pedido = new PedidoCompra(fornecedor.Id, usuarioId, nome);
        foreach (var item in request.Itens) pedido.AdicionarItem(item.ProdutoId, item.Quantidade);
        db.PedidosCompra.Add(pedido);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Obter), new { id = pedido.Id }, new { pedido.Id });
    }

    [HttpPost("{id:long}/cancelar")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Cancelar(long id, CancellationToken ct)
    {
        if (!Usuario(out var usuarioId, out var nome)) return Unauthorized();
        var pedido = await db.PedidosCompra.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (pedido is null) return NotFound();
        try
        {
            pedido.Encerrar(SituacaoPedidoCompra.Cancelado, usuarioId, nome);
            await db.SaveChangesAsync(ct);
            return Ok();
        }
        catch (InvalidOperationException ex) { return Conflict(new { erro = ex.Message }); }
        catch (DbUpdateConcurrencyException) { return Conflict(new { erro = "Pedido alterado por outra operação. Recarregue." }); }
    }

    [HttpPost("{id:long}/receber")]
    [Authorize(Policy = Permissoes.MovimentarEstoque)]
    public async Task<IActionResult> Receber(long id, CancellationToken ct)
    {
        if (!Usuario(out var usuarioId, out var nome)) return Unauthorized();
        var pedido = await db.PedidosCompra.Include(p => p.Itens).FirstOrDefaultAsync(p => p.Id == id, ct);
        if (pedido is null) return NotFound();
        if (pedido.Situacao != SituacaoPedidoCompra.Aberto)
            return Conflict(new { erro = "Pedido já encerrado." });
        var ids = pedido.Itens.Select(i => i.ProdutoId).ToList();
        var produtos = await db.Produtos.Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);
        if (produtos.Count != ids.Count || pedido.Itens.Any(i => !produtos[i.ProdutoId].Ativo))
            return Conflict(new { erro = "Produto inativo ou não encontrado. O pedido não pode ser recebido." });
        try
        {
            pedido.Encerrar(SituacaoPedidoCompra.Recebido, usuarioId, nome);
            foreach (var item in pedido.Itens)
            {
                var produto = produtos[item.ProdutoId];
                produto.RegistrarEntrada(item.Quantidade);
                db.MovimentosEstoque.Add(new MovimentoEstoque(produto.Id, TipoMovimento.Entrada,
                    item.Quantidade, null, DateTime.UtcNow, $"PC-{pedido.Id}", "Recebimento de pedido de compra",
                    usuarioId, nome, null, produto.Estoque));
            }
            // EF Core grava fechamento, saldos e movimentos em uma única transação.
            await db.SaveChangesAsync(ct);
            return Ok(new { pedido.Id, pedido.Situacao });
        }
        catch (OverflowException) { return Conflict(new { erro = "Saldo excede o limite permitido." }); }
        catch (DbUpdateConcurrencyException) { return Conflict(new { erro = "Pedido ou saldo alterado por outra operação. Recarregue." }); }
    }
}
