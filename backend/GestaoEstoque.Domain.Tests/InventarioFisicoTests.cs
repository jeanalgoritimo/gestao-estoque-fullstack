using GestaoEstoque.Domain.Entities;
using Xunit;

namespace GestaoEstoque.Domain.Tests;

public class InventarioFisicoTests
{
    [Fact]
    public void ContagemMostraDiferencaEExigeMotivo()
    {
        var inventario = new InventarioFisico(1, 10, [1, 2, 3], 2, "Operador");
        inventario.RegistrarContagem(7);
        Assert.Equal(-3, inventario.Diferenca);
        Assert.Throws<ArgumentException>(() => inventario.Confirmar(" ", 2, "Operador"));
        inventario.Confirmar("Perda identificada", 2, "Operador");
        Assert.Equal(SituacaoInventario.Confirmado, inventario.Situacao);
        Assert.Throws<InvalidOperationException>(() => inventario.RegistrarContagem(8));
    }

    [Fact]
    public void SemContagemNaoPermiteConfirmacao()
    {
        var inventario = new InventarioFisico(1, 0, [1], 2, "Operador");
        Assert.Throws<InvalidOperationException>(() => inventario.Confirmar("Conferência", 2, "Operador"));
        inventario.Cancelar(2, "Operador");
        Assert.Equal(SituacaoInventario.Cancelado, inventario.Situacao);
    }
}
