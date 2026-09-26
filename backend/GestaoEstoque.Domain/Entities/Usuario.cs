namespace GestaoEstoque.Domain.Entities;

public class Usuario
{
    public int Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string SenhaHash { get; private set; } = string.Empty;
    public int PerfilId { get; private set; }
    public PerfilAcesso? PerfilAcesso { get; private set; }
    public string VersaoSeguranca { get; private set; } = string.Empty;
    public bool Ativo { get; private set; }

    protected Usuario() { }

    public Usuario(string nome, string email, string senhaHash, int perfilId)
    {
        if (string.IsNullOrWhiteSpace(nome) || nome.Trim().Length > 120) throw new ArgumentException("Nome inválido.");
        if (string.IsNullOrWhiteSpace(email) || email.Trim().Length > 254) throw new ArgumentException("E-mail inválido.");
        if (string.IsNullOrWhiteSpace(senhaHash)) throw new ArgumentException("Senha inválida.");
        if (perfilId <= 0) throw new ArgumentException("Perfil inválido.", nameof(perfilId));
        Nome = nome.Trim(); Email = email.Trim().ToLowerInvariant(); SenhaHash = senhaHash;
        PerfilId = perfilId; VersaoSeguranca = Guid.NewGuid().ToString("N"); Ativo = true;
    }

    public void AlterarSenha(string senhaHash)
    {
        if (string.IsNullOrWhiteSpace(senhaHash)) throw new ArgumentException("Senha inválida.");
        SenhaHash = senhaHash;
        VersaoSeguranca = Guid.NewGuid().ToString("N");
    }

    public void AlterarPerfil(int perfilId)
    {
        if (perfilId <= 0) throw new ArgumentException("Perfil inválido.", nameof(perfilId));
        PerfilId = perfilId;
        VersaoSeguranca = Guid.NewGuid().ToString("N");
    }

    public void Desativar() { Ativo = false; VersaoSeguranca = Guid.NewGuid().ToString("N"); }
    public void Ativar() { Ativo = true; VersaoSeguranca = Guid.NewGuid().ToString("N"); }
}
