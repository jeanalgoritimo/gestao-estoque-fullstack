namespace GestaoEstoque.Domain.Entities;

public enum SituacaoInventario { Aberto = 1, Confirmado = 2, Cancelado = 3 }

// Uma contagem por produto. O saldo e a versão do produto são congelados na abertura.
public class InventarioFisico
{
    public long Id { get; private set; }
    public int ProdutoId { get; private set; }
    public int SaldoInicial { get; private set; }
    public byte[] VersaoProduto { get; private set; } = [];
    public int? QuantidadeContada { get; private set; }
    public SituacaoInventario Situacao { get; private set; }
    public DateTime AbertoUtc { get; private set; }
    public int AbertoPorId { get; private set; }
    public string AbertoPorNome { get; private set; } = string.Empty;
    public DateTime? EncerradoUtc { get; private set; }
    public int? EncerradoPorId { get; private set; }
    public string? EncerradoPorNome { get; private set; }
    public string? Motivo { get; private set; }
    public byte[] Versao { get; private set; } = [];

    public int? Diferenca => QuantidadeContada - SaldoInicial;

    protected InventarioFisico() { }

    public InventarioFisico(int produtoId, int saldoInicial, byte[] versaoProduto, int usuarioId, string usuarioNome)
    {
        if (produtoId <= 0 || saldoInicial < 0 || versaoProduto.Length == 0 || usuarioId <= 0 ||
            string.IsNullOrWhiteSpace(usuarioNome)) throw new ArgumentException("Dados da abertura inválidos.");
        ProdutoId = produtoId;
        SaldoInicial = saldoInicial;
        VersaoProduto = (byte[])versaoProduto.Clone();
        AbertoPorId = usuarioId;
        AbertoPorNome = usuarioNome.Trim();
        AbertoUtc = DateTime.UtcNow;
        Situacao = SituacaoInventario.Aberto;
    }

    public void RegistrarContagem(int quantidade)
    {
        if (Situacao != SituacaoInventario.Aberto) throw new InvalidOperationException("Contagem encerrada.");
        if (quantidade < 0) throw new ArgumentOutOfRangeException(nameof(quantidade));
        QuantidadeContada = quantidade;
    }

    public void Confirmar(string motivo, int usuarioId, string usuarioNome)
    {
        if (Situacao != SituacaoInventario.Aberto || QuantidadeContada is null)
            throw new InvalidOperationException("Registre a contagem antes de confirmar.");
        if (string.IsNullOrWhiteSpace(motivo) || motivo.Trim().Length > 150)
            throw new ArgumentException("Informe um motivo de até 150 caracteres.", nameof(motivo));
        Encerrar(SituacaoInventario.Confirmado, usuarioId, usuarioNome);
        Motivo = motivo.Trim();
    }

    public void Cancelar(int usuarioId, string usuarioNome) => Encerrar(SituacaoInventario.Cancelado, usuarioId, usuarioNome);

    private void Encerrar(SituacaoInventario situacao, int usuarioId, string usuarioNome)
    {
        if (Situacao != SituacaoInventario.Aberto) throw new InvalidOperationException("Contagem encerrada.");
        if (usuarioId <= 0 || string.IsNullOrWhiteSpace(usuarioNome)) throw new ArgumentException("Usuário inválido.");
        Situacao = situacao;
        EncerradoUtc = DateTime.UtcNow;
        EncerradoPorId = usuarioId;
        EncerradoPorNome = usuarioNome.Trim();
    }
}
