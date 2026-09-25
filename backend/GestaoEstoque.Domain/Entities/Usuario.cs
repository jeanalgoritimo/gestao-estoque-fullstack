namespace GestaoEstoque.Domain.Entities;

public static class Perfis
{
    public const string Administrador = "Administrador";
    public const string Operador = "Operador";
}

public class Usuario
{
    public int Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string SenhaHash { get; private set; } = string.Empty;
    public string Perfil { get; private set; } = string.Empty;
    public string VersaoSeguranca { get; private set; } = string.Empty;
    public bool Ativo { get; private set; }

    protected Usuario() { }

    public Usuario(string nome, string email, string senhaHash, string perfil)
    {
        if (string.IsNullOrWhiteSpace(nome) || nome.Trim().Length > 120) throw new ArgumentException("Nome inválido.");
        if (string.IsNullOrWhiteSpace(email) || email.Trim().Length > 254) throw new ArgumentException("E-mail inválido.");
        if (string.IsNullOrWhiteSpace(senhaHash)) throw new ArgumentException("Senha inválida.");
        if (perfil != Perfis.Administrador && perfil != Perfis.Operador) throw new ArgumentException("Perfil inválido.");
        Nome = nome.Trim(); Email = email.Trim().ToLowerInvariant(); SenhaHash = senhaHash;
        Perfil = perfil; VersaoSeguranca = Guid.NewGuid().ToString("N"); Ativo = true;
    }

    public void AlterarSenha(string senhaHash)
    {
        if (string.IsNullOrWhiteSpace(senhaHash)) throw new ArgumentException("Senha inválida.");
        SenhaHash = senhaHash;
        VersaoSeguranca = Guid.NewGuid().ToString("N");
    }

    public void Desativar() { Ativo = false; VersaoSeguranca = Guid.NewGuid().ToString("N"); }
    public void Ativar() { Ativo = true; VersaoSeguranca = Guid.NewGuid().ToString("N"); }
}
