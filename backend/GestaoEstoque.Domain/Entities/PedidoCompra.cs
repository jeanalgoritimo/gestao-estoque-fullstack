namespace GestaoEstoque.Domain.Entities;

public enum SituacaoPedidoCompra { Aberto = 1, Recebido = 2, Cancelado = 3 }

public class PedidoCompra
{
    public long Id { get; private set; }
    public int FornecedorId { get; private set; }
    public Fornecedor Fornecedor { get; private set; } = null!;
    public SituacaoPedidoCompra Situacao { get; private set; } = SituacaoPedidoCompra.Aberto;
    public DateTime CriadoUtc { get; private set; } = DateTime.UtcNow;
    public int CriadoPorId { get; private set; }
    public string CriadoPorNome { get; private set; } = string.Empty;
    public DateTime? EncerradoUtc { get; private set; }
    public int? EncerradoPorId { get; private set; }
    public string? EncerradoPorNome { get; private set; }
    public byte[] Versao { get; private set; } = [];
    public List<ItemPedidoCompra> Itens { get; private set; } = [];

    protected PedidoCompra() { }
    public PedidoCompra(int fornecedorId, int usuarioId, string nome)
    {
        if (fornecedorId <= 0 || usuarioId <= 0 || string.IsNullOrWhiteSpace(nome) || nome.Length > 120)
            throw new ArgumentException("Fornecedor ou usuário inválido.");
        FornecedorId = fornecedorId;
        CriadoPorId = usuarioId;
        CriadoPorNome = nome.Trim();
    }
    public void AdicionarItem(int produtoId, int quantidade)
    {
        if (Situacao != SituacaoPedidoCompra.Aberto) throw new InvalidOperationException("Pedido encerrado.");
        if (produtoId <= 0 || quantidade <= 0) throw new ArgumentException("Produto ou quantidade inválida.");
        if (Itens.Any(i => i.ProdutoId == produtoId)) throw new ArgumentException("Produto repetido no pedido.");
        Itens.Add(new ItemPedidoCompra(produtoId, quantidade));
    }
    public void Encerrar(SituacaoPedidoCompra situacao, int usuarioId, string nome)
    {
        if (Situacao != SituacaoPedidoCompra.Aberto) throw new InvalidOperationException("Pedido já encerrado.");
        if (situacao is not (SituacaoPedidoCompra.Recebido or SituacaoPedidoCompra.Cancelado) ||
            usuarioId <= 0 || string.IsNullOrWhiteSpace(nome) || nome.Length > 120)
            throw new ArgumentException("Situação ou usuário inválido.");
        Situacao = situacao;
        EncerradoUtc = DateTime.UtcNow;
        EncerradoPorId = usuarioId;
        EncerradoPorNome = nome.Trim();
    }
}

public class ItemPedidoCompra
{
    public long Id { get; private set; }
    public long PedidoCompraId { get; private set; }
    public int ProdutoId { get; private set; }
    public Produto Produto { get; private set; } = null!;
    public int Quantidade { get; private set; }
    protected ItemPedidoCompra() { }
    public ItemPedidoCompra(int produtoId, int quantidade)
    {
        if (produtoId <= 0 || quantidade <= 0) throw new ArgumentException("Item inválido.");
        ProdutoId = produtoId;
        Quantidade = quantidade;
    }
}
