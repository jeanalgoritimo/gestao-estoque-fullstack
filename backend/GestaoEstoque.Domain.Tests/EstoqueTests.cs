using GestaoEstoque.Domain.Entities;
using Xunit;

namespace GestaoEstoque.Domain.Tests;

public class EstoqueTests
{
    [Fact]
    public void EntradaESaidaAtualizamSaldoEAlerta()
    {
        var produto = new Produto("Parafuso", 1, 2.50m, 3);
        produto.RegistrarEntrada(8);
        produto.RegistrarSaida(5);
        Assert.Equal(3, produto.Estoque);
        Assert.True(produto.EstaComEstoqueBaixo());
    }

    [Fact]
    public void SaidaAcimaDoSaldoNaoAlteraProduto()
    {
        var produto = new Produto("Parafuso", 1, 2.50m);
        produto.RegistrarEntrada(2);
        Assert.Throws<InvalidOperationException>(() => produto.RegistrarSaida(3));
        Assert.Equal(2, produto.Estoque);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void QuantidadeInvalidaNaoAlteraSaldo(int quantidade)
    {
        var produto = new Produto("Parafuso", 1, 2.50m);
        Assert.Throws<ArgumentOutOfRangeException>(() => produto.RegistrarEntrada(quantidade));
        Assert.Throws<ArgumentOutOfRangeException>(() => produto.RegistrarSaida(quantidade));
        Assert.Equal(0, produto.Estoque);
    }

    [Fact]
    public void MovimentoGuardaResponsavelSaldoEDadosDeOrigem()
    {
        var data = DateTime.UtcNow.AddDays(-1);
        var movimento = new MovimentoEstoque(1, TipoMovimento.Entrada, 3, "Recebido",
            data, "PED-123", "Compra", 7, "Operador", 12.50m, 8);

        Assert.Equal(8, movimento.SaldoApos);
        Assert.Equal(7, movimento.UsuarioId);
        Assert.Equal("PED-123", movimento.DocumentoOrigem);
        Assert.Equal(12.50m, movimento.CustoUnitario);
        Assert.Equal(data, movimento.DataEfetivaUtc);
        Assert.Throws<ArgumentException>(() => new MovimentoEstoque(1, TipoMovimento.Saida, 1,
            null, data, "", " ", 7, "Operador", null, 7));
    }
}
