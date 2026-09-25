namespace GestaoEstoque.Domain.Entities;

public enum TipoMovimento { Entrada = 1, Saida = 2 }

public class MovimentoEstoque
{
    public long Id { get; private set; }
    public int ProdutoId { get; private set; }
    public TipoMovimento Tipo { get; private set; }
    public int Quantidade { get; private set; }
    public DateTime DataUtc { get; private set; }
    public string? Observacao { get; private set; }

    protected MovimentoEstoque() { }

    public MovimentoEstoque(int produtoId, TipoMovimento tipo, int quantidade, string? observacao)
    {
        if (produtoId <= 0) throw new ArgumentOutOfRangeException(nameof(produtoId));
        if (!Enum.IsDefined(tipo)) throw new ArgumentOutOfRangeException(nameof(tipo));
        if (quantidade <= 0) throw new ArgumentOutOfRangeException(nameof(quantidade));
        if (observacao?.Length > 300) throw new ArgumentException("Observação limitada a 300 caracteres.", nameof(observacao));
        ProdutoId = produtoId;
        Tipo = tipo;
        Quantidade = quantidade;
        Observacao = observacao?.Trim();
        DataUtc = DateTime.UtcNow;
    }
}
