using GestaoEstoque.Domain.Entities;
using GestaoEstoque.Infrastructure.Persistence;
using GestaoEstoque.Infrastructure.Reports;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;
namespace GestaoEstoque.Domain.Tests;

public class UnidadeMedidaTests
{
    [Fact]
    public void UnidadeNormalizaSiglaENomeEDesativacaoPreservaIdentidade()
    {
        var u = new UnidadeMedida("  cx  ", "  Caixa  ");
        Assert.Equal("CX", u.Sigla); Assert.Equal("Caixa", u.Nome);
        u.DefinirAtivo(false); Assert.False(u.Ativo); Assert.Equal("CX", u.Sigla);
        u.DefinirAtivo(true); Assert.True(u.Ativo);
    }
    [Theory]
    [InlineData("", "Caixa")]
    [InlineData("1CX", "Caixa")]
    [InlineData("C X", "Caixa")]
    [InlineData("=CX", "Caixa")]
    [InlineData("ABCDEFGHIJK", "Caixa")]
    [InlineData("CX", " ")]
    public void UnidadeInvalidaNaoAlteraCadastro(string sigla, string nome)
    {
        var u = new UnidadeMedida("CX", "Caixa");
        Assert.Throws<ArgumentException>(() => u.Alterar(sigla, nome, false));
        Assert.Equal("CX", u.Sigla); Assert.Equal("Caixa", u.Nome);
    }
    [Fact]
    public void SiglaEmUsoNaoMudaMasNomePodeSerCorrigido()
    {
        var u = new UnidadeMedida("CX", "Caixa");
        Assert.Throws<InvalidOperationException>(() => u.Alterar("PCT", "Pacote", true));
        Assert.Equal("CX", u.Sigla); Assert.Equal("Caixa", u.Nome);
        u.Alterar(" cx ", "Caixa de material", true); Assert.Equal("Caixa de material", u.Nome);
        var livre = new UnidadeMedida("CX", "Caixa"); livre.Alterar("PCT", "Pacote", false); Assert.Equal("PCT", livre.Sigla);
    }
    [Fact]
    public void UnidadePadraoNaoDesativaNemTrocaSigla()
    {
        var u = new UnidadeMedida("UN", "Unidade", sistema: true);
        Assert.Throws<InvalidOperationException>(() => u.DefinirAtivo(false));
        Assert.Throws<InvalidOperationException>(() => u.Alterar("CX", "Caixa", false));
        Assert.True(u.Ativo); Assert.Equal("UN", u.Sigla);
    }
    [Fact]
    public void ProdutoSemOperacoesPodeSelecionarUnidadeSemMudarQuantidades()
    {
        var p = new Produto("Material", 1, 10m, 5); Assert.Equal(1, p.UnidadeMedidaId);
        p.AlterarUnidade(2); Assert.Equal(2, p.UnidadeMedidaId);
        Assert.Throws<ArgumentException>(() => p.AlterarUnidade(0));
        Assert.Equal(2, p.UnidadeMedidaId); Assert.Equal(0, p.Estoque); Assert.Equal(5, p.EstoqueMinimo);
        Assert.Equal(10m, p.Preco);
    }
    [Fact]
    public void ProdutoComSaldoOuHistoricoNaoReinterpretaUnidade()
    {
        var p = new Produto("Material", 1, 10m); p.AlterarUnidade(2); p.RegistrarEntrada(10); p.Reservar(3);
        Assert.Throws<InvalidOperationException>(() => p.AlterarUnidade(3));
        p.AlterarUnidade(2, possuiHistorico: true); Assert.Equal(2, p.UnidadeMedidaId);
        Assert.Equal(10, p.Estoque); Assert.Equal(3, p.EstoqueReservado);
        p.LiberarReserva(3); p.RegistrarSaida(10);
        Assert.Throws<InvalidOperationException>(() => p.AlterarUnidade(3, possuiHistorico: true));
        Assert.Equal(2, p.UnidadeMedidaId); Assert.Equal(0, p.Estoque);
    }
    private static GestaoEstoqueDbContext Contexto() => new(new DbContextOptionsBuilder<GestaoEstoqueDbContext>()
        .UseSqlServer("Server=localhost;Database=ScriptOnly;Integrated Security=true;TrustServerCertificate=true").Options);
    [Fact]
    public void MigrationVinculaLegadoAUnidadePadraoSemConverterQuantidades()
    {
        using var db = Contexto();
        var sql = db.GetService<IMigrator>().GenerateScript("20261001190000_StockLocations", "20261001194000_MeasurementUnits");
        Assert.Contains("CREATE TABLE [UnidadesMedida]", sql); Assert.Contains("N'UN'", sql);
        Assert.Contains("[UnidadeMedidaId] int NOT NULL DEFAULT 1", sql);
        Assert.Contains("REFERENCES [UnidadesMedida] ([Id])", sql); Assert.Contains("CREATE UNIQUE INDEX", sql);
        Assert.True(sql.IndexOf("INSERT INTO [UnidadesMedida]", StringComparison.Ordinal) < sql.IndexOf("ALTER TABLE [Produtos] ADD [UnidadeMedidaId]", StringComparison.Ordinal));
        Assert.DoesNotContain("DROP TABLE", sql); Assert.DoesNotContain("ALTER COLUMN [Quantidade]", sql);
        Assert.DoesNotContain("ALTER COLUMN [Estoque]", sql); Assert.DoesNotContain("ON DELETE CASCADE", sql);
    }
    [Fact]
    public void UnidadeObrigatoriaEUnicidadeDaSiglaConfiguradas()
    {
        using var db = Contexto();
        var unidade = db.Model.FindEntityType(typeof(UnidadeMedida))!;
        Assert.Equal("Sigla", Assert.Single(Assert.Single(unidade.GetIndexes().Where(i => i.IsUnique)).Properties).Name);
        var produto = db.Model.FindEntityType(typeof(Produto))!;
        Assert.False(produto.FindProperty("UnidadeMedidaId")!.IsNullable);
        Assert.Equal(DeleteBehavior.Restrict, Assert.Single(produto.GetForeignKeys().Where(f => f.PrincipalEntityType.ClrType == typeof(UnidadeMedida))).DeleteBehavior);
    }
    [Fact]
    public void RelatoriosTraduzemUnidadeParaSqlSemMisturarProdutos()
    {
        using var db = Contexto(); var inicio = new DateTime(2026, 10, 1, 3, 0, 0, DateTimeKind.Utc);
        var consultas = new[] { ReposicaoQueries.Consultar(db).ToQueryString(),
            ConsumoQueries.Solicitadas(db, inicio, inicio.AddDays(1), null, false).ToQueryString(),
            ConsumoQueries.Entregues(db, inicio, inicio.AddDays(1), null, false).ToQueryString(),
            ConsumoQueries.Devolvidas(db, inicio, inicio.AddDays(1), null, false).ToQueryString() };
        Assert.All(consultas, sql => { Assert.Contains("[UnidadesMedida]", sql); Assert.Contains("[Sigla]", sql); Assert.Contains("[ProdutoId]", sql); });
    }
}
