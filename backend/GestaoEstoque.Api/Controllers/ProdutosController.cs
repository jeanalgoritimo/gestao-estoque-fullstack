using GestaoEstoque.Application.Abstractions;
using GestaoEstoque.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using GestaoEstoque.Infrastructure.Persistence;
using GestaoEstoque.Api.Security;

namespace GestaoEstoque.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/produtos")]
public class ProdutosController(IProdutoRepository repository, GestaoEstoqueDbContext db) : ControllerBase
{
    public record ProdutoRequest(string Nome, int CategoriaId, decimal Preco, int EstoqueMinimo);
    public record MovimentoRequest(TipoMovimento Tipo, int Quantidade, string? Observacao);
    public record ProdutoResponse(int Id, string Nome, int CategoriaId, string Categoria, decimal Preco,
        int Estoque, int EstoqueMinimo, bool Ativo, bool EstoqueBaixo);

    private static ProdutoResponse Map(Produto p, string? nomeCategoria = null) => new(p.Id, p.Nome, p.CategoriaId, nomeCategoria ?? p.CategoriaProduto?.Nome ?? "",
        p.Preco, p.Estoque, p.EstoqueMinimo, p.Ativo, p.EstaComEstoqueBaixo());

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
    [Authorize(Policy = Permissoes.GerenciarProdutos)]
    public async Task<ActionResult<ProdutoResponse>> Criar(ProdutoRequest request, CancellationToken ct)
    {
        var categoria = await db.CategoriasProduto.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CategoriaId && c.Ativo, ct);
        if (categoria is null) return BadRequest(new { erro = "Selecione uma categoria ativa." });
        try
        {
            var produto = await repository.AdicionarAsync(
                new Produto(request.Nome, request.CategoriaId, request.Preco, request.EstoqueMinimo), ct);
            return CreatedAtAction(nameof(Obter), new { id = produto.Id }, Map(produto, categoria.Nome));
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
        try
        {
            produto.AlterarNome(request.Nome);
            produto.AlterarCategoria(request.CategoriaId);
            produto.AlterarPreco(request.Preco);
            produto.AlterarEstoqueMinimo(request.EstoqueMinimo);
            await repository.SalvarAsync(ct);
            return Ok(Map(produto, categoria.Nome));
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
        produto.Desativar();
        try { await repository.SalvarAsync(ct); return NoContent(); }
        catch (DbUpdateConcurrencyException) { return Conflict(new { erro = "Produto alterado por outra operação." }); }
    }

    [HttpGet("{id:int}/movimentos")]
    public async Task<IActionResult> Movimentos(int id, CancellationToken ct)
    {
        if (await repository.ObterPorIdAsync(id, ct) is null) return NotFound();
        return Ok(await repository.ListarMovimentosAsync(id, ct));
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
            var movimento = new MovimentoEstoque(id, request.Tipo, request.Quantidade, request.Observacao);
            if (request.Tipo == TipoMovimento.Entrada) produto.RegistrarEntrada(request.Quantidade);
            else produto.RegistrarSaida(request.Quantidade);
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
