using GestaoEstoque.Domain.Entities;
using Xunit;

namespace GestaoEstoque.Domain.Tests;

public class RequisicaoMaterialTests
{
    private static Produto ProdutoComSaldo()
    {
        var p = new Produto("Material", 1, 10m); p.RegistrarEntrada(20); return p;
    }
    private static RequisicaoMaterial Aprovada()
    {
        var r = new RequisicaoMaterial("Uso na manutenção", 1, "Solicitante");
        r.AdicionarItem(1, 8); r.Enviar(); r.Aprovar(2, "Administrador"); return r;
    }
    [Fact]
    public void EditarRascunhoAtualizaItensPreservaOsMantidosESolicitante()
    {
        var r = new RequisicaoMaterial("Finalidade inicial", 1, "Solicitante");
        r.AdicionarItem(1, 8); r.AdicionarItem(2, 4);
        var mantido = r.Itens.First(); var criado = r.CriadoUtc;
        r.AtualizarRascunho("  Finalidade corrigida  ", [(1, 12), (3, 5)]);
        Assert.Equal("Finalidade corrigida", r.Finalidade);
        Assert.Same(mantido, r.Itens.Single(i => i.ProdutoId == 1));
        Assert.Equal(12, mantido.Quantidade);
        Assert.DoesNotContain(r.Itens, i => i.ProdutoId == 2);
        Assert.Equal(5, r.Itens.Single(i => i.ProdutoId == 3).Quantidade);
        Assert.All(r.Itens, i => Assert.Equal(0, i.QuantidadeEntregue));
        Assert.Equal(SituacaoRequisicao.Rascunho, r.Situacao);
        Assert.Equal(criado, r.CriadoUtc); Assert.Equal(1, r.SolicitanteId);
        r.Enviar(); Assert.Equal(SituacaoRequisicao.Pendente, r.Situacao);
    }
    [Theory]
    [InlineData(2)] [InlineData(3)] [InlineData(4)] [InlineData(5)] [InlineData(6)]
    public void EdicaoBloqueadaDepoisDoRascunho(int situacao)
    {
        var r = new RequisicaoMaterial("Original", 1, "Solicitante"); r.AdicionarItem(1, 8);
        if (situacao == 6) r.Cancelar(1, "Solicitante", "Desistência");
        else
        {
            r.Enviar();
            if (situacao >= 3) r.Aprovar(2, "Administrador");
            if (situacao == 4) r.Entregar(1, 3, 3, "Operador");
            if (situacao == 5) r.Entregar(1, 8, 3, "Operador");
        }
        Assert.Throws<InvalidOperationException>(() => r.AtualizarRascunho("Alterada", [(1, 2)]));
        Assert.Equal("Original", r.Finalidade); Assert.Equal(8, r.Itens.Single().Quantidade);
    }
    [Fact]
    public void EdicaoInvalidaNaoModificaRascunhoParcialmente()
    {
        var r = new RequisicaoMaterial("Original", 1, "Solicitante"); r.AdicionarItem(1, 8);
        Assert.Throws<ArgumentException>(() => r.AtualizarRascunho("Alterada", [(2, 4), (3, 0)]));
        Assert.Throws<ArgumentException>(() => r.AtualizarRascunho("Alterada", [(1, 4), (1, 3)]));
        Assert.Throws<ArgumentException>(() => r.AtualizarRascunho("Alterada", []));
        Assert.Throws<ArgumentException>(() => r.AtualizarRascunho(" ", [(1, 4)]));
        Assert.Equal("Original", r.Finalidade); Assert.Equal(8, r.Itens.Single().Quantidade);
    }
    [Fact]
    public void ReservaProtegeSaldoDeSaidasAvulsasEAjustes()
    {
        var p = ProdutoComSaldo(); p.Reservar(8);
        Assert.Equal(20, p.Estoque); Assert.Equal(8, p.EstoqueReservado); Assert.Equal(12, p.EstoqueDisponivel);
        Assert.Throws<InvalidOperationException>(() => p.RegistrarSaida(13));
        p.RegistrarSaida(12); Assert.Equal(8, p.Estoque); Assert.Equal(0, p.EstoqueDisponivel);
    }
    [Fact]
    public void EntregaParcialReduzReservaESaldoFisicoSemLiberarReservaDeOutraRequisicao()
    {
        var p = ProdutoComSaldo(); p.Reservar(8); p.Reservar(5);
        var r = Aprovada(); r.Entregar(1, 3, 3, "Operador"); p.EntregarReserva(3);
        Assert.Equal(SituacaoRequisicao.ParcialmenteAtendida, r.Situacao);
        Assert.Equal(17, p.Estoque); Assert.Equal(10, p.EstoqueReservado); Assert.Equal(7, p.EstoqueDisponivel);
        r.Cancelar(2, "Administrador", "Saldo dispensado"); p.LiberarReserva(5);
        Assert.Equal(3, r.Itens.Single().QuantidadeEntregue);
        Assert.Equal(17, p.Estoque); Assert.Equal(5, p.EstoqueReservado); Assert.Equal(12, p.EstoqueDisponivel);
    }
    [Fact]
    public void EntregaCompletaEncerraRequisicaoEImpedeNovaEntrega()
    {
        var r = Aprovada(); r.Entregar(1, 8, 3, "Operador");
        Assert.Equal(SituacaoRequisicao.Atendida, r.Situacao); Assert.Equal("Operador", r.EncerradoPorNome);
        Assert.Throws<InvalidOperationException>(() => r.Entregar(1, 1, 3, "Operador"));
        Assert.Throws<InvalidOperationException>(() => r.Cancelar(2, "Administrador", "Cancelar"));
    }
    [Fact]
    public void TransicoesInvalidasEExcessoDeEntregaNaoSaoPermitidos()
    {
        var r = new RequisicaoMaterial("Manutenção", 1, "Solicitante");
        Assert.Throws<InvalidOperationException>(() => r.Enviar());
        r.AdicionarItem(1, 8);
        Assert.Throws<ArgumentException>(() => r.AdicionarItem(1, 1));
        Assert.Throws<InvalidOperationException>(() => r.Aprovar(2, "Administrador"));
        r.Enviar(); r.Aprovar(2, "Administrador");
        Assert.Throws<ArgumentException>(() => r.Entregar(1, 9, 3, "Operador"));
        Assert.Equal(0, r.Itens.Single().QuantidadeEntregue);
        Assert.Throws<InvalidOperationException>(() => r.Aprovar(2, "Administrador"));
    }
    [Fact]
    public void ReservaInsuficienteEProdutoInativoNaoSaoPermitidos()
    {
        var p = ProdutoComSaldo(); p.Reservar(8);
        Assert.Throws<InvalidOperationException>(() => p.Reservar(13));
        Assert.Throws<InvalidOperationException>(() => p.Desativar());
        p.LiberarReserva(8); p.Desativar();
        Assert.Throws<InvalidOperationException>(() => p.Reservar(1));
    }
}
