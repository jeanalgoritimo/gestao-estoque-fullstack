namespace GestaoEstoque.Domain.Entities;

public class Produto
{
    public int Id { get; private set; }

    public string Nome { get; private set; } = string.Empty;

    public string Categoria { get; private set; } = string.Empty;

    public decimal Preco { get; private set; }

    public int Estoque { get; private set; }

    public bool Ativo { get; private set; }

    protected Produto()
    {
    }

    public Produto(
        string nome,
        string categoria,
        decimal preco,
        int estoque)
    {
        AlterarNome(nome);
        AlterarCategoria(categoria);
        AlterarPreco(preco);
        AlterarEstoque(estoque);

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

    public void AlterarCategoria(string categoria)
    {
        if (string.IsNullOrWhiteSpace(categoria))
            throw new ArgumentException(
                "A categoria é obrigatória.",
                nameof(categoria));

        Categoria = categoria.Trim();
    }

    public void AlterarPreco(decimal preco)
    {
        if (preco <= 0)
            throw new ArgumentException(
                "O preço deve ser maior que zero.",
                nameof(preco));

        Preco = preco;
    }

    public void AlterarEstoque(int estoque)
    {
        if (estoque < 0)
            throw new ArgumentException(
                "O estoque não pode ser negativo.",
                nameof(estoque));

        Estoque = estoque;
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
        return Estoque < 5;
    }
}