namespace GestaoEstoque.Domain.Entities;

public class Produto
{
    public int Id { get; private set; }

    public string Nome { get; private set; } = string.Empty;

    public int CategoriaId { get; private set; }
    public CategoriaProduto? CategoriaProduto { get; private set; }

    public decimal Preco { get; private set; }

    public int Estoque { get; private set; }
    public int EstoqueMinimo { get; private set; }
    public byte[] Versao { get; private set; } = [];

    public bool Ativo { get; private set; }

    protected Produto()
    {
    }

    public Produto(
        string nome,
        int categoriaId,
        decimal preco,
        int estoqueMinimo = 5)
    {
        AlterarNome(nome);
        AlterarCategoria(categoriaId);
        AlterarPreco(preco);
        AlterarEstoqueMinimo(estoqueMinimo);

        Ativo = true;
    }

    public void AlterarNome(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException(
                "O nome do produto é obrigatório.",
                nameof(nome));

        Nome = nome.Trim();
    }

    public void AlterarCategoria(int categoriaId)
    {
        if (categoriaId <= 0)
            throw new ArgumentException(
                "A categoria é obrigatória.",
                nameof(categoriaId));

        CategoriaId = categoriaId;
    }

    public void AlterarPreco(decimal preco)
    {
        if (preco <= 0)
            throw new ArgumentException(
                "O preço deve ser maior que zero.",
                nameof(preco));

        Preco = preco;
    }

    public void AlterarEstoqueMinimo(int estoqueMinimo)
    {
        if (estoqueMinimo < 0)
            throw new ArgumentException(
                "O estoque mínimo não pode ser negativo.",
                nameof(estoqueMinimo));

        EstoqueMinimo = estoqueMinimo;
    }

    public void RegistrarEntrada(int quantidade)
    {
        if (quantidade <= 0) throw new ArgumentOutOfRangeException(nameof(quantidade));
        Estoque = checked(Estoque + quantidade);
    }

    public void RegistrarSaida(int quantidade)
    {
        if (quantidade <= 0) throw new ArgumentOutOfRangeException(nameof(quantidade));
        if (quantidade > Estoque) throw new InvalidOperationException("Estoque insuficiente.");
        Estoque -= quantidade;
    }

    public void Ativar()
    {
        Ativo = true;
    }

    public void Desativar()
    {
        Ativo = false;
    }

    public bool EstaComEstoqueBaixo()
    {
        return Estoque <= EstoqueMinimo;
    }
}
