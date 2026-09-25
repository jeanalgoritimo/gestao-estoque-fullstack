using GestaoEstoque.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestaoEstoque.Infrastructure.Persistence.Configurations;

public class MovimentoEstoqueConfiguration : IEntityTypeConfiguration<MovimentoEstoque>
{
    public void Configure(EntityTypeBuilder<MovimentoEstoque> builder)
    {
        builder.ToTable("MovimentosEstoque");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Tipo).HasConversion<int>();
        builder.Property(m => m.Observacao).HasMaxLength(300);
        builder.HasOne<Produto>().WithMany().HasForeignKey(m => m.ProdutoId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(m => new { m.ProdutoId, m.DataUtc });
    }
}
