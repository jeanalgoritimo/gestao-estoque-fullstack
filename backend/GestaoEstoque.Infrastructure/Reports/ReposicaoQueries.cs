using GestaoEstoque.Domain.Entities;
using GestaoEstoque.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace GestaoEstoque.Infrastructure.Reports;

public record NecessidadeReposicao(int ProdutoId, string Nome, string Categoria, int? FornecedorId, string? Fornecedor,
    bool FornecedorAtivo, int Estoque, int Reservado, int Disponivel, int Minimo, long ComprasPendentes, long EmRascunhos, string Unidade);
public static class ReposicaoQueries
{
    public static IQueryable<NecessidadeReposicao> Consultar(GestaoEstoqueDbContext db) =>
        db.Produtos.AsNoTracking().Where(p => p.Ativo && p.Estoque - p.EstoqueReservado < p.EstoqueMinimo)
        .OrderBy(p => p.Nome).ThenBy(p => p.Id)
        .Select(p => new NecessidadeReposicao(p.Id, p.Nome, p.CategoriaProduto!.Nome, p.FornecedorId,
            p.Fornecedor == null ? null : p.Fornecedor.Nome, p.Fornecedor != null && p.Fornecedor.Ativo,
            p.Estoque, p.EstoqueReservado, p.Estoque - p.EstoqueReservado, p.EstoqueMinimo,
            (from i in db.ItensPedidoCompra join pedido in db.PedidosCompra on i.PedidoCompraId equals pedido.Id
             where i.ProdutoId == p.Id && (pedido.Situacao == SituacaoPedidoCompra.Aberto || pedido.Situacao == SituacaoPedidoCompra.ParcialmenteRecebido)
             select (long?)(i.Quantidade - i.QuantidadeRecebida)).Sum() ?? 0,
            (from i in db.ItensPedidoCompra join pedido in db.PedidosCompra on i.PedidoCompraId equals pedido.Id
             where i.ProdutoId == p.Id && pedido.Situacao == SituacaoPedidoCompra.Rascunho
             select (long?)i.Quantidade).Sum() ?? 0, p.UnidadeMedida.Sigla));
}
