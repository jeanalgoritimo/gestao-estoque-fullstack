using GestaoEstoque.Domain.Entities;
using GestaoEstoque.Infrastructure.Persistence;
using GestaoEstoque.Infrastructure.Reports;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace GestaoEstoque.Domain.Tests;

public class DevolucaoMaterialTests
{
    private static RequisicaoMaterial Aprovada()
    {
        var r = new RequisicaoMaterial("Manutenção", 1, "Solicitante", 2);
        r.AdicionarItem(1, 10); r.Enviar(); r.Aprovar(2, "Administrador"); return r;
    }
    [Fact]
    public void DevolucoesAcumulamSemApagarEntregaOuReabrirRequisicao()
    {
        var r = Aprovada(); r.Entregar(1, 10, 3, "Operador");
        var encerrado = r.EncerradoUtc;
        r.RegistrarDevolucao(1, 3); r.RegistrarDevolucao(1, 7);
        Assert.Equal(10, r.Itens.Single().QuantidadeEntregue);
        Assert.Equal(10, r.Itens.Single().QuantidadeDevolvida);
        Assert.Equal(SituacaoRequisicao.Atendida, r.Situacao);
        Assert.Equal(encerrado, r.EncerradoUtc); Assert.Equal("Operador", r.EncerradoPorNome);
        Assert.Equal(2, r.CentroCustoId);
        Assert.Throws<ArgumentException>(() => r.RegistrarDevolucao(1, 1));
        Assert.Throws<InvalidOperationException>(() => r.Entregar(1, 1, 3, "Operador"));
    }
    [Theory]
    [InlineData(0)] [InlineData(-1)] [InlineData(8)]
    public void DevolucaoInvalidaNaoAlteraQuantidade(int quantidade)
    {
        var r = Aprovada(); r.Entregar(1, 10, 3, "Operador"); r.RegistrarDevolucao(1, 3);
        Assert.Throws<ArgumentException>(() => r.RegistrarDevolucao(1, quantidade));
        Assert.Throws<ArgumentException>(() => r.RegistrarDevolucao(99, 1));
        Assert.Equal(3, r.Itens.Single().QuantidadeDevolvida);
    }
    [Fact]
    public void DevolucaoParcialPreservaReservaEPendenteOriginal()
    {
        var p = new Produto("Material", 1, 10m); p.RegistrarEntrada(20); p.Reservar(10);
        var r = Aprovada(); r.Entregar(1, 4, 3, "Operador"); p.EntregarReserva(4);
        r.RegistrarDevolucao(1, 3); p.RegistrarEntrada(3);
        Assert.Equal(19, p.Estoque); Assert.Equal(6, p.EstoqueReservado); Assert.Equal(13, p.EstoqueDisponivel);
        Assert.Equal(SituacaoRequisicao.ParcialmenteAtendida, r.Situacao);
        r.Entregar(1, 6, 3, "Operador"); p.EntregarReserva(6);
        Assert.Equal(10, r.Itens.Single().QuantidadeEntregue); Assert.Equal(3, r.Itens.Single().QuantidadeDevolvida);
        Assert.Equal(0, p.EstoqueReservado); Assert.Equal(13, p.Estoque);
    }
    [Fact]
    public void CanceladaAposEntregaPermiteDevolverSemMudarCancelamento()
    {
        var r = Aprovada(); r.Entregar(1, 4, 3, "Operador"); r.Cancelar(2, "Administrador", "Dispensado restante");
        var encerrado = r.EncerradoUtc; r.RegistrarDevolucao(1, 4);
        Assert.Equal(SituacaoRequisicao.Cancelada, r.Situacao); Assert.Equal(encerrado, r.EncerradoUtc);
        Assert.Equal("Dispensado restante", r.MotivoCancelamento); Assert.Equal(4, r.Itens.Single().QuantidadeEntregue);
    }
    [Fact]
    public void SemEntregaNaoPermiteDevolverInclusiveCancelada()
    {
        var r = Aprovada(); Assert.Throws<InvalidOperationException>(() => r.RegistrarDevolucao(1, 1));
        r.Cancelar(2, "Administrador", "Dispensado");
        Assert.Throws<ArgumentException>(() => r.RegistrarDevolucao(1, 1));
        Assert.Equal(0, r.Itens.Single().QuantidadeDevolvida);
    }
    [Fact]
    public void MovimentoDevolucaoSomenteEntradaEExclusivoDoRecebimento()
    {
        var id = Guid.NewGuid(); var entrada = new MovimentoEstoque(1, TipoMovimento.Entrada, 3, null);
        entrada.VincularDevolucao(8, id);
        Assert.Equal(8L, entrada.RequisicaoMaterialId); Assert.Equal(id, entrada.DevolucaoId);
        Assert.Null(entrada.EntregaId); Assert.Null(entrada.RecebimentoId);
        Assert.Throws<InvalidOperationException>(() => entrada.VincularRecebimento(4, Guid.NewGuid()));
        Assert.Throws<InvalidOperationException>(() => entrada.VincularDevolucao(8, Guid.NewGuid()));
        var recebimento = new MovimentoEstoque(1, TipoMovimento.Entrada, 3, null); recebimento.VincularRecebimento(4, id);
        Assert.Throws<InvalidOperationException>(() => recebimento.VincularDevolucao(8, id));
        var saida = new MovimentoEstoque(1, TipoMovimento.Saida, 3, null);
        Assert.Throws<InvalidOperationException>(() => saida.VincularDevolucao(8, id));
        var invalida = new MovimentoEstoque(1, TipoMovimento.Entrada, 3, null);
        Assert.Throws<InvalidOperationException>(() => invalida.VincularDevolucao(0, id));
        Assert.Throws<InvalidOperationException>(() => invalida.VincularDevolucao(8, Guid.Empty));
    }
    private static GestaoEstoqueDbContext Contexto() => new(new DbContextOptionsBuilder<GestaoEstoqueDbContext>()
        .UseSqlServer("Server=localhost;Database=ScriptOnly;Integrated Security=true;TrustServerCertificate=true").Options);
    [Fact]
    public void MigrationPreservaEntregasEInicializaDevolvidoZero()
    {
        using var db = Contexto();
        var sql = db.GetService<IMigrator>().GenerateScript("20261001143000_CostCenters", "20261001175000_MaterialReturns");
        Assert.Contains("[QuantidadeDevolvida] int NOT NULL DEFAULT 0", sql);
        Assert.Contains("[DevolucaoId] uniqueidentifier NULL", sql);
        Assert.Contains("CREATE INDEX", sql);
        Assert.DoesNotContain("DROP TABLE", sql); Assert.DoesNotContain("DROP COLUMN", sql);
    }
    [Fact]
    public void RelatorioSeparaSaidasEDevolucoesPorTipoEDataDoMovimento()
    {
        using var db = Contexto(); var inicio = new DateTime(2026, 10, 1, 3, 0, 0, DateTimeKind.Utc);
        var entregues = ConsumoQueries.Entregues(db, inicio, inicio.AddDays(1), null, false).ToQueryString();
        var devolvidas = ConsumoQueries.Devolvidas(db, inicio, inicio.AddDays(1), null, false).ToQueryString();
        Assert.Contains("[Tipo]", entregues);
        Assert.True(entregues.Contains("[Tipo] = 2") || entregues.Contains(" int = 2;"), entregues);
        Assert.Contains("[Tipo]", devolvidas);
        Assert.True(devolvidas.Contains("[Tipo] = 1") || devolvidas.Contains(" int = 1;"), devolvidas); Assert.Contains("[DevolucaoId] IS NOT NULL", devolvidas);
        Assert.Contains("[DataUtc]", devolvidas); Assert.Contains("GROUP BY", devolvidas);
        Assert.DoesNotContain("[CriadoUtc]", devolvidas);
    }
}
