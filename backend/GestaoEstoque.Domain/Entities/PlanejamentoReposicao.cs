namespace GestaoEstoque.Domain.Entities;

public static class PlanejamentoReposicao
{
    public static decimal Sugerir(decimal minimo, decimal disponivel, decimal comprasPendentes)
    {
        if (minimo < 0 || disponivel < 0 || comprasPendentes < 0) throw new ArgumentOutOfRangeException();
        var falta = Math.Max(0m, minimo - disponivel);
        // Compara antes de subtrair para aceitar somas grandes de pedidos sem overflow.
        return comprasPendentes >= falta ? 0m : falta - comprasPendentes;
    }
}
