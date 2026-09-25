using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GestaoEstoque.Infrastructure.Persistence;

public class GestaoEstoqueDesignTimeDbContextFactory : IDesignTimeDbContextFactory<GestaoEstoqueDbContext>
{
    public GestaoEstoqueDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException("Defina ConnectionStrings__DefaultConnection para executar migrations.");
        return new GestaoEstoqueDbContext(new DbContextOptionsBuilder<GestaoEstoqueDbContext>()
            .UseSqlServer(connection).Options);
    }
}
