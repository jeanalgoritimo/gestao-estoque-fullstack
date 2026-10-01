using GestaoEstoque.Domain.Entities;
using GestaoEstoque.Infrastructure.Persistence;
using GestaoEstoque.Infrastructure.Reports;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;
namespace GestaoEstoque.Domain.Tests;

public class CentroCustoTests
{
    [Fact]
    public void CentroNormalizaNomeEPreservaNomeAoDesativar()
    {
        var c = new CentroCusto("  Manutenção  ");
        Assert.Equal("Manutenção", c.Nome); Assert.Equal("MANUTENÇÃO", c.NomeNormalizado);
        c.DefinirAtivo(false); Assert.False(c.Ativo); Assert.Equal("Manutenção", c.Nome);
        c.DefinirAtivo(true); Assert.True(c.Ativo);
        Assert.Throws<ArgumentException>(() => c.AlterarNome(" "));
        Assert.Throws<ArgumentException>(() => c.AlterarNome(new string('a', 101)));
    }
    [Fact]
    public void VinculoCentroSomenteNoRascunhoELegadoPodeFicarSemCentro()
    {
        var r = new RequisicaoMaterial("Finalidade", 1, "Solicitante");
        Assert.Null(r.CentroCustoId); r.AlterarCentroCusto(3); Assert.Equal(3, r.CentroCustoId);
        Assert.Throws<ArgumentException>(() => r.AlterarCentroCusto(0));
        r.AdicionarItem(1, 2); r.Enviar();
        Assert.Throws<InvalidOperationException>(() => r.AlterarCentroCusto(4));
        Assert.Equal(3, r.CentroCustoId);
    }
    private static GestaoEstoqueDbContext Contexto() => new(new DbContextOptionsBuilder<GestaoEstoqueDbContext>()
        .UseSqlServer("Server=localhost;Database=ScriptOnly;Integrated Security=true;TrustServerCertificate=true").Options);
    [Fact]
    public void MigrationPreservaRequisicoesExistentesComVinculoOpcional()
    {
        using var db = Contexto();
        var sql = db.GetService<IMigrator>().GenerateScript("20260930173000_MaterialRequisitions", "20261001143000_CostCenters");
        Assert.Contains("CREATE TABLE [CentrosCusto]", sql);
        Assert.Contains("[CentroCustoId] int NULL", sql);
        Assert.Contains("REFERENCES [CentrosCusto] ([Id])", sql);
        Assert.DoesNotContain("DROP TABLE", sql);
        Assert.DoesNotContain("[Id].[RequisicoesMaterial]", sql);
    }
    [Theory]
    [InlineData(null, false)] [InlineData(null, true)] [InlineData(1, false)]
    public void ConsultasConsumoTraduzemParaSqlSemConectar(int? centro, bool semCentro)
    {
        using var db = Contexto();
        var inicio = new DateTime(2026, 10, 1, 3, 0, 0, DateTimeKind.Utc); var fim = inicio.AddDays(31);
        var solicitadas = ConsumoQueries.Solicitadas(db, inicio, fim, centro, semCentro).ToQueryString();
        var entregues = ConsumoQueries.Entregues(db, inicio, fim, centro, semCentro).ToQueryString();
        Assert.Contains("GROUP BY", solicitadas); Assert.Contains("[CriadoUtc]", solicitadas);
        Assert.Contains("GROUP BY", entregues); Assert.Contains("[DataUtc]", entregues);
        Assert.Contains("bigint", entregues);
        Assert.DoesNotContain("[CriadoUtc]", entregues);
    }
}
