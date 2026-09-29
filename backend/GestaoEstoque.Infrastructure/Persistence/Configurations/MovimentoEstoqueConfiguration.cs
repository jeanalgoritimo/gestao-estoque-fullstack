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
        builder.Property(m => m.DocumentoOrigem).HasMaxLength(100).IsRequired();
        builder.Property(m => m.Motivo).HasMaxLength(150).IsRequired();
        builder.Property(m => m.UsuarioNome).HasMaxLength(120).IsRequired();
        builder.Property(m => m.CustoUnitario).HasPrecision(18, 4);
        builder.HasOne<Usuario>().WithMany().HasForeignKey(m => m.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Produto>().WithMany().HasForeignKey(m => m.ProdutoId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(m => new { m.ProdutoId, m.DataUtc });
    }
}
