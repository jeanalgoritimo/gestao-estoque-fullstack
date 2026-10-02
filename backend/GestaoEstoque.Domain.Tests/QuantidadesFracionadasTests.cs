using GestaoEstoque.Domain.Entities;
using GestaoEstoque.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;
namespace GestaoEstoque.Domain.Tests;

public class QuantidadesFracionadasTests
{
    [Fact]
    public void ReservaEntregaDevolucaoMantemMilesimosSemReabrirRequisicao()
    {
        var p = new Produto("Cabo", 1, 2m, 0.5m); p.RegistrarEntrada(1.001m);
        var r = new RequisicaoMaterial("Manutenção", 1, "Operador"); r.AdicionarItem(1, 0.3m);
        r.Enviar(); r.Aprovar(1, "Admin"); p.Reservar(0.3m);
        p.EntregarReserva(0.1m); r.Entregar(1, 0.1m, 1, "Admin");
        Assert.Equal(0.2m, p.EstoqueReservado); Assert.Equal(0.701m, p.EstoqueDisponivel);
        p.EntregarReserva(0.2m); r.Entregar(1, 0.2m, 1, "Admin");
        r.RegistrarDevolucao(1, 0.001m); p.RegistrarEntrada(0.001m);
        Assert.Equal(0.702m, p.Estoque); Assert.Equal(0m, p.EstoqueReservado);
        Assert.Equal(SituacaoRequisicao.Atendida, r.Situacao);
        Assert.Equal(0.001m, r.Itens.Single().QuantidadeDevolvida);
    }
    [Fact]
    public void RecebimentosFracionadosEncerramPedidoExatamente()
    {
        var p = new PedidoCompra(1, 1, "Admin"); p.AdicionarItem(1, 0.3m);
        p.RegistrarRecebimento(1, 0.1m, 1, "Admin"); p.RegistrarRecebimento(1, 0.2m, 1, "Admin");
        Assert.Equal(SituacaoPedidoCompra.Recebido, p.Situacao);
        Assert.Equal(0.3m, p.Itens.Single().QuantidadeRecebida);
    }
    [Fact]
    public void ContagemESugestaoReposicaoPreservamMilesimos()
    {
        var i = new InventarioFisico(1, 1.001m, [1], 1, "Admin"); i.RegistrarContagem(0.999m);
        Assert.Equal(-0.002m, i.Diferenca);
        Assert.Equal(0.001m, PlanejamentoReposicao.Sugerir(1.001m, 0.7m, 0.3m));
    }
    [Theory]
    [InlineData("0.0001")]
    [InlineData("0.1234")]
    [InlineData("2147483648")]
    public void EntradaInvalidaNaoAlteraSaldo(string texto)
    {
        var q = decimal.Parse(texto, System.Globalization.CultureInfo.InvariantCulture);
        var p = new Produto("Material", 1, 2m); p.RegistrarEntrada(0.001m);
        Assert.Throws<ArgumentOutOfRangeException>(() => p.RegistrarEntrada(q));
        Assert.Equal(0.001m, p.Estoque);
    }
    [Fact]
    public void SaldoNoLimiteNaoPermiteAcrescimoNemPerdeValorAnterior()
    {
        var p = new Produto("Material", 1, 2m); p.RegistrarEntrada(QuantidadeEstoque.Maximo);
        Assert.Throws<ArgumentOutOfRangeException>(() => p.RegistrarEntrada(0.001m));
        Assert.Equal(QuantidadeEstoque.Maximo, p.Estoque);
    }
    [Fact]
    public void RascunhoInvalidoNaoRemoveItensExistentes()
    {
        var p = new PedidoCompra(1, 1, "Admin", true); p.AdicionarItem(1, 1m);
        Assert.Throws<ArgumentOutOfRangeException>(() => p.AtualizarRascunho([(2, 0.5m), (3, 0.0001m)]));
        Assert.Equal(1, p.Itens.Single().ProdutoId);
        var r = new RequisicaoMaterial("Finalidade", 1, "Admin"); r.AdicionarItem(1, 1m);
        Assert.Throws<ArgumentOutOfRangeException>(() => r.AtualizarRascunho("Outra", [(2, 0.0001m)]));
        Assert.Equal("Finalidade", r.Finalidade); Assert.Equal(1, r.Itens.Single().ProdutoId);
    }
    [Fact]
    public void LiberacaoInvalidaNaoMudaReserva()
    {
        var p = new Produto("Material", 1, 2m); p.RegistrarEntrada(1m); p.Reservar(0.3m);
        Assert.Throws<ArgumentOutOfRangeException>(() => p.EntregarReserva(0.0001m));
        Assert.Equal(0.3m, p.EstoqueReservado); Assert.Equal(1m, p.Estoque);
    }
    [Fact]
    public void MigrationAlteraTiposPreservaDadosEBloqueiaReversaoComFracoes()
    {
        using var db = new GestaoEstoqueDbContext(new DbContextOptionsBuilder<GestaoEstoqueDbContext>()
            .UseSqlServer("Server=localhost;Database=ScriptOnly;Integrated Security=true;TrustServerCertificate=true").Options);
        var migrator = db.GetService<IMigrator>();
        var sql = migrator.GenerateScript("20261001194000_MeasurementUnits", "20261002100000_FractionalQuantities");
        Assert.Contains("ALTER COLUMN [Estoque] decimal(13,3)", sql);
        Assert.Contains("ALTER COLUMN [QuantidadeContada] decimal(13,3) NULL", sql);
        Assert.DoesNotContain("DROP TABLE", sql);
        var down = migrator.GenerateScript("20261002100000_FractionalQuantities", "20261001194000_MeasurementUnits");
        Assert.Contains("THROW 51000", down); Assert.Contains("ROUND([Quantidade], 0)", down);
    }
}
