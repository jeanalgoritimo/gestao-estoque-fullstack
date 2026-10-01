namespace GestaoEstoque.Domain.Entities;

public class Almoxarifado
{
    public int Id { get; private set; }
    public string Nome { get; private set; } = "";
    public string NomeNormalizado { get; private set; } = "";
    public bool Ativo { get; private set; } = true;
    protected Almoxarifado() { }
    public Almoxarifado(string nome) => AlterarNome(nome);
    public void AlterarNome(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome) || nome.Trim().Length > 100) throw new ArgumentException("Informe o nome do almoxarifado com até 100 caracteres.");
        Nome = nome.Trim(); NomeNormalizado = Nome.ToUpperInvariant();
    }
    public void DefinirAtivo(bool ativo) => Ativo = ativo;
}

public class PosicaoEstoque
{
    public int Id { get; private set; }
    public int AlmoxarifadoId { get; private set; }
    public Almoxarifado Almoxarifado { get; private set; } = null!;
    public string Corredor { get; private set; } = "";
    public string Estante { get; private set; } = "";
    public string Prateleira { get; private set; } = "";
    public string CorredorNormalizado { get; private set; } = "";
    public string EstanteNormalizada { get; private set; } = "";
    public string PrateleiraNormalizada { get; private set; } = "";
    public bool Ativo { get; private set; } = true;
    protected PosicaoEstoque() { }
    public PosicaoEstoque(int almoxarifadoId, string corredor, string estante, string prateleira)
    {
        if (almoxarifadoId <= 0) throw new ArgumentException("Almoxarifado inválido.");
        AlmoxarifadoId = almoxarifadoId; AlterarEndereco(corredor, estante, prateleira);
    }
    private static string Validar(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor) || valor.Trim().Length > 40) throw new ArgumentException("Informe corredor, estante e prateleira com até 40 caracteres cada.");
        return valor.Trim();
    }
    public void AlterarEndereco(string corredor, string estante, string prateleira)
    {
        var c = Validar(corredor); var e = Validar(estante); var p = Validar(prateleira);
        Corredor = c; Estante = e; Prateleira = p;
        CorredorNormalizado = c.ToUpperInvariant(); EstanteNormalizada = e.ToUpperInvariant(); PrateleiraNormalizada = p.ToUpperInvariant();
    }
    public void DefinirAtivo(bool ativo) => Ativo = ativo;
}
