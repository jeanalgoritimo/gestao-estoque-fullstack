using System.Text.RegularExpressions;
namespace GestaoEstoque.Domain.Entities;

public class UnidadeMedida
{
    public int Id { get; private set; }
    public string Sigla { get; private set; } = "";
    public string Nome { get; private set; } = "";
    public bool Ativo { get; private set; } = true;
    public bool Sistema { get; private set; }
    protected UnidadeMedida() { }
    public UnidadeMedida(string sigla, string nome, bool sistema = false)
    {
        Alterar(sigla, nome, false); Sistema = sistema;
    }
    public void Alterar(string sigla, string nome, bool vinculada)
    {
        var s = sigla?.Trim().ToUpperInvariant() ?? "";
        if (!Regex.IsMatch(s, "^[A-Z][A-Z0-9]{0,9}$") || string.IsNullOrWhiteSpace(nome) || nome.Trim().Length > 80)
            throw new ArgumentException("Informe sigla de até 10 letras/números iniciando por letra e nome de até 80 caracteres.");
        if (Sigla.Length > 0 && s != Sigla && (vinculada || Sistema))
            throw new InvalidOperationException("A sigla de uma unidade em uso ou do sistema não pode mudar. Cadastre outra unidade.");
        Sigla = s; Nome = nome.Trim();
    }
    public void DefinirAtivo(bool ativo)
    {
        if (!ativo && Sistema) throw new InvalidOperationException("A unidade padrão do sistema não pode ser desativada.");
        Ativo = ativo;
    }
}
