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
}
