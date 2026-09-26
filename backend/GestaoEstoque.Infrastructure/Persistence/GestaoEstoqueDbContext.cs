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

    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<MovimentoEstoque> MovimentosEstoque => Set<MovimentoEstoque>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<CategoriaProduto> CategoriasProduto => Set<CategoriaProduto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(GestaoEstoqueDbContext).Assembly);
    }
}
