using System.Security.Claims;
using GestaoEstoque.Api.Security;
using GestaoEstoque.Domain.Entities;
using GestaoEstoque.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace GestaoEstoque.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/transferencias")]
public class TransferenciasController(GestaoEstoqueDbContext db) : ControllerBase
{
    public record TransferenciaRequest(int ProdutoId, int? OrigemId, int DestinoId, decimal Quantidade, string Motivo, string VersaoProduto);
    private static string Descricao(PosicaoEstoque? p) => p is null ? "Sem localização" :
        $"{p.Almoxarifado.Nome} · Corredor {p.Corredor} · Estante {p.Estante} · Prateleira {p.Prateleira}";

    [HttpGet("saldos/{produtoId:int}")]
    public async Task<IActionResult> Saldos(int produtoId, CancellationToken ct)
    {
        // Uma leitura consistente entre o total, a versão e os locais.
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var produto = await db.Produtos.AsNoTracking().Include(p => p.UnidadeMedida).SingleOrDefaultAsync(p => p.Id == produtoId, ct);
        if (produto is null) return NotFound();
        var saldos = await db.SaldosLocais.AsNoTracking().Include(s => s.PosicaoEstoque).ThenInclude(p => p!.Almoxarifado)
            .Where(s => s.ProdutoId == produtoId).OrderBy(s => s.PosicaoEstoqueId).ToListAsync(ct);
        var resultado = new { produto.Id, produto.Nome, Unidade = produto.UnidadeMedida.Sigla, produto.Estoque,
            produto.EstoqueReservado, produto.EstoqueDisponivel, VersaoProduto = Convert.ToBase64String(produto.Versao),
            Locais = saldos.Select(s => new { s.PosicaoEstoqueId, Local = Descricao(s.PosicaoEstoque), s.Quantidade, s.Reservado, s.Disponivel }) };
        await tx.CommitAsync(ct); return Ok(resultado);
    }

    [HttpGet]
    public async Task<IActionResult> Historico([FromQuery] int produtoId, [FromQuery] int pagina = 1, CancellationToken ct = default)
    {
        if (produtoId <= 0 || pagina < 1 || pagina > 100000) return BadRequest(new { erro = "Informe produto e página válidos." });
        var consulta = db.TransferenciasEstoque.AsNoTracking().Where(t => t.ProdutoId == produtoId);
        var total = await consulta.CountAsync(ct);
        var itens = await consulta.OrderByDescending(t => t.Id).Skip((pagina - 1) * 20).Take(20)
            .Select(t => new { t.Id, t.ProdutoId, t.OrigemId, t.DestinoId, t.OrigemDescricao, t.DestinoDescricao, t.Quantidade,
                t.DataUtc, t.UsuarioNome, t.Motivo }).ToListAsync(ct);
        return Ok(new { Itens = itens, Total = total, Pagina = pagina, TotalPaginas = Math.Max(1, (int)Math.Ceiling(total / 20d)) });
    }

    [HttpPost]
    [Authorize(Policy = Permissoes.MovimentarEstoque)]
    public async Task<IActionResult> Transferir(TransferenciaRequest request, CancellationToken ct)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId)) return Unauthorized();
        byte[] versao;
        try { versao = Convert.FromBase64String(request.VersaoProduto ?? ""); }
        catch (FormatException) { return BadRequest(new { erro = "Versão inválida. Recarregue os saldos." }); }
        if (versao.Length != 8) return BadRequest(new { erro = "Recarregue os saldos antes de transferir." });
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var produto = await db.Produtos.SingleOrDefaultAsync(p => p.Id == request.ProdutoId, ct);
        if (produto is null) return NotFound();
        if (!produto.Ativo) return Conflict(new { erro = "Produto inativo." });
        if (!produto.Versao.SequenceEqual(versao)) return Conflict(new { erro = "Os saldos mudaram. Recarregue antes de transferir." });
        var destino = await db.PosicoesEstoque.Include(p => p.Almoxarifado).SingleOrDefaultAsync(p => p.Id == request.DestinoId, ct);
        if (destino is null || !destino.Ativo || !destino.Almoxarifado.Ativo) return BadRequest(new { erro = "Selecione uma posição de destino ativa." });
        var saldos = await db.SaldosLocais.Include(s => s.PosicaoEstoque).ThenInclude(p => p!.Almoxarifado)
            .Where(s => s.ProdutoId == produto.Id).ToListAsync(ct);
        var origem = saldos.SingleOrDefault(s => s.PosicaoEstoqueId == request.OrigemId);
        if (origem is null) return BadRequest(new { erro = "Origem sem saldo cadastrado para esse produto." });
        try
        {
            if (saldos.Sum(s => s.Quantidade) != produto.Estoque || saldos.Sum(s => s.Reservado) != produto.EstoqueReservado)
                throw new InvalidOperationException("Saldos locais inconsistentes. Recarregue ou revise os dados.");
            var transferencia = new TransferenciaEstoque(produto.Id, request.OrigemId, request.DestinoId, request.Quantidade,
                usuarioId, User.Identity?.Name ?? "", request.Motivo, Descricao(origem.PosicaoEstoque), Descricao(destino));
            var saldoDestino = saldos.SingleOrDefault(s => s.PosicaoEstoqueId == destino.Id);
            if (saldoDestino is null) { saldoDestino = new SaldoLocal(produto.Id, destino.Id); db.SaldosLocais.Add(saldoDestino); }
            origem.TransferirPara(saldoDestino, request.Quantidade);
            // Serializa transferências com reservas e movimentações sem alterar o saldo total.
            db.Entry(produto).Property(p => p.Estoque).IsModified = true;
            db.TransferenciasEstoque.Add(transferencia);
            await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
            return StatusCode(201, new { transferencia.Id, transferencia.Quantidade, transferencia.DataUtc });
        }
        catch (ArgumentException ex) { return BadRequest(new { erro = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { erro = ex.Message }); }
        catch (DbUpdateConcurrencyException) { return Conflict(new { erro = "Estoque alterado por outra operação. Recarregue os saldos." }); }
    }
}
