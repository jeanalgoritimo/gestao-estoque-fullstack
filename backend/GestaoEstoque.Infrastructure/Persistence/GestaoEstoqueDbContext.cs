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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(GestaoEstoqueDbContext).Assembly);
    }
}
