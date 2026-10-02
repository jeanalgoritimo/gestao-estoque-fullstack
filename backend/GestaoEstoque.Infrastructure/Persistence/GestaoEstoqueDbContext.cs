using GestaoEstoque.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GestaoEstoque.Infrastructure.Persistence;

public class GestaoEstoqueDbContext : DbContext
{
    public GestaoEstoqueDbContext(
        DbContextOptions<GestaoEstoqueDbContext> options)
        : base(options)
    {
    }

    public DbSet<SaldoLocal> SaldosLocais => Set<SaldoLocal>();
    public DbSet<TransferenciaEstoque> TransferenciasEstoque => Set<TransferenciaEstoque>();

    public DbSet<UnidadeMedida> UnidadesMedida => Set<UnidadeMedida>();
    public DbSet<Almoxarifado> Almoxarifados => Set<Almoxarifado>();
    public DbSet<PosicaoEstoque> PosicoesEstoque => Set<PosicaoEstoque>();
    public DbSet<CentroCusto> CentrosCusto => Set<CentroCusto>();
    public DbSet<RequisicaoMaterial> RequisicoesMaterial => Set<RequisicaoMaterial>();
    public DbSet<ItemRequisicaoMaterial> ItensRequisicaoMaterial => Set<ItemRequisicaoMaterial>();
    public DbSet<PedidoCompra> PedidosCompra => Set<PedidoCompra>();
    public DbSet<ItemPedidoCompra> ItensPedidoCompra => Set<ItemPedidoCompra>();
    public DbSet<Fornecedor> Fornecedores => Set<Fornecedor>();
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<MovimentoEstoque> MovimentosEstoque => Set<MovimentoEstoque>();
    public DbSet<InventarioFisico> InventariosFisicos => Set<InventarioFisico>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<CategoriaProduto> CategoriasProduto => Set<CategoriaProduto>();
    public DbSet<PerfilAcesso> PerfisAcesso => Set<PerfilAcesso>();

    private async Task SincronizarLocais(CancellationToken ct)
    {
        ChangeTracker.DetectChanges();
        if (ChangeTracker.Entries<TransferenciaEstoque>().Any(e => e.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Transferências registradas são imutáveis. Registre outra transferência para corrigir os locais.");
        var alterados = ChangeTracker.Entries<Produto>().Where(e => e.State == EntityState.Modified &&
            (e.Property(p => p.Estoque).OriginalValue != e.Entity.Estoque ||
             e.Property(p => p.EstoqueReservado).OriginalValue != e.Entity.EstoqueReservado)).ToList();
        foreach (var e in alterados)
        {
            var p = e.Entity;
            var locais = await SaldosLocais.Where(s => s.ProdutoId == p.Id).ToListAsync(ct);
            if (p.Estoque > e.Property(x => x.Estoque).OriginalValue && p.PosicaoEstoqueId is { } posicao &&
                !await PosicoesEstoque.AnyAsync(l => l.Id == posicao && l.Ativo && l.Almoxarifado.Ativo, ct))
                throw new InvalidOperationException("A posição padrão está inativa. Selecione uma posição ativa no produto antes de registrar entradas.");
            DistribuicaoEstoque.Sincronizar(locais, p.Id, p.PosicaoEstoqueId,
                e.Property(x => x.Estoque).OriginalValue, e.Property(x => x.EstoqueReservado).OriginalValue,
                p.Estoque, p.EstoqueReservado);
            foreach (var local in locais.Where(l => l.Id == 0)) SaldosLocais.Add(local);
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => SaveChangesAsync(true, cancellationToken);
    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ChangeTracker.DetectChanges();
        var sincronizar = ChangeTracker.Entries<Produto>().Any(e => e.State == EntityState.Modified &&
            (e.Property(p => p.Estoque).OriginalValue != e.Entity.Estoque || e.Property(p => p.EstoqueReservado).OriginalValue != e.Entity.EstoqueReservado));
        await using var tx = sincronizar && Database.CurrentTransaction is null
            ? await Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken) : null;
        await SincronizarLocais(cancellationToken);
        var resultado = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        if (tx is not null) await tx.CommitAsync(cancellationToken);
        return resultado;
    }
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ChangeTracker.DetectChanges();
        using var tx = Database.CurrentTransaction is null && ChangeTracker.Entries<Produto>().Any(e => e.State == EntityState.Modified &&
            (e.Property(p => p.Estoque).OriginalValue != e.Entity.Estoque || e.Property(p => p.EstoqueReservado).OriginalValue != e.Entity.EstoqueReservado))
            ? Database.BeginTransaction(System.Data.IsolationLevel.Serializable) : null;
        SincronizarLocais(CancellationToken.None).GetAwaiter().GetResult();
        var resultado = base.SaveChanges(acceptAllChangesOnSuccess);
        tx?.Commit(); return resultado;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(GestaoEstoqueDbContext).Assembly);
    }
}
