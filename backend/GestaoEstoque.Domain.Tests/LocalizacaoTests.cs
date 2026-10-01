using GestaoEstoque.Domain.Entities;
using GestaoEstoque.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;
namespace GestaoEstoque.Domain.Tests;

public class LocalizacaoTests
{
    [Fact]
    public void AlmoxarifadoNormalizaNomeEPreservaCadastroAoDesativar()
    {
        var a = new Almoxarifado("  Central  ");
        Assert.Equal("Central", a.Nome); Assert.Equal("CENTRAL", a.NomeNormalizado);
        a.DefinirAtivo(false); Assert.False(a.Ativo); Assert.Equal("Central", a.Nome);
        a.DefinirAtivo(true); a.AlterarNome("  Manutenção  ");
        Assert.True(a.Ativo); Assert.Equal("MANUTENÇÃO", a.NomeNormalizado);
        Assert.Throws<ArgumentException>(() => a.AlterarNome(" "));
        Assert.Throws<ArgumentException>(() => a.AlterarNome(new string('a', 101)));
        Assert.Equal("Manutenção", a.Nome);
    }
    [Fact]
    public void PosicaoNormalizaComponentesEPreservaAlmoxarifadoAoEditar()
    {
        var p = new PosicaoEstoque(2, "  a  ", " b ", " c ");
        Assert.Equal("a", p.Corredor); Assert.Equal("A", p.CorredorNormalizado);
        Assert.Equal("B", p.EstanteNormalizada); Assert.Equal("C", p.PrateleiraNormalizada);
        p.AlterarEndereco("d", "e", "f"); Assert.Equal(2, p.AlmoxarifadoId);
        p.DefinirAtivo(false); Assert.False(p.Ativo); Assert.Equal("d", p.Corredor);
        p.DefinirAtivo(true); Assert.True(p.Ativo);
    }
    [Theory]
    [InlineData("", "B", "C")]
    [InlineData("A", " ", "C")]
    [InlineData("A", "B", "")]
    public void EnderecoInvalidoNaoAlteraPosicaoParcialmente(string corredor, string estante, string prateleira)
    {
        var p = new PosicaoEstoque(1, "Original", "E1", "P1");
        Assert.Throws<ArgumentException>(() => p.AlterarEndereco(corredor, estante, prateleira));
        Assert.Equal("Original", p.Corredor); Assert.Equal("E1", p.Estante); Assert.Equal("P1", p.Prateleira);
    }
    [Fact]
    public void PosicaoRejeitaAlmoxarifadoInvalidoECamposLongos()
    {
        Assert.Throws<ArgumentException>(() => new PosicaoEstoque(0, "A", "B", "C"));
        Assert.Throws<ArgumentException>(() => new PosicaoEstoque(1, new string('a', 41), "B", "C"));
        Assert.Throws<ArgumentException>(() => new PosicaoEstoque(1, "A", new string('b', 41), "C"));
        Assert.Throws<ArgumentException>(() => new PosicaoEstoque(1, "A", "B", new string('c', 41)));
    }
    [Fact]
    public void VinculoOpcionalNoProdutoNaoAlteraSaldoOuReserva()
    {
        var p = new Produto("Material", 1, 10m); p.RegistrarEntrada(20); p.Reservar(8);
        Assert.Null(p.PosicaoEstoqueId); p.AlterarPosicao(3); Assert.Equal(3, p.PosicaoEstoqueId);
        p.AlterarPosicao(4); Assert.Equal(4, p.PosicaoEstoqueId);
        Assert.Throws<ArgumentException>(() => p.AlterarPosicao(0)); Assert.Equal(4, p.PosicaoEstoqueId);
        p.AlterarPosicao(null); Assert.Null(p.PosicaoEstoqueId);
        Assert.Equal(20, p.Estoque); Assert.Equal(8, p.EstoqueReservado); Assert.Equal(12, p.EstoqueDisponivel);
    }
    private static GestaoEstoqueDbContext Contexto() => new(new DbContextOptionsBuilder<GestaoEstoqueDbContext>()
        .UseSqlServer("Server=localhost;Database=ScriptOnly;Integrated Security=true;TrustServerCertificate=true").Options);
    [Fact]
    public void MigrationPreservaProdutosEUsaChavesRestritasComColunaOpcional()
    {
        using var db = Contexto();
        var sql = db.GetService<IMigrator>().GenerateScript("20261001175000_MaterialReturns", "20261001190000_StockLocations");
        Assert.Contains("CREATE TABLE [Almoxarifados]", sql); Assert.Contains("CREATE TABLE [PosicoesEstoque]", sql);
        Assert.Contains("[PosicaoEstoqueId] int NULL", sql); Assert.Contains("REFERENCES [PosicoesEstoque] ([Id])", sql);
        Assert.Contains("REFERENCES [Almoxarifados] ([Id])", sql); Assert.Contains("CREATE UNIQUE INDEX", sql);
        Assert.DoesNotContain("DROP TABLE", sql); Assert.DoesNotContain("ON DELETE CASCADE", sql);
        Assert.DoesNotContain("[Id].[Produtos]", sql);
    }
    [Fact]
    public void ModeloIsolaUnicidadeDaPosicaoPorAlmoxarifadoENaoCascateiaExclusoes()
    {
        using var db = Contexto();
        var posicao = db.Model.FindEntityType(typeof(PosicaoEstoque))!;
        var unico = Assert.Single(posicao.GetIndexes().Where(i => i.IsUnique));
        Assert.Equal(new[] { "AlmoxarifadoId", "CorredorNormalizado", "EstanteNormalizada", "PrateleiraNormalizada" }, unico.Properties.Select(p => p.Name));
        Assert.Equal(DeleteBehavior.Restrict, Assert.Single(posicao.GetForeignKeys()).DeleteBehavior);
        var produto = db.Model.FindEntityType(typeof(Produto))!;
        Assert.True(produto.FindProperty("PosicaoEstoqueId")!.IsNullable);
        Assert.Equal(DeleteBehavior.Restrict, Assert.Single(produto.GetForeignKeys().Where(f => f.PrincipalEntityType.ClrType == typeof(PosicaoEstoque))).DeleteBehavior);
    }
}
