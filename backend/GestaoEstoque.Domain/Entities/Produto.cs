namespace GestaoEstoque.Domain.Entities;

public class Produto
{
    public int Id { get; private set; }

    public string Nome { get; private set; } = string.Empty;

    public int CategoriaId { get; private set; }
    public CategoriaProduto? CategoriaProduto { get; private set; }

    public int? FornecedorId { get; private set; }
    public Fornecedor? Fornecedor { get; private set; }

    public int UnidadeMedidaId { get; private set; } = 1;
    public UnidadeMedida UnidadeMedida { get; private set; } = null!;
    public void AlterarUnidade(int id, bool possuiHistorico = false)
    {
        if (id <= 0) throw new ArgumentException("Unidade inválida.");
        if (id != UnidadeMedidaId && (Estoque != 0 || EstoqueReservado != 0 || possuiHistorico))
            throw new InvalidOperationException("Não é possível trocar a unidade de produto com saldo, reservas ou registros de operações.");
        UnidadeMedidaId = id;
    }

    public int? PosicaoEstoqueId { get; private set; }
    public PosicaoEstoque? PosicaoEstoque { get; private set; }

    public void AlterarPosicao(int? id)
    {
        if (id <= 0) throw new ArgumentException("Posição inválida.");
        PosicaoEstoqueId = id;
    }

    public decimal Preco { get; private set; }

    public int Estoque { get; private set; }
    public int EstoqueReservado { get; private set; }
    public int EstoqueDisponivel => Estoque - EstoqueReservado;
    public int EstoqueMinimo { get; private set; }
    public byte[] Versao { get; private set; } = [];

    public bool Ativo { get; private set; }

    protected Produto()
    {
    }

    public Produto(
        string nome,
        int categoriaId,
        decimal preco,
        int estoqueMinimo = 5)
    {
        AlterarNome(nome);
        AlterarCategoria(categoriaId);
        AlterarPreco(preco);
        AlterarEstoqueMinimo(estoqueMinimo);

        Ativo = true;
    }

    public void AlterarNome(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException(
                "O nome do produto é obrigatório.",
                nameof(nome));

        Nome = nome.Trim();
    }

    public void AlterarCategoria(int categoriaId)
    {
        if (categoriaId <= 0)
            throw new ArgumentException(
                "A categoria é obrigatória.",
                nameof(categoriaId));

        CategoriaId = categoriaId;
    }

    public void AlterarFornecedor(int? fornecedorId)
    {
        if (fornecedorId <= 0) throw new ArgumentException("Fornecedor inválido.");
        FornecedorId = fornecedorId;
    }

    public void AlterarPreco(decimal preco)
    {
        if (preco <= 0)
            throw new ArgumentException(
                "O preço deve ser maior que zero.",
                nameof(preco));

        Preco = preco;
    }

    public void AlterarEstoqueMinimo(int estoqueMinimo)
    {
        if (estoqueMinimo < 0)
            throw new ArgumentException(
                "O estoque mínimo não pode ser negativo.",
                nameof(estoqueMinimo));

        EstoqueMinimo = estoqueMinimo;
    }

    public void RegistrarEntrada(int quantidade)
    {
        if (quantidade <= 0) throw new ArgumentOutOfRangeException(nameof(quantidade));
        Estoque = checked(Estoque + quantidade);
    }

    public void RegistrarSaida(int quantidade)
    {
        if (quantidade <= 0) throw new ArgumentOutOfRangeException(nameof(quantidade));
        if (quantidade > EstoqueDisponivel) throw new InvalidOperationException("Saldo disponível insuficiente. Há estoque reservado para requisições.");
        Estoque -= quantidade;
    }

    public void Reservar(int quantidade)
    {
        if (!Ativo) throw new InvalidOperationException("Produto inativo.");
        if (quantidade <= 0) throw new ArgumentOutOfRangeException(nameof(quantidade));
        if (quantidade > EstoqueDisponivel) throw new InvalidOperationException("Saldo disponível insuficiente para reserva.");
        EstoqueReservado = checked(EstoqueReservado + quantidade);
    }
    public void LiberarReserva(int quantidade)
    {
        if (quantidade <= 0 || quantidade > EstoqueReservado)
            throw new InvalidOperationException("Quantidade de reserva inválida.");
        EstoqueReservado -= quantidade;
    }
    public void EntregarReserva(int quantidade)
    {
        if (!Ativo) throw new InvalidOperationException("Produto inativo.");
        LiberarReserva(quantidade);
        RegistrarSaida(quantidade);
    }
    public void Ativar()
    {
        Ativo = true;
    }

    public void Desativar()
    {
        if (EstoqueReservado > 0) throw new InvalidOperationException("Libere as reservas antes de desativar o produto.");
        Ativo = false;
    }

    public bool EstaComEstoqueBaixo()
    {
        return Estoque <= EstoqueMinimo;
    }
}
