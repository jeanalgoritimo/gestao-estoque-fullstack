namespace GestaoEstoque.Domain.Entities;

// Usa a posição padrão primeiro e depois uma ordem estável. Reservas ficam protegidas em cada local.
public static class DistribuicaoEstoque
{
    public static void Sincronizar(ICollection<SaldoLocal> locais, int produtoId, int? padrao,
        decimal saldoAnterior, decimal reservaAnterior, decimal saldoAtual, decimal reservaAtual)
    {
        if (locais.Any(l => l.ProdutoId != produtoId) || locais.Sum(l => l.Quantidade) != saldoAnterior || locais.Sum(l => l.Reservado) != reservaAnterior)
            throw new InvalidOperationException("Os saldos locais divergem do saldo total. Recarregue ou revise os dados.");
        QuantidadeEstoque.Validar(saldoAtual); QuantidadeEstoque.Validar(reservaAtual);
        if (reservaAtual > saldoAtual) throw new InvalidOperationException("Reserva maior que o saldo.");
        var ordem = locais.OrderByDescending(l => l.PosicaoEstoqueId == padrao).ThenBy(l => l.PosicaoEstoqueId).ToList();
        var liberar = Math.Max(0, reservaAnterior - reservaAtual);
        foreach (var l in ordem)
        {
            var q = Math.Min(l.Reservado, liberar); if (q > 0) l.Liberar(q); liberar -= q;
        }
        var entrada = saldoAtual - saldoAnterior;
        if (entrada > 0)
        {
            var destino = locais.SingleOrDefault(l => l.PosicaoEstoqueId == padrao);
            if (destino is null) { destino = new SaldoLocal(produtoId, padrao); locais.Add(destino); ordem.Insert(0, destino); }
            destino.Entrar(entrada);
        }
        var saida = Math.Max(0, -entrada);
        foreach (var l in ordem)
        {
            var q = Math.Min(l.Disponivel, saida); if (q > 0) l.Sair(q); saida -= q;
        }
        var reservar = Math.Max(0, reservaAtual - reservaAnterior);
        foreach (var l in ordem)
        {
            var q = Math.Min(l.Disponivel, reservar); if (q > 0) l.Reservar(q); reservar -= q;
        }
        if (liberar != 0 || saida != 0 || reservar != 0)
            throw new InvalidOperationException("Não há saldo local suficiente para concluir a operação.");
    }
}
