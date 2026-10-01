using GestaoEstoque.Domain.Entities;
using GestaoEstoque.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace GestaoEstoque.Infrastructure.Reports;

public record QuantidadeCentro(int? CentroCustoId, string Centro, int ProdutoId, string Produto, long Quantidade);
public static class ConsumoQueries
{
    public static IQueryable<QuantidadeCentro> Solicitadas(GestaoEstoqueDbContext db, DateTime inicioUtc, DateTime fimUtc, int? centroCustoId, bool semCentro)
    {
        var requisicoes = db.RequisicoesMaterial.AsNoTracking()
            .Where(r => (!semCentro || r.CentroCustoId == null) &&
                (centroCustoId == null || r.CentroCustoId == centroCustoId));
        return (from i in db.ItensRequisicaoMaterial.AsNoTracking()
            join r in requisicoes on i.RequisicaoMaterialId equals r.Id
            where r.CriadoUtc >= inicioUtc && r.CriadoUtc < fimUtc
            group i by new { r.CentroCustoId, Centro = r.CentroCusto == null ? "Sem centro de custo" : r.CentroCusto.Nome,
                i.ProdutoId, Produto = i.Produto.Nome } into g
            select new { g.Key.CentroCustoId, g.Key.Centro, g.Key.ProdutoId, g.Key.Produto, Quantidade = g.Sum(i => (long)i.Quantidade) })
            .Select(x => new QuantidadeCentro(x.CentroCustoId, x.Centro, x.ProdutoId, x.Produto, x.Quantidade));
    }
    public static IQueryable<QuantidadeCentro> Entregues(GestaoEstoqueDbContext db, DateTime inicioUtc, DateTime fimUtc, int? centroCustoId, bool semCentro) =>
        Movimentadas(db, inicioUtc, fimUtc, centroCustoId, semCentro, TipoMovimento.Saida);
    public static IQueryable<QuantidadeCentro> Devolvidas(GestaoEstoqueDbContext db, DateTime inicioUtc, DateTime fimUtc, int? centroCustoId, bool semCentro) =>
        Movimentadas(db, inicioUtc, fimUtc, centroCustoId, semCentro, TipoMovimento.Entrada);
    private static IQueryable<QuantidadeCentro> Movimentadas(GestaoEstoqueDbContext db, DateTime inicioUtc, DateTime fimUtc, int? centroCustoId, bool semCentro, TipoMovimento tipo)
    {
        var requisicoes = db.RequisicoesMaterial.AsNoTracking()
            .Where(r => (!semCentro || r.CentroCustoId == null) &&
                (centroCustoId == null || r.CentroCustoId == centroCustoId));
        return (from m in db.MovimentosEstoque.AsNoTracking()
            join r in requisicoes on m.RequisicaoMaterialId equals (long?)r.Id
            join p in db.Produtos.AsNoTracking() on m.ProdutoId equals p.Id
            where m.Tipo == tipo && (tipo == TipoMovimento.Saida || m.DevolucaoId != null) && m.DataUtc >= inicioUtc && m.DataUtc < fimUtc
            group m by new { r.CentroCustoId, Centro = r.CentroCusto == null ? "Sem centro de custo" : r.CentroCusto.Nome,
                m.ProdutoId, Produto = p.Nome } into g
            select new { g.Key.CentroCustoId, g.Key.Centro, g.Key.ProdutoId, g.Key.Produto, Quantidade = g.Sum(m => (long)m.Quantidade) })
            .Select(x => new QuantidadeCentro(x.CentroCustoId, x.Centro, x.ProdutoId, x.Produto, x.Quantidade));
    }
}
