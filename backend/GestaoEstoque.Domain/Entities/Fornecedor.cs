namespace GestaoEstoque.Domain.Entities;

public class Fornecedor
{
    public int Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string NomeNormalizado { get; private set; } = string.Empty;
    public string? Contato { get; private set; }
    public string? Email { get; private set; }
    public string? Telefone { get; private set; }
    public bool Ativo { get; private set; }

    protected Fornecedor() { }

    public Fornecedor(string nome, string? contato, string? email, string? telefone)
    {
        Alterar(nome, contato, email, telefone);
        Ativo = true;
    }

    public void Alterar(string nome, string? contato, string? email, string? telefone)
    {
        if (string.IsNullOrWhiteSpace(nome) || nome.Trim().Length > 150)
            throw new ArgumentException("Informe um nome de até 150 caracteres.");
        if (contato?.Trim().Length > 120 || email?.Trim().Length > 254 || telefone?.Trim().Length > 30)
            throw new ArgumentException("Contato, e-mail ou telefone excede o tamanho permitido.");
        Nome = nome.Trim();
        NomeNormalizado = Nome.ToUpperInvariant();
        Contato = string.IsNullOrWhiteSpace(contato) ? null : contato.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        Telefone = string.IsNullOrWhiteSpace(telefone) ? null : telefone.Trim();
    }

    public void Ativar() => Ativo = true;
    public void Desativar() => Ativo = false;
}
