namespace GestaoEstoque.Domain.Entities;

public static class QuantidadeEstoque
{
    // Mantém o limite anterior e permite milésimos sem perder precisão no navegador.
    public const decimal Maximo = 2147483647.999m;
    public static void Validar(decimal valor)
    {
        if (valor < 0 || valor > Maximo || decimal.Round(valor, 3) != valor)
            throw new ArgumentOutOfRangeException(nameof(valor), "Informe uma quantidade não negativa, com até três casas decimais e no máximo 2147483647,999.");
    }
}
