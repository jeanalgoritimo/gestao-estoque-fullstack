namespace GestaoEstoque.Domain.Entities;

public enum TipoMovimento { Entrada = 1, Saida = 2 }

public class MovimentoEstoque
{
    public long Id { get; private set; }
    public int ProdutoId { get; private set; }
    public TipoMovimento Tipo { get; private set; }
    public decimal Quantidade { get; private set; }
    public DateTime DataUtc { get; private set; }
    public string? Observacao { get; private set; }
    public DateTime DataEfetivaUtc { get; private set; }
    public string DocumentoOrigem { get; private set; } = string.Empty;
    public string Motivo { get; private set; } = string.Empty;
    public int? UsuarioId { get; private set; }
    public string UsuarioNome { get; private set; } = string.Empty;
    public decimal? CustoUnitario { get; private set; }
    public decimal? SaldoApos { get; private set; }
    public long? PedidoCompraId { get; private set; }
    public long? RequisicaoMaterialId { get; private set; }
    public Guid? DevolucaoId { get; private set; }
    public Guid? EntregaId { get; private set; }
    public Guid? RecebimentoId { get; private set; }

    protected MovimentoEstoque() { }

    public MovimentoEstoque(int produtoId, TipoMovimento tipo, decimal quantidade, string? observacao)
    {
        if (quantidade >= 0) QuantidadeEstoque.Validar(quantidade);
        if (produtoId <= 0) throw new ArgumentOutOfRangeException(nameof(produtoId));
        if (!Enum.IsDefined(tipo)) throw new ArgumentOutOfRangeException(nameof(tipo));
        if (quantidade <= 0) throw new ArgumentOutOfRangeException(nameof(quantidade));
        if (observacao?.Length > 300) throw new ArgumentException("Observação limitada a 300 caracteres.", nameof(observacao));
        ProdutoId = produtoId;
        Tipo = tipo;
        Quantidade = quantidade;
        Observacao = observacao?.Trim();
        DataUtc = DateTime.UtcNow;
        DataEfetivaUtc = DataUtc;
    }

    public MovimentoEstoque(int produtoId, TipoMovimento tipo, decimal quantidade, string? observacao,
        DateTime dataEfetivaUtc, string documentoOrigem, string motivo, int usuarioId,
        string usuarioNome, decimal? custoUnitario, decimal saldoApos)
        : this(produtoId, tipo, quantidade, observacao)
    {
        if (dataEfetivaUtc.Kind != DateTimeKind.Utc || dataEfetivaUtc > DataUtc.AddMinutes(1))
            throw new ArgumentException("A data efetiva deve estar em UTC e não pode ser futura.", nameof(dataEfetivaUtc));
        if (string.IsNullOrWhiteSpace(motivo) || motivo.Trim().Length > 150)
            throw new ArgumentException("Informe um motivo de até 150 caracteres.", nameof(motivo));
        if (documentoOrigem?.Trim().Length > 100)
            throw new ArgumentException("Documento limitado a 100 caracteres.", nameof(documentoOrigem));
        if (usuarioId <= 0 || string.IsNullOrWhiteSpace(usuarioNome) || usuarioNome.Length > 120)
            throw new ArgumentException("Usuário responsável inválido.", nameof(usuarioId));
        if (custoUnitario is < 0 or > 999999999999.9999m)
            throw new ArgumentOutOfRangeException(nameof(custoUnitario));
        if (saldoApos < 0)
            throw new ArgumentOutOfRangeException(nameof(saldoApos));
        DataEfetivaUtc = dataEfetivaUtc;
        DocumentoOrigem = documentoOrigem?.Trim() ?? string.Empty;
        Motivo = motivo.Trim();
        UsuarioId = usuarioId;
        UsuarioNome = usuarioNome.Trim();
        CustoUnitario = custoUnitario;
        QuantidadeEstoque.Validar(saldoApos);
        SaldoApos = saldoApos;
    }

    public void VincularEntrega(long requisicaoId, Guid entregaId)
    {
        if (Tipo != TipoMovimento.Saida || requisicaoId <= 0 || entregaId == Guid.Empty || RequisicaoMaterialId is not null)
            throw new InvalidOperationException("Vínculo de entrega inválido.");
        RequisicaoMaterialId = requisicaoId; EntregaId = entregaId;
    }
    public void VincularDevolucao(long requisicaoId, Guid devolucaoId)
    {
        if (Tipo != TipoMovimento.Entrada || requisicaoId <= 0 || devolucaoId == Guid.Empty ||
            RequisicaoMaterialId is not null || PedidoCompraId is not null || RecebimentoId is not null)
            throw new InvalidOperationException("Vínculo de devolução inválido.");
        RequisicaoMaterialId = requisicaoId; DevolucaoId = devolucaoId;
    }
    public void VincularRecebimento(long pedidoCompraId, Guid recebimentoId)
    {
        if (Tipo != TipoMovimento.Entrada || pedidoCompraId <= 0 || recebimentoId == Guid.Empty ||
            PedidoCompraId is not null || RecebimentoId is not null || RequisicaoMaterialId is not null)
            throw new InvalidOperationException("Vínculo de recebimento inválido.");
        PedidoCompraId = pedidoCompraId;
        RecebimentoId = recebimentoId;
    }
}
