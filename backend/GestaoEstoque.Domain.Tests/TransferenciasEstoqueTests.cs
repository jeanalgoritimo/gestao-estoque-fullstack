using GestaoEstoque.Domain.Entities;
using GestaoEstoque.Infrastructure.Persistence;
using GestaoEstoque.Infrastructure.Persistence.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;
namespace GestaoEstoque.Domain.Tests;

public class TransferenciasEstoqueTests
{
    [Fact]
    public void TransferenciaPreservaTotalEReservasDaOrigem()
    {
        var origem = new SaldoLocal(1, 1, 10.5m, 3m); var destino = new SaldoLocal(1, 2, 2m);
        origem.TransferirPara(destino, 7.5m);
        Assert.Equal(3m, origem.Quantidade); Assert.Equal(3m, origem.Reservado); Assert.Equal(0m, origem.Disponivel);
        Assert.Equal(9.5m, destino.Quantidade); Assert.Equal(12.5m, origem.Quantidade + destino.Quantidade);
        Assert.Throws<InvalidOperationException>(() => origem.TransferirPara(destino, 0.001m));
        Assert.Equal(9.5m, destino.Quantidade);
    }
    [Fact]
    public void ProdutoSemLocalPodeSerDistribuidoEmPosicaoDefinida()
    {
        var origem = new SaldoLocal(1, null, 1.25m); var destino = new SaldoLocal(1, 2);
        origem.TransferirPara(destino, 0.001m);
        Assert.Equal(1.249m, origem.Quantidade); Assert.Equal(0.001m, destino.Quantidade);
    }
    [Theory]
    [InlineData("0")][InlineData("-1")][InlineData("0.0001")][InlineData("2147483648")]
    public void QuantidadeInvalidaNaoMudaNenhumSaldo(string texto)
    {
        var origem = new SaldoLocal(1, 1, 5m); var destino = new SaldoLocal(1, 2);
        var quantidade = decimal.Parse(texto, System.Globalization.CultureInfo.InvariantCulture);
        Assert.ThrowsAny<ArgumentException>(() => origem.TransferirPara(destino, quantidade));
        Assert.Equal(5m, origem.Quantidade); Assert.Equal(0m, destino.Quantidade);
    }
    [Fact]
    public void LocaisRepetidosOutroProdutoOuLimiteDoDestinoNaoAlteramOrigem()
    {
        var origem = new SaldoLocal(1, 1, 1m);
        Assert.Throws<ArgumentException>(() => origem.TransferirPara(new SaldoLocal(1, 1), 1m));
        Assert.Throws<ArgumentException>(() => origem.TransferirPara(new SaldoLocal(2, 2), 1m));
        Assert.Throws<ArgumentOutOfRangeException>(() => origem.TransferirPara(new SaldoLocal(1, 2, QuantidadeEstoque.Maximo), 0.001m));
        Assert.Equal(1m, origem.Quantidade);
    }
    [Fact]
    public void HistoricoGuardaDescricoesEUsuarioNoMomentoDaTransferencia()
    {
        var t = new TransferenciaEstoque(1, null, 2, 0.001m, 1, " Operador ", " Organização ", "Sem localização", "Almoxarifado B");
        Assert.Equal("Operador", t.UsuarioNome); Assert.Equal("Organização", t.Motivo);
        Assert.Equal("Sem localização", t.OrigemDescricao); Assert.Equal("Almoxarifado B", t.DestinoDescricao);
        Assert.Equal(DateTimeKind.Utc, t.DataUtc.Kind);
        Assert.Throws<ArgumentException>(() => new TransferenciaEstoque(1, 2, 2, 1m, 1, "A", "Motivo", "B", "B"));
    }
    [Fact]
    public void CompraReservaEntregaDevolucaoEInventarioMantemTotaisAposTransferir()
    {
        var locais = new List<SaldoLocal>();
        // Entrada no padrão e transferência parcial para outro local.
        DistribuicaoEstoque.Sincronizar(locais, 1, 1, 0, 0, 10.001m, 0);
        var destino = new SaldoLocal(1, 2); locais.Add(destino); locais[0].TransferirPara(destino, 8m);
        // Reserva maior que o saldo do padrão distribui o compromisso entre locais.
        DistribuicaoEstoque.Sincronizar(locais, 1, 1, 10.001m, 0, 10.001m, 6m);
        Assert.Equal(2.001m, locais[0].Reservado); Assert.Equal(3.999m, destino.Reservado);
        Assert.Throws<InvalidOperationException>(() => locais[0].TransferirPara(destino, 0.001m));
        // Entrega parcial libera somente a reserva entregue e consome saldo.
        DistribuicaoEstoque.Sincronizar(locais, 1, 1, 10.001m, 6m, 7.001m, 3m);
        Assert.Equal(7.001m, locais.Sum(s => s.Quantidade)); Assert.Equal(3m, locais.Sum(s => s.Reservado));
        // Cancelamento libera o restante, devolução entra no novo padrão e contagem ajusta o total.
        DistribuicaoEstoque.Sincronizar(locais, 1, 2, 7.001m, 3m, 7.001m, 0);
        DistribuicaoEstoque.Sincronizar(locais, 1, 2, 7.001m, 0, 7.501m, 0);
        DistribuicaoEstoque.Sincronizar(locais, 1, 2, 7.501m, 0, 0.001m, 0);
        Assert.Equal(0.001m, locais.Sum(s => s.Quantidade)); Assert.Equal(0m, locais.Sum(s => s.Reservado));
        Assert.All(locais, s => Assert.True(s.Disponivel >= 0));
    }
    [Fact]
    public void AlterarPadraoNaoTransfereSaldoESaidaNaoUsaReservaDeOutroLocal()
    {
        var locais = new List<SaldoLocal> { new(1, 1, 5m, 4m), new(1, 2, 5m) };
        DistribuicaoEstoque.Sincronizar(locais, 1, 2, 10, 4, 10, 4);
        Assert.Equal(5m, locais[0].Quantidade); Assert.Equal(5m, locais[1].Quantidade);
        DistribuicaoEstoque.Sincronizar(locais, 1, 2, 10, 4, 4, 4);
        Assert.Equal(4m, locais[0].Quantidade); Assert.Equal(4m, locais[0].Reservado); Assert.Equal(0m, locais[1].Quantidade);
    }
    [Fact]
    public void DivergenciaDetectadaAntesDeAlterarLocais()
    {
        var locais = new List<SaldoLocal> { new(1, 1, 5m) };
        Assert.Throws<InvalidOperationException>(() => DistribuicaoEstoque.Sincronizar(locais, 1, 1, 6, 0, 7, 0));
        Assert.Equal(5m, locais[0].Quantidade);
    }
    private static GestaoEstoqueDbContext Contexto() => new(new DbContextOptionsBuilder<GestaoEstoqueDbContext>()
        .UseSqlServer("Server=localhost;Database=ScriptOnly;Integrated Security=true;TrustServerCertificate=true").Options);
    [Fact]
    public void MigrationDistribuiSaldoLegadoPreservaReservasEBloqueiaPerdaDeHistorico()
    {
        using var db = Contexto(); var migrator = db.GetService<IMigrator>();
        var sql = migrator.GenerateScript("20261002100000_FractionalQuantities", "20261002140000_StockTransfers");
        Assert.Contains("SELECT [Id], [PosicaoEstoqueId], [Estoque], [EstoqueReservado] FROM [Produtos]", sql);
        Assert.Contains("CREATE TABLE [TransferenciasEstoque]", sql); Assert.Contains("CK_SaldosLocais_Quantidades", sql);
        Assert.Contains("WHERE [PosicaoEstoqueId] IS NULL", sql); Assert.Contains("WHERE [PosicaoEstoqueId] IS NOT NULL", sql);
        Assert.DoesNotContain("UPDATE [Produtos]", sql); Assert.DoesNotContain("DROP TABLE", sql);
        var down = migrator.GenerateScript("20261002140000_StockTransfers", "20261002100000_FractionalQuantities");
        Assert.Contains("THROW 51000", down);
    }
    [Fact]
    public void ModeloTemConcorrenciaEConsultaSaldoLocalTraduzParaSql()
    {
        using var db = Contexto();
        Assert.True(db.Model.FindEntityType(typeof(SaldoLocal))!.FindProperty(nameof(SaldoLocal.Versao))!.IsConcurrencyToken);
        Assert.True(db.Model.FindEntityType(typeof(Produto))!.FindProperty(nameof(Produto.Versao))!.IsConcurrencyToken);
        var sql = db.SaldosLocais.Where(s => s.ProdutoId == 1).Select(s => new { s.PosicaoEstoqueId, Disponivel = s.Quantidade - s.Reservado }).ToQueryString();
        Assert.Contains("[Quantidade] -", sql); Assert.Contains("[Reservado]", sql);
    }
    [Fact]
    public void SnapshotCorrespondeAoModeloAtual()
    {
        using var db = Contexto();
        var snapshot = new GestaoEstoqueDbContextModelSnapshot().Model;
        var modelo = db.GetService<IModelRuntimeInitializer>().Initialize(snapshot, designTime: true);
        var atual = db.GetService<IDesignTimeModel>().Model;
        var diferencas = db.GetService<IMigrationsModelDiffer>().GetDifferences(modelo.GetRelationalModel(), atual.GetRelationalModel());
        Assert.Empty(diferencas);
    }
}
