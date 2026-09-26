using GestaoEstoque.Domain.Entities;
using Xunit;

namespace GestaoEstoque.Domain.Tests;

public class CategoriaTests
{
    [Fact]
    public void NomeNormalizadoEvitaVariantesDeMaiusculas()
    {
        var categoria = new CategoriaProduto("  Ferragens  ");
        Assert.Equal("Ferragens", categoria.Nome);
        Assert.Equal("FERRAGENS", categoria.NomeNormalizado);
    }

    [Fact]
    public void ProdutoExigeCategoriaValida() =>
        Assert.Throws<ArgumentException>(() => new Produto("Parafuso", 0, 2.5m));
}
