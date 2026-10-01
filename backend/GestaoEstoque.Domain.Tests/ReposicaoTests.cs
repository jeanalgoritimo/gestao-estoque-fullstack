using GestaoEstoque.Domain.Entities;
using GestaoEstoque.Infrastructure.Persistence;
using GestaoEstoque.Infrastructure.Reports;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace GestaoEstoque.Domain.Tests;

public class ReposicaoTests
{
    [Theory]
    [InlineData(10, 3, 0L, 7L)]
    [InlineData(10, 3, 4L, 3L)]
    [InlineData(10, 3, 7L, 0L)]
    [InlineData(10, 3, 20L, 0L)]
    [InlineData(10, 10, 0L, 0L)]
    [InlineData(0, 0, 0L, 0L)]
    [InlineData(2147483647, 0, 0L, 2147483647L)]
    [InlineData(2147483647, 1, 9223372036854775807L, 0L)]
    [InlineData(0, 2147483647, 9223372036854775807L, 0L)]
    public void SugestaoAtingeMinimoDescontaComprasENaoExcedeLimites(int minimo, int disponivel, long compras, long esperado)
        => Assert.Equal(esperado, PlanejamentoReposicao.Sugerir(minimo, disponivel, compras));
    [Fact]
    public void ReservaGeraNecessidadeMesmoComSaldoFisicoAcimaDoMinimo()
    {
        var p = new Produto("Material", 1, 10m, 10); p.RegistrarEntrada(15); p.Reservar(12);
        Assert.Equal(7L, PlanejamentoReposicao.Sugerir(p.EstoqueMinimo, p.EstoqueDisponivel, 0));
        p.LiberarReserva(12); Assert.Equal(0L, PlanejamentoReposicao.Sugerir(p.EstoqueMinimo, p.EstoqueDisponivel, 0));
    }
    [Fact]
    public void RascunhoNaoPodeReceberAntesDeConfirmacao()
    {
        var p = new PedidoCompra(1, 1, "Administrador", rascunho: true); p.AdicionarItem(1, 7);
        Assert.Equal(SituacaoPedidoCompra.Rascunho, p.Situacao);
        Assert.Throws<InvalidOperationException>(() => p.RegistrarRecebimento(1, 2, 2, "Operador"));
        Assert.Equal(0, p.Itens.Single().QuantidadeRecebida);
        p.ConfirmarRascunho(); Assert.Equal(SituacaoPedidoCompra.Aberto, p.Situacao);
        p.RegistrarRecebimento(1, 3, 2, "Operador"); Assert.Equal(SituacaoPedidoCompra.ParcialmenteRecebido, p.Situacao);
        p.RegistrarRecebimento(1, 4, 2, "Operador"); Assert.Equal(SituacaoPedidoCompra.Recebido, p.Situacao);
        Assert.Throws<InvalidOperationException>(() => p.ConfirmarRascunho());
    }
    [Fact]
    public void EditarRascunhoPreservaItemMantidoEAuditoria()
    {
        var p = new PedidoCompra(1, 1, "Administrador", true); p.AdicionarItem(1, 7); p.AdicionarItem(2, 4);
        var mantido = p.Itens.First(); var criado = p.CriadoUtc;
        p.AtualizarRascunho([(1, 9), (3, 5)]);
        Assert.Same(mantido, p.Itens.Single(i => i.ProdutoId == 1)); Assert.Equal(9, mantido.Quantidade);
        Assert.DoesNotContain(p.Itens, i => i.ProdutoId == 2); Assert.Equal(5, p.Itens.Single(i => i.ProdutoId == 3).Quantidade);
        Assert.Equal(criado, p.CriadoUtc); Assert.Equal(1, p.CriadoPorId); Assert.Null(p.EncerradoUtc);
        Assert.All(p.Itens, i => Assert.Equal(0, i.QuantidadeRecebida));
        p.ConfirmarRascunho(); Assert.Throws<InvalidOperationException>(() => p.AtualizarRascunho([(1, 2)]));
    }
    [Fact]
    public void EdicaoInvalidaNaoAlteraRascunho()
    {
        var p = new PedidoCompra(1, 1, "Administrador", true); p.AdicionarItem(1, 7);
        Assert.Throws<ArgumentException>(() => p.AtualizarRascunho([(2, 4), (3, 0)]));
        Assert.Throws<ArgumentException>(() => p.AtualizarRascunho([(1, 4), (1, 3)]));
        Assert.Throws<ArgumentException>(() => p.AtualizarRascunho([]));
        Assert.Equal(1, p.Itens.Single().ProdutoId); Assert.Equal(7, p.Itens.Single().Quantidade);
    }
    [Fact]
    public void CancelarRascunhoImpedeConfirmacaoERecebimento()
    {
        var p = new PedidoCompra(1, 1, "Administrador", true); p.AdicionarItem(1, 7);
        p.CancelarSaldo(1, "Administrador", "Reposição dispensada");
        Assert.Equal(SituacaoPedidoCompra.Cancelado, p.Situacao); Assert.Equal("Reposição dispensada", p.MotivoCancelamento);
        Assert.Throws<InvalidOperationException>(() => p.ConfirmarRascunho());
        Assert.Throws<InvalidOperationException>(() => p.RegistrarRecebimento(1, 1, 2, "Operador"));
    }
    [Fact]
    public void RascunhoVazioNaoConfirmaEConstrutorAntigoMantemAberto()
    {
        var r = new PedidoCompra(1, 1, "Administrador", true);
        Assert.Throws<InvalidOperationException>(() => r.ConfirmarRascunho());
        Assert.Throws<ArgumentException>(() => r.Encerrar(SituacaoPedidoCompra.Recebido, 1, "Administrador"));
        Assert.Equal(SituacaoPedidoCompra.Aberto, new PedidoCompra(1, 1, "Administrador").Situacao);
    }
    [Fact]
    public void ConsultaTraduzReservasComprasPendentesERascunhosParaSqlSemConexao()
    {
        using var db = new GestaoEstoqueDbContext(new DbContextOptionsBuilder<GestaoEstoqueDbContext>()
            .UseSqlServer("Server=localhost;Database=ScriptOnly;Integrated Security=true;TrustServerCertificate=true").Options);
        var sql = ReposicaoQueries.Consultar(db).ToQueryString();
        Assert.Contains("[EstoqueReservado]", sql); Assert.Contains("[EstoqueMinimo]", sql);
        Assert.Contains("[QuantidadeRecebida]", sql); Assert.Contains("SUM", sql);
        Assert.Contains("[Situacao] IN (1, 4)", sql); Assert.Contains("[Situacao] = 6", sql);
        Assert.Contains("[Fornecedores]", sql); Assert.Contains("ORDER BY", sql);
    }
}
