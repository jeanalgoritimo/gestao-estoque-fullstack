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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(GestaoEstoqueDbContext).Assembly);
    }
}