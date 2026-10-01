using GestaoEstoque.Domain.Entities;
using GestaoEstoque.Infrastructure.Persistence;
using GestaoEstoque.Infrastructure.Reports;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace GestaoEstoque.Domain.Tests;
public class RequisicoesConsultaTests
{
    [Fact]
    public void BuscaAplicaFiltrosAntesDePaginarMesmoAlemDosCemRegistros()
    {
        var registros = Enumerable.Range(0, 150).Select(i => new RequisicaoMaterial("Reposição", 1, "Solicitante", 1)).ToList();
        var encontrado = new RequisicaoMaterial("Manutenção urgente", 2, "Jean", 2);
        encontrado.AdicionarItem(1, 1); encontrado.Enviar(); registros.Add(encontrado);
        var consulta = RequisicoesConsulta.Filtrar(registros.AsQueryable(), new FiltroRequisicoes
        { Busca = "MANUTENÇÃO", Solicitante = "jean", CentroCustoId = 2, Situacao = SituacaoRequisicao.Pendente });
        Assert.Same(encontrado, consulta.Take(10).Single());
    }
    [Fact]
    public void FiltroSemCentroExcluiCentrosVinculados()
    {
        var legado = new RequisicaoMaterial("Finalidade", 1, "Pessoa");
        var novo = new RequisicaoMaterial("Finalidade", 1, "Pessoa", 2);
        var resultado = RequisicoesConsulta.Filtrar(new[] { legado, novo }.AsQueryable(), new FiltroRequisicoes { SemCentro = true });
        Assert.Same(legado, resultado.Single());
    }
    [Theory]
    [InlineData(0, 10)] [InlineData(1, 0)] [InlineData(1, 500)] [InlineData(1000001, 10)]
    public void PaginacaoInvalidaRejeitada(int pagina, int tamanho)
    {
        Assert.Throws<ArgumentException>(() => new FiltroRequisicoes { Pagina = pagina, TamanhoPagina = tamanho }.Validar());
    }
    [Fact]
    public void PeriodosEFiltrosInvalidosRejeitados()
    {
        Assert.Throws<ArgumentException>(() => new FiltroRequisicoes { CentroCustoId = 2, SemCentro = true }.Validar());
        Assert.Throws<ArgumentException>(() => new FiltroRequisicoes { Situacao = (SituacaoRequisicao)99 }.Validar());
        Assert.Throws<ArgumentException>(() => new FiltroRequisicoes { Inicio = new(2026, 10, 2), Fim = new(2026, 10, 1) }.Validar());
        Assert.Throws<ArgumentException>(() => new FiltroRequisicoes { Fim = DateOnly.MaxValue }.Validar());
    }
    [Fact]
    public void SqlTraduzFiltrosDatasEPaginacaoSemConexao()
    {
        using var db = new GestaoEstoqueDbContext(new DbContextOptionsBuilder<GestaoEstoqueDbContext>()
            .UseSqlServer("Server=localhost;Database=ScriptOnly;Integrated Security=true;TrustServerCertificate=true").Options);
        var query = RequisicoesConsulta.Filtrar(db.RequisicoesMaterial, new FiltroRequisicoes
        { Inicio = new(2026, 10, 1), Fim = new(2026, 10, 1), Busca = "#123", Solicitante = "Jean", SemCentro = true });
        var sql = query.OrderByDescending(r => r.Id).Skip(10).Take(10).ToQueryString();
        Assert.Contains("OFFSET", sql); Assert.Contains("FETCH NEXT", sql);
        Assert.Contains("2026-10-01T03:00:00", sql); Assert.Contains("2026-10-02T03:00:00", sql);
        Assert.Contains("[CentroCustoId] IS NULL", sql);
        Assert.Contains("[Id] =", sql);
    }
}
