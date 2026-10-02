namespace GestaoEstoque.Domain.Entities;

public enum SituacaoRequisicao { Rascunho = 1, Pendente = 2, Aprovada = 3, ParcialmenteAtendida = 4, Atendida = 5, Cancelada = 6 }

public class RequisicaoMaterial
{
    public long Id { get; private set; }
    public int? CentroCustoId { get; private set; }
    public CentroCusto? CentroCusto { get; private set; }
    public string Finalidade { get; private set; } = "";
    public int SolicitanteId { get; private set; }
    public string SolicitanteNome { get; private set; } = "";
    public DateTime CriadoUtc { get; private set; }
    public SituacaoRequisicao Situacao { get; private set; }
    public DateTime? AprovadoUtc { get; private set; }
    public int? AprovadoPorId { get; private set; }
    public string? AprovadoPorNome { get; private set; }
    public DateTime? EncerradoUtc { get; private set; }
    public int? EncerradoPorId { get; private set; }
    public string? EncerradoPorNome { get; private set; }
    public string? MotivoCancelamento { get; private set; }
    public byte[] Versao { get; private set; } = [];
    public List<ItemRequisicaoMaterial> Itens { get; private set; } = [];
    protected RequisicaoMaterial() { }
    public RequisicaoMaterial(string finalidade, int solicitanteId, string nome, int? centroCustoId = null)
    {
        if (string.IsNullOrWhiteSpace(finalidade) || finalidade.Trim().Length > 300)
            throw new ArgumentException("Informe a finalidade com até 300 caracteres.");
        ValidarUsuario(solicitanteId, nome);
        Finalidade = finalidade.Trim(); SolicitanteId = solicitanteId; SolicitanteNome = nome.Trim();
        CriadoUtc = DateTime.UtcNow; Situacao = SituacaoRequisicao.Rascunho;
        AlterarCentroCusto(centroCustoId);
    }
    private static void ValidarUsuario(int id, string nome)
    {
        if (id <= 0 || string.IsNullOrWhiteSpace(nome) || nome.Trim().Length > 120)
            throw new ArgumentException("Responsável inválido.");
    }
    public void AdicionarItem(int produtoId, decimal quantidade)
    {
        if (quantidade >= 0) QuantidadeEstoque.Validar(quantidade);
        if (Situacao != SituacaoRequisicao.Rascunho) throw new InvalidOperationException("Requisição já enviada.");
        if (Itens.Count >= 100 || Itens.Any(i => i.ProdutoId == produtoId))
            throw new ArgumentException("Informe até 100 produtos distintos.");
        Itens.Add(new ItemRequisicaoMaterial(produtoId, quantidade));
    }
    public void AlterarCentroCusto(int? centroCustoId)
    {
        if (Situacao != SituacaoRequisicao.Rascunho)
            throw new InvalidOperationException("O centro de custo só pode ser alterado no rascunho.");
        if (centroCustoId <= 0) throw new ArgumentException("Centro de custo inválido.");
        CentroCustoId = centroCustoId;
    }
    public void AtualizarRascunho(string finalidade, IReadOnlyCollection<(int ProdutoId, decimal Quantidade)> itens)
    {
        if (Situacao != SituacaoRequisicao.Rascunho)
            throw new InvalidOperationException("Somente rascunhos podem ser editados.");
        if (string.IsNullOrWhiteSpace(finalidade) || finalidade.Trim().Length > 300)
            throw new ArgumentException("Informe a finalidade com até 300 caracteres.");
        if (itens is null || itens.Count is < 1 or > 100 ||
            itens.Select(i => i.ProdutoId).Distinct().Count() != itens.Count)
            throw new ArgumentException("Informe de 1 a 100 produtos distintos.");
        // Valida tudo antes de modificar o agregado e preserva IDs dos itens mantidos.
        var novos = itens.Select(i => new ItemRequisicaoMaterial(i.ProdutoId, i.Quantidade)).ToList();
        Finalidade = finalidade.Trim();
        Itens.RemoveAll(i => !novos.Any(n => n.ProdutoId == i.ProdutoId));
        foreach (var novo in novos)
        {
            var existente = Itens.SingleOrDefault(i => i.ProdutoId == novo.ProdutoId);
            if (existente is null) Itens.Add(novo);
            else existente.AlterarQuantidadeRascunho(novo.Quantidade);
        }
    }
    public void Enviar()
    {
        if (Situacao != SituacaoRequisicao.Rascunho || Itens.Count == 0)
            throw new InvalidOperationException("Somente rascunhos com itens podem ser enviados.");
        Situacao = SituacaoRequisicao.Pendente;
    }
    public void Aprovar(int usuarioId, string nome)
    {
        if (Situacao != SituacaoRequisicao.Pendente) throw new InvalidOperationException("Requisição não está pendente.");
        ValidarUsuario(usuarioId, nome);
        Situacao = SituacaoRequisicao.Aprovada; AprovadoUtc = DateTime.UtcNow;
        AprovadoPorId = usuarioId; AprovadoPorNome = nome.Trim();
    }
    public void Entregar(int produtoId, decimal quantidade, int usuarioId, string nome)
    {
        if (quantidade >= 0) QuantidadeEstoque.Validar(quantidade);
        if (Situacao is not (SituacaoRequisicao.Aprovada or SituacaoRequisicao.ParcialmenteAtendida))
            throw new InvalidOperationException("Requisição não permite entregas.");
        ValidarUsuario(usuarioId, nome);
        var item = Itens.SingleOrDefault(i => i.ProdutoId == produtoId)
            ?? throw new ArgumentException("Produto não pertence à requisição.");
        item.Entregar(quantidade);
        Situacao = Itens.All(i => i.QuantidadeEntregue == i.Quantidade)
            ? SituacaoRequisicao.Atendida : SituacaoRequisicao.ParcialmenteAtendida;
        if (Situacao == SituacaoRequisicao.Atendida)
        { EncerradoUtc = DateTime.UtcNow; EncerradoPorId = usuarioId; EncerradoPorNome = nome.Trim(); }
    }
    public void RegistrarDevolucao(int produtoId, decimal quantidade)
    {
        if (quantidade >= 0) QuantidadeEstoque.Validar(quantidade);
        if (Situacao is not (SituacaoRequisicao.ParcialmenteAtendida or SituacaoRequisicao.Atendida or SituacaoRequisicao.Cancelada))
            throw new InvalidOperationException("Somente materiais já entregues podem ser devolvidos.");
        var item = Itens.SingleOrDefault(i => i.ProdutoId == produtoId)
            ?? throw new ArgumentException("Produto não pertence à requisição.");
        item.Devolver(quantidade);
        // Devolução preserva a entrega original e não reabre o atendimento ou a reserva.
    }
    public void Cancelar(int usuarioId, string nome, string motivo)
    {
        if (Situacao is SituacaoRequisicao.Atendida or SituacaoRequisicao.Cancelada)
            throw new InvalidOperationException("Requisição já encerrada.");
        ValidarUsuario(usuarioId, nome);
        if (string.IsNullOrWhiteSpace(motivo) || motivo.Trim().Length > 300)
            throw new ArgumentException("Informe o motivo do cancelamento com até 300 caracteres.");
        Situacao = SituacaoRequisicao.Cancelada; MotivoCancelamento = motivo.Trim();
        EncerradoUtc = DateTime.UtcNow; EncerradoPorId = usuarioId; EncerradoPorNome = nome.Trim();
    }
}

public class ItemRequisicaoMaterial
{
    public long Id { get; private set; }
    public long RequisicaoMaterialId { get; private set; }
    public int ProdutoId { get; private set; }
    public Produto Produto { get; private set; } = null!;
    public decimal Quantidade { get; private set; }
    public decimal QuantidadeEntregue { get; private set; }
    public decimal QuantidadeDevolvida { get; private set; }
    protected ItemRequisicaoMaterial() { }
    public ItemRequisicaoMaterial(int produtoId, decimal quantidade)
    {
        if (quantidade >= 0) QuantidadeEstoque.Validar(quantidade);
        if (produtoId <= 0 || quantidade <= 0) throw new ArgumentException("Produto e quantidade devem ser positivos.");
        ProdutoId = produtoId; Quantidade = quantidade;
    }
    internal void AlterarQuantidadeRascunho(decimal quantidade)
    {
        if (quantidade >= 0) QuantidadeEstoque.Validar(quantidade);
        if (quantidade <= 0 || QuantidadeEntregue != 0)
            throw new ArgumentException("Quantidade de rascunho inválida.");
        Quantidade = quantidade;
    }
    internal void Devolver(decimal quantidade)
    {
        if (quantidade >= 0) QuantidadeEstoque.Validar(quantidade);
        if (quantidade <= 0 || quantidade > QuantidadeEntregue - QuantidadeDevolvida)
            throw new ArgumentException("Quantidade de devolução excede o saldo entregue ainda não devolvido.");
        QuantidadeDevolvida += quantidade;
    }
    internal void Entregar(decimal quantidade)
    {
        if (quantidade >= 0) QuantidadeEstoque.Validar(quantidade);
        if (quantidade <= 0 || quantidade > Quantidade - QuantidadeEntregue)
            throw new ArgumentException("Quantidade de entrega excede o saldo pendente.");
        QuantidadeEntregue += quantidade;
    }
}
