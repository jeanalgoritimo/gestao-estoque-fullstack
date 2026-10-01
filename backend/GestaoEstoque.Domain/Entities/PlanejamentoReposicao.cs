namespace GestaoEstoque.Domain.Entities;

public static class PlanejamentoReposicao
{
    public static long Sugerir(int minimo, int disponivel, long comprasPendentes)
    {
        if (minimo < 0 || disponivel < 0 || comprasPendentes < 0) throw new ArgumentOutOfRangeException();
        var falta = Math.Max(0L, (long)minimo - disponivel);
        // Compara antes de subtrair para aceitar somas grandes de pedidos sem overflow.
        return comprasPendentes >= falta ? 0L : falta - comprasPendentes;
    }
}
