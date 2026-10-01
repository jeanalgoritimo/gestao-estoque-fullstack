namespace GestaoEstoque.Domain.Entities;

public class CentroCusto
{
    public int Id { get; private set; }
    public string Nome { get; private set; } = "";
    public string NomeNormalizado { get; private set; } = "";
    public bool Ativo { get; private set; } = true;
    protected CentroCusto() { }
    public CentroCusto(string nome) => AlterarNome(nome);
    public void AlterarNome(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome) || nome.Trim().Length > 100)
            throw new ArgumentException("Informe o nome do centro de custo com até 100 caracteres.");
        Nome = nome.Trim(); NomeNormalizado = Nome.ToUpperInvariant();
    }
    public void DefinirAtivo(bool ativo) => Ativo = ativo;
}
