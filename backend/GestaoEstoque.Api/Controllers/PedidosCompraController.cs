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
    public record ItemRecebimentoRequest(int ProdutoId, int Quantidade, decimal? CustoUnitario);
    public record CriarRequest(int FornecedorId, List<ItemRequest> Itens, bool Rascunho = false);
    public record EditarRequest(List<ItemRequest> Itens, string Versao);
    public record ConfirmarRequest(string Versao);
    public record ReceberRequest(List<ItemRecebimentoRequest> Itens);
    public record CancelarRequest(string Motivo);
    public record ItemResponse(int ProdutoId, string Produto, int Quantidade, int QuantidadeRecebida, string Unidade);
    public record LinhaRecebimentoResponse(long MovimentoId, int ProdutoId, string Produto, int Quantidade,
        decimal? CustoUnitario, decimal? ValorCompra, string Unidade);
    public record RecebimentoResponse(Guid Id, DateTime DataUtc, string UsuarioNome,
        List<LinhaRecebimentoResponse> Itens);
    public record PedidoResponse(long Id, int FornecedorId, string Fornecedor, SituacaoPedidoCompra Situacao,
        DateTime CriadoUtc, string CriadoPorNome, DateTime? EncerradoUtc, string? EncerradoPorNome, string? MotivoCancelamento,
        List<ItemResponse> Itens, string Versao);

    private static PedidoResponse Map(PedidoCompra p) => new(p.Id, p.FornecedorId, p.Fornecedor.Nome,
        p.Situacao, p.CriadoUtc, p.CriadoPorNome, p.EncerradoUtc, p.EncerradoPorNome, p.MotivoCancelamento,
        p.Itens.Select(i => new ItemResponse(i.ProdutoId, i.Produto.Nome, i.Quantidade, i.QuantidadeRecebida, i.Produto.UnidadeMedida.Sigla)).ToList(), Convert.ToBase64String(p.Versao));
    private IQueryable<PedidoCompra> Consulta() => db.PedidosCompra.AsSplitQuery()
        .Include(p => p.Fornecedor).Include(p => p.Itens).ThenInclude(i => i.Produto).ThenInclude(p => p.UnidadeMedida);
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

    [HttpGet("{id:long}/recebimentos")]
    public async Task<IActionResult> ListarRecebimentos(long id, CancellationToken ct)
    {
        if (!await db.PedidosCompra.AsNoTracking().AnyAsync(p => p.Id == id, ct)) return NotFound();
        var linhas = await (from m in db.MovimentosEstoque.AsNoTracking()
            join p in db.Produtos.AsNoTracking() on m.ProdutoId equals p.Id
            where m.PedidoCompraId == id && m.RecebimentoId != null
            orderby m.DataUtc descending, m.Id descending
            select new { Movimento = m, Produto = p.Nome, Unidade = p.UnidadeMedida.Sigla }).ToListAsync(ct);
        return Ok(linhas.GroupBy(x => x.Movimento.RecebimentoId!.Value)
            .Select(g => new RecebimentoResponse(g.Key, g.Max(x => x.Movimento.DataUtc),
                g.First().Movimento.UsuarioNome,
                g.Select(x => new LinhaRecebimentoResponse(x.Movimento.Id, x.Movimento.ProdutoId,
                    x.Produto, x.Movimento.Quantidade, x.Movimento.CustoUnitario,
                    x.Movimento.CustoUnitario * x.Movimento.Quantidade, x.Unidade)).ToList()))
            .OrderByDescending(r => r.DataUtc).ToList());
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
        var pedido = new PedidoCompra(fornecedor.Id, usuarioId, nome, request.Rascunho);
        foreach (var item in request.Itens) pedido.AdicionarItem(item.ProdutoId, item.Quantidade);
        db.PedidosCompra.Add(pedido);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Obter), new { id = pedido.Id }, new { pedido.Id });
    }

    private bool VersaoValida(PedidoCompra pedido, string versao)
    {
        try
        {
            var bytes = Convert.FromBase64String(versao ?? "");
            if (bytes.Length != 8 || !bytes.SequenceEqual(pedido.Versao)) return false;
            db.Entry(pedido).Property(p => p.Versao).OriginalValue = bytes;
            return true;
        }
        catch (FormatException) { return false; }
    }
    private async Task<bool> VinculosAtivos(PedidoCompra pedido, IEnumerable<int> ids, CancellationToken ct)
    {
        var produtos = ids.Distinct().ToArray();
        return await db.Fornecedores.AnyAsync(f => f.Id == pedido.FornecedorId && f.Ativo, ct) &&
            await db.Produtos.CountAsync(p => produtos.Contains(p.Id) && p.Ativo && p.FornecedorId == pedido.FornecedorId, ct) == produtos.Length;
    }
    [HttpPut("{id:long}/rascunho")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> EditarRascunho(long id, EditarRequest request, CancellationToken ct)
    {
        var pedido = await db.PedidosCompra.Include(p => p.Itens).FirstOrDefaultAsync(p => p.Id == id, ct);
        if (pedido is null) return NotFound();
        if (!VersaoValida(pedido, request.Versao)) return Conflict(new { erro = "Pedido alterado. Recarregue antes de editar." });
        if (request.Itens is not { Count: > 0 and <= 100 } ||
            !await VinculosAtivos(pedido, request.Itens.Select(i => i.ProdutoId), ct))
            return BadRequest(new { erro = "Informe itens ativos do fornecedor ativo do pedido." });
        try
        {
            pedido.AtualizarRascunho(request.Itens.Select(i => (i.ProdutoId, i.Quantidade)).ToArray());
            db.Entry(pedido).Property(p => p.Situacao).IsModified = true;
            await db.SaveChangesAsync(ct); return Ok();
        }
        catch (ArgumentException ex) { return BadRequest(new { erro = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { erro = ex.Message }); }
        catch (DbUpdateConcurrencyException) { return Conflict(new { erro = "Pedido alterado. Recarregue." }); }
    }
    [HttpPost("{id:long}/confirmar")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> ConfirmarRascunho(long id, ConfirmarRequest request, CancellationToken ct)
    {
        var pedido = await db.PedidosCompra.Include(p => p.Itens).FirstOrDefaultAsync(p => p.Id == id, ct);
        if (pedido is null) return NotFound();
        if (!VersaoValida(pedido, request.Versao)) return Conflict(new { erro = "Pedido alterado. Recarregue antes de confirmar." });
        if (!await VinculosAtivos(pedido, pedido.Itens.Select(i => i.ProdutoId), ct))
            return Conflict(new { erro = "Produtos e fornecedor precisam estar ativos e vinculados antes de confirmar." });
        try
        {
            pedido.ConfirmarRascunho(); await db.SaveChangesAsync(ct); return Ok();
        }
        catch (InvalidOperationException ex) { return Conflict(new { erro = ex.Message }); }
        catch (DbUpdateConcurrencyException) { return Conflict(new { erro = "Pedido alterado. Recarregue." }); }
    }

    [HttpPost("{id:long}/cancelar")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Cancelar(long id, CancelarRequest request, CancellationToken ct)
    {
        if (!Usuario(out var usuarioId, out var nome)) return Unauthorized();
        var pedido = await db.PedidosCompra.Include(p => p.Itens).FirstOrDefaultAsync(p => p.Id == id, ct);
        if (pedido is null) return NotFound();
        try
        {
            pedido.CancelarSaldo(usuarioId, nome, request.Motivo);
            await db.SaveChangesAsync(ct);
            return Ok();
        }
        catch (InvalidOperationException ex) { return Conflict(new { erro = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { erro = ex.Message }); }
        catch (DbUpdateConcurrencyException) { return Conflict(new { erro = "Pedido alterado por outra operação. Recarregue." }); }
    }

    [HttpPost("{id:long}/receber")]
    [Authorize(Policy = Permissoes.MovimentarEstoque)]
    public async Task<IActionResult> Receber(long id, ReceberRequest request, CancellationToken ct)
    {
        if (!Usuario(out var usuarioId, out var nome)) return Unauthorized();
        var pedido = await db.PedidosCompra.Include(p => p.Itens).FirstOrDefaultAsync(p => p.Id == id, ct);
        if (pedido is null) return NotFound();
        if (pedido.Situacao is not (SituacaoPedidoCompra.Aberto or SituacaoPedidoCompra.ParcialmenteRecebido))
            return Conflict(new { erro = "Pedido não permite recebimentos. Confirme a abertura do rascunho ou verifique se está encerrado." });
        if (request.Itens is not { Count: > 0 and <= 100 } ||
            request.Itens.Any(i => i.ProdutoId <= 0 || i.Quantidade <= 0 ||
                i.CustoUnitario is null or <= 0 or > 999999999999.9999m ||
                decimal.Round(i.CustoUnitario.Value, 4) != i.CustoUnitario.Value) ||
            request.Itens.Select(i => i.ProdutoId).Distinct().Count() != request.Itens.Count ||
            request.Itens.Any(i => !pedido.Itens.Any(item => item.ProdutoId == i.ProdutoId &&
                i.Quantidade <= item.Quantidade - item.QuantidadeRecebida)))
            return BadRequest(new { erro = "Informe quantidades positivas dentro do saldo pendente e custo unitário de compra válido (até 4 casas decimais)." });
        var ids = request.Itens.Select(i => i.ProdutoId).ToList();
        var produtos = await db.Produtos.Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);
        if (produtos.Count != ids.Count || request.Itens.Any(i => !produtos[i.ProdutoId].Ativo))
            return Conflict(new { erro = "Produto inativo ou não encontrado. O pedido não pode ser recebido." });
        try
        {
            var recebimentoId = Guid.NewGuid();
            foreach (var item in request.Itens)
            {
                pedido.RegistrarRecebimento(item.ProdutoId, item.Quantidade, usuarioId, nome);
                var produto = produtos[item.ProdutoId];
                produto.RegistrarEntrada(item.Quantidade);
                var movimento = new MovimentoEstoque(produto.Id, TipoMovimento.Entrada,
                    item.Quantidade, null, DateTime.UtcNow, $"PC-{pedido.Id}-R-{recebimentoId:N}",
                    "Recebimento de pedido de compra", usuarioId, nome, item.CustoUnitario, produto.Estoque);
                movimento.VincularRecebimento(pedido.Id, recebimentoId);
                db.MovimentosEstoque.Add(movimento);
            }
            // Atualiza o rowversion do cabeçalho também nos recebimentos parciais.
            db.Entry(pedido).Property(p => p.Situacao).IsModified = true;
            // EF Core grava saldos, quantidades recebidas e movimentos em uma única transação.
            await db.SaveChangesAsync(ct);
            return Ok(new { pedido.Id, pedido.Situacao, RecebimentoId = recebimentoId });
        }
        catch (OverflowException) { return Conflict(new { erro = "Saldo excede o limite permitido." }); }
        catch (ArgumentException ex) { return BadRequest(new { erro = ex.Message }); }
        catch (DbUpdateConcurrencyException) { return Conflict(new { erro = "Pedido ou saldo alterado por outra operação. Recarregue." }); }
    }
}
