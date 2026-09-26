using GestaoEstoque.Application.Abstractions;
using GestaoEstoque.Domain.Entities;
using GestaoEstoque.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestaoEstoque.Infrastructure.Repositories;

public class ProdutoRepository : IProdutoRepository
{
    private readonly GestaoEstoqueDbContext _context;

    public ProdutoRepository(GestaoEstoqueDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Produto>> ListarAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.Produtos
            .AsNoTracking()
            .Include(produto => produto.CategoriaProduto)
            .OrderBy(produto => produto.Nome)
            .ToListAsync(cancellationToken);
    }

    public async Task<Produto?> ObterPorIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Produtos
            .Include(produto => produto.CategoriaProduto)
            .FirstOrDefaultAsync(
                produto => produto.Id == id,
                cancellationToken);
    }

    public async Task<Produto> AdicionarAsync(
        Produto produto,
        CancellationToken cancellationToken = default)
    {
        await _context.Produtos.AddAsync(
            produto,
            cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return produto;
    }

    public async Task AtualizarAsync(
        Produto produto,
        CancellationToken cancellationToken = default)
    {
        _context.Produtos.Update(produto);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoverAsync(
        Produto produto,
        CancellationToken cancellationToken = default)
    {
        _context.Produtos.Remove(produto);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task SalvarAsync(CancellationToken cancellationToken = default) => _context.SaveChangesAsync(cancellationToken);

    public void AdicionarMovimento(MovimentoEstoque movimento) => _context.MovimentosEstoque.Add(movimento);

    public async Task<IReadOnlyList<MovimentoEstoque>> ListarMovimentosAsync(int produtoId, CancellationToken cancellationToken = default) =>
        await _context.MovimentosEstoque.AsNoTracking()
            .Where(m => m.ProdutoId == produtoId)
            .OrderByDescending(m => m.DataUtc).ThenByDescending(m => m.Id)
            .ToListAsync(cancellationToken);
}
