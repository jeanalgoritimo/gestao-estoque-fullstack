namespace GestaoEstoque.Domain.Entities;

public static class Perfis
{
    public const string Administrador = "Administrador";
    public const string Operador = "Operador";
}

public class PerfilAcesso
{
    public int Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string NomeNormalizado { get; private set; } = string.Empty;
    public bool Sistema { get; private set; }
    public bool Ativo { get; private set; }
    public bool GerenciarProdutos { get; private set; }
    public bool GerenciarCategorias { get; private set; }
    public bool MovimentarEstoque { get; private set; }
    public string VersaoSeguranca { get; private set; } = string.Empty;

    protected PerfilAcesso() { }

    public PerfilAcesso(string nome, bool gerenciarProdutos, bool gerenciarCategorias, bool movimentarEstoque, bool sistema = false)
    {
        Sistema = sistema;
        Atualizar(nome, gerenciarProdutos, gerenciarCategorias, movimentarEstoque);
        Ativo = true;
    }

    public void Atualizar(string nome, bool gerenciarProdutos, bool gerenciarCategorias, bool movimentarEstoque)
    {
        if (Sistema && Id != 0) throw new InvalidOperationException("Perfil do sistema não pode ser alterado.");
        if (string.IsNullOrWhiteSpace(nome) || nome.Trim().Length > 80)
            throw new ArgumentException("Nome do perfil deve ter até 80 caracteres.", nameof(nome));
        Nome = nome.Trim(); NomeNormalizado = Nome.ToUpperInvariant();
        GerenciarProdutos = gerenciarProdutos;
        GerenciarCategorias = gerenciarCategorias;
        MovimentarEstoque = movimentarEstoque;
        VersaoSeguranca = Guid.NewGuid().ToString("N");
    }

    public void DefinirAtivo(bool ativo)
    {
        if (Sistema) throw new InvalidOperationException("Perfil do sistema não pode ser desativado.");
        Ativo = ativo;
        VersaoSeguranca = Guid.NewGuid().ToString("N");
    }
}
