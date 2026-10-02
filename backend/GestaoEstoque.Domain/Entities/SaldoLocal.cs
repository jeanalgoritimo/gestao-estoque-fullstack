namespace GestaoEstoque.Domain.Entities;

public class SaldoLocal
{
    public long Id { get; private set; }
    public int ProdutoId { get; private set; }
    public int? PosicaoEstoqueId { get; private set; }
    public PosicaoEstoque? PosicaoEstoque { get; private set; }
    public decimal Quantidade { get; private set; }
    public decimal Reservado { get; private set; }
    public decimal Disponivel => Quantidade - Reservado;
    public byte[] Versao { get; private set; } = [];
    protected SaldoLocal() { }
    public SaldoLocal(int produtoId, int? posicaoId, decimal quantidade = 0, decimal reservado = 0)
    {
        if (produtoId <= 0 || posicaoId <= 0) throw new ArgumentException("Produto ou posição inválidos.");
        QuantidadeEstoque.Validar(quantidade); QuantidadeEstoque.Validar(reservado);
        if (reservado > quantidade) throw new ArgumentException("Reserva maior que o saldo local.");
        ProdutoId = produtoId; PosicaoEstoqueId = posicaoId; Quantidade = quantidade; Reservado = reservado;
    }
    private static void Positiva(decimal quantidade)
    {
        QuantidadeEstoque.Validar(quantidade);
        if (quantidade == 0) throw new ArgumentException("Informe uma quantidade positiva.");
    }
    public void Entrar(decimal quantidade)
    {
        Positiva(quantidade); QuantidadeEstoque.Validar(Quantidade + quantidade); Quantidade += quantidade;
    }
    public void Sair(decimal quantidade)
    {
        Positiva(quantidade);
        if (quantidade > Disponivel) throw new InvalidOperationException("Saldo disponível insuficiente no local. Materiais reservados não podem ser movimentados.");
        Quantidade -= quantidade;
    }
    public void Reservar(decimal quantidade)
    {
        Positiva(quantidade);
        if (quantidade > Disponivel) throw new InvalidOperationException("Saldo local insuficiente para reserva.");
        Reservado += quantidade;
    }
    public void Liberar(decimal quantidade)
    {
        Positiva(quantidade);
        if (quantidade > Reservado) throw new InvalidOperationException("Reserva local insuficiente.");
        Reservado -= quantidade;
    }
    public void TransferirPara(SaldoLocal destino, decimal quantidade)
    {
        Positiva(quantidade);
        if (destino.ProdutoId != ProdutoId || destino.PosicaoEstoqueId == PosicaoEstoqueId)
            throw new ArgumentException("Informe locais distintos do mesmo produto.");
        if (quantidade > Disponivel) throw new InvalidOperationException("Transferência excede o saldo disponível da origem. Há materiais reservados.");
        QuantidadeEstoque.Validar(destino.Quantidade + quantidade);
        Sair(quantidade); destino.Entrar(quantidade);
    }
}

public class TransferenciaEstoque
{
    public long Id { get; private set; }
    public int ProdutoId { get; private set; }
    public int? OrigemId { get; private set; }
    public int DestinoId { get; private set; }
    public decimal Quantidade { get; private set; }
    public DateTime DataUtc { get; private set; }
    public int UsuarioId { get; private set; }
    public string UsuarioNome { get; private set; } = "";
    public string OrigemDescricao { get; private set; } = "";
    public string DestinoDescricao { get; private set; } = "";
    public string Motivo { get; private set; } = "";
    protected TransferenciaEstoque() { }
    public TransferenciaEstoque(int produtoId, int? origemId, int destinoId, decimal quantidade,
        int usuarioId, string usuarioNome, string motivo, string origemDescricao, string destinoDescricao)
    {
        QuantidadeEstoque.Validar(quantidade);
        if (produtoId <= 0 || origemId <= 0 || destinoId <= 0 || origemId == destinoId || quantidade == 0 || usuarioId <= 0 ||
            string.IsNullOrWhiteSpace(usuarioNome) || usuarioNome.Length > 120 || string.IsNullOrWhiteSpace(motivo) || motivo.Trim().Length > 150 ||
            string.IsNullOrWhiteSpace(origemDescricao) || origemDescricao.Length > 300 || string.IsNullOrWhiteSpace(destinoDescricao) || destinoDescricao.Length > 300)
            throw new ArgumentException("Informe produto, locais distintos, quantidade, responsável e motivo de até 150 caracteres.");
        ProdutoId = produtoId; OrigemId = origemId; DestinoId = destinoId; Quantidade = quantidade;
        UsuarioId = usuarioId; UsuarioNome = usuarioNome.Trim(); Motivo = motivo.Trim(); DataUtc = DateTime.UtcNow;
        OrigemDescricao = origemDescricao; DestinoDescricao = destinoDescricao;
    }
}
