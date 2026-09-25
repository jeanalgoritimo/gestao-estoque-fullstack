using GestaoEstoque.Domain.Entities;

namespace GestaoEstoque.Application.Abstractions;

public interface IProdutoRepository
{
    Task<IReadOnlyList<Produto>> ListarAsync(
        CancellationToken cancellationToken = default);

    Task<Produto?> ObterPorIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<Produto> AdicionarAsync(
        Produto produto,
        CancellationToken cancellationToken = default);

    Task AtualizarAsync(
        Produto produto,
        CancellationToken cancellationToken = default);

    Task RemoverAsync(
        Produto produto,
        CancellationToken cancellationToken = default);

    Task SalvarAsync(CancellationToken cancellationToken = default);
    void AdicionarMovimento(MovimentoEstoque movimento);
    Task<IReadOnlyList<MovimentoEstoque>> ListarMovimentosAsync(int produtoId, CancellationToken cancellationToken = default);
}
