namespace GestaoEstoque.Domain.Entities;

public class CategoriaProduto
{
    public int Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string NomeNormalizado { get; private set; } = string.Empty;
    public bool Ativo { get; private set; }

    protected CategoriaProduto() { }

    public CategoriaProduto(string nome)
    {
        Renomear(nome);
        Ativo = true;
    }

    public void Renomear(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome) || nome.Trim().Length > 100)
            throw new ArgumentException("Informe uma categoria de até 100 caracteres.", nameof(nome));
        Nome = nome.Trim();
        NomeNormalizado = Nome.ToUpperInvariant();
    }

    public void Desativar() => Ativo = false;
    public void Ativar() => Ativo = true;
}
