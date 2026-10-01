using GestaoEstoque.Application.Abstractions;
using GestaoEstoque.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using GestaoEstoque.Infrastructure.Persistence;
using GestaoEstoque.Api.Security;
using System.Security.Claims;

namespace GestaoEstoque.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/produtos")]
public class ProdutosController(IProdutoRepository repository, GestaoEstoqueDbContext db) : ControllerBase
{
    public record ProdutoRequest(string Nome, int CategoriaId, decimal Preco, int EstoqueMinimo, int? FornecedorId, int? PosicaoEstoqueId = null);
    public record MovimentoRequest(TipoMovimento Tipo, int Quantidade, string? Observacao,
        DateTime? DataEfetivaUtc, string? DocumentoOrigem, string? Motivo, decimal? CustoUnitario);
    public record ProdutoResponse(int Id, string Nome, int CategoriaId, string Categoria, decimal Preco,
        int Estoque, int EstoqueMinimo, bool Ativo, bool EstoqueBaixo, int? FornecedorId, string? Fornecedor, int EstoqueReservado, int EstoqueDisponivel, int? PosicaoEstoqueId, string Localizacao);

    private static ProdutoResponse Map(Produto p, string? nomeCategoria = null) => new(p.Id, p.Nome, p.CategoriaId, nomeCategoria ?? p.CategoriaProduto?.Nome ?? "",
        p.Preco, p.Estoque, p.EstoqueMinimo, p.Ativo, p.EstaComEstoqueBaixo(), p.FornecedorId, p.Fornecedor?.Nome, p.EstoqueReservado, p.EstoqueDisponivel, p.PosicaoEstoqueId,
        p.PosicaoEstoque is { } pos ? $"{pos.Almoxarifado.Nome} · Corredor {pos.Corredor} · Estante {pos.Estante} · Prateleira {pos.Prateleira}" : "Sem localização");

    private async Task<ProdutoResponse> CarregarResponse(int id, CancellationToken ct) =>
        Map(await db.Produtos.AsNoTracking().Include(p => p.CategoriaProduto).Include(p => p.Fornecedor)
            .Include(p => p.PosicaoEstoque).ThenInclude(p => p!.Almoxarifado).SingleAsync(p => p.Id == id, ct));

    private async Task<bool> PosicaoValida(int? id, int? atual, CancellationToken ct) =>
        id is null || (id == atual && id > 0) || await db.PosicoesEstoque.AnyAsync(p => p.Id == id && p.Ativo && p.Almoxarifado.Ativo, ct);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProdutoResponse>>> Listar(CancellationToken ct)
    {
        var produtos = await repository.ListarAsync(ct);
        return Ok(produtos.Select(p => Map(p)).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProdutoResponse>> Obter(int id, CancellationToken ct)
    {
        var produto = await repository.ObterPorIdAsync(id, ct);
        return produto is null ? NotFound() : Ok(Map(produto));
    }

    [HttpPost]
    [Authorize(Policy = Permissoes.CadastrarProdutos)]
    public async Task<ActionResult<ProdutoResponse>> Criar(ProdutoRequest request, CancellationToken ct)
    {
        var categoria = await db.CategoriasProduto.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CategoriaId && c.Ativo, ct);
        if (categoria is null) return BadRequest(new { erro = "Selecione uma categoria ativa." });
        if (request.FornecedorId is not null && !await db.Fornecedores.AnyAsync(f => f.Id == request.FornecedorId && f.Ativo, ct))
            return BadRequest(new { erro = "Selecione um fornecedor ativo." });
        if (!await PosicaoValida(request.PosicaoEstoqueId, null, ct)) return BadRequest(new { erro = "Selecione uma posição e almoxarifado ativos." });
        try
        {
            var novoProduto = new Produto(request.Nome, request.CategoriaId, request.Preco, request.EstoqueMinimo);
            novoProduto.AlterarFornecedor(request.FornecedorId);
            novoProduto.AlterarPosicao(request.PosicaoEstoqueId);
            var produto = await repository.AdicionarAsync(
                novoProduto, ct);
            return CreatedAtAction(nameof(Obter), new { id = produto.Id }, await CarregarResponse(produto.Id, ct));
        }
        catch (ArgumentException ex) { return BadRequest(new { erro = ex.Message }); }
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = Permissoes.GerenciarProdutos)]
    public async Task<ActionResult<ProdutoResponse>> Alterar(int id, ProdutoRequest request, CancellationToken ct)
    {
        var produto = await repository.ObterPorIdAsync(id, ct);
        if (produto is null) return NotFound();
        if (!produto.Ativo) return Conflict(new { erro = "Produto inativo." });
        var categoria = await db.CategoriasProduto.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CategoriaId && c.Ativo, ct);
        if (categoria is null) return BadRequest(new { erro = "Selecione uma categoria ativa." });
        if (request.FornecedorId is not null && !await db.Fornecedores.AnyAsync(f => f.Id == request.FornecedorId && f.Ativo, ct))
            return BadRequest(new { erro = "Selecione um fornecedor ativo." });
        if (!await PosicaoValida(request.PosicaoEstoqueId, produto.PosicaoEstoqueId, ct)) return BadRequest(new { erro = "Selecione uma posição e almoxarifado ativos." });
        try
        {
            produto.AlterarPosicao(request.PosicaoEstoqueId);
            produto.AlterarFornecedor(request.FornecedorId);
            produto.AlterarNome(request.Nome);
            produto.AlterarCategoria(request.CategoriaId);
            produto.AlterarPreco(request.Preco);
            produto.AlterarEstoqueMinimo(request.EstoqueMinimo);
            await repository.SalvarAsync(ct);
            return Ok(await CarregarResponse(produto.Id, ct));
        }
        catch (ArgumentException ex) { return BadRequest(new { erro = ex.Message }); }
        catch (DbUpdateConcurrencyException) { return Conflict(new { erro = "Produto alterado por outra operação. Recarregue e tente novamente." }); }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = Permissoes.GerenciarProdutos)]
    public async Task<IActionResult> Desativar(int id, CancellationToken ct)
    {
        var produto = await repository.ObterPorIdAsync(id, ct);
        if (produto is null) return NotFound();
        try { produto.Desativar(); }
        catch (InvalidOperationException ex) { return Conflict(new { erro = ex.Message }); }
        try { await repository.SalvarAsync(ct); return NoContent(); }
        catch (DbUpdateConcurrencyException) { return Conflict(new { erro = "Produto alterado por outra operação." }); }
    }

    [HttpGet("{id:int}/movimentos")]
    public async Task<IActionResult> Movimentos(int id, CancellationToken ct)
    {
        if (await repository.ObterPorIdAsync(id, ct) is null) return NotFound();
        var movimentos = await repository.ListarMovimentosAsync(id, ct);
        return Ok(movimentos.Select(m => new { m.Id, m.ProdutoId, m.Tipo, m.Quantidade, m.DataUtc,
            m.DataEfetivaUtc, m.DocumentoOrigem, m.Motivo, m.UsuarioNome, m.CustoUnitario,
            m.SaldoApos, m.Observacao }));
    }

    [HttpPost("{id:int}/movimentos")]
    [Authorize(Policy = Permissoes.MovimentarEstoque)]
    public async Task<IActionResult> Movimentar(int id, MovimentoRequest request, CancellationToken ct)
    {
        var produto = await repository.ObterPorIdAsync(id, ct);
        if (produto is null) return NotFound();
        if (!produto.Ativo) return Conflict(new { erro = "Produto inativo." });
        try
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId)) return Unauthorized();
            if (request.Tipo == TipoMovimento.Entrada) produto.RegistrarEntrada(request.Quantidade);
            else if (request.Tipo == TipoMovimento.Saida) produto.RegistrarSaida(request.Quantidade);
            else return BadRequest(new { erro = "Tipo de movimento inválido." });
            var movimento = new MovimentoEstoque(id, request.Tipo, request.Quantidade, request.Observacao,
                request.DataEfetivaUtc ?? DateTime.UtcNow, request.DocumentoOrigem ?? "",
                request.Motivo ?? "", usuarioId, User.Identity?.Name ?? "", request.CustoUnitario, produto.Estoque);
            repository.AdicionarMovimento(movimento);
            // Um único SaveChanges grava o saldo e o histórico na mesma transação.
            await repository.SalvarAsync(ct);
            return CreatedAtAction(nameof(Movimentos), new { id }, movimento);
        }
        catch (ArgumentException ex) { return BadRequest(new { erro = ex.Message }); }
        catch (OverflowException) { return BadRequest(new { erro = "Quantidade excede o limite permitido." }); }
        catch (InvalidOperationException ex) { return Conflict(new { erro = ex.Message }); }
        catch (DbUpdateConcurrencyException) { return Conflict(new { erro = "Estoque alterado por outra operação. Recarregue e tente novamente." }); }
    }
}
