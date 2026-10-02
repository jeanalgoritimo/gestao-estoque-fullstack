using GestaoEstoque.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestaoEstoque.Infrastructure.Persistence.Configurations;

public class InventarioFisicoConfiguration : IEntityTypeConfiguration<InventarioFisico>
{
    public void Configure(EntityTypeBuilder<InventarioFisico> builder)
    {
        builder.Property(e => e.SaldoInicial).HasPrecision(13, 3);
        builder.Property(e => e.QuantidadeContada).HasPrecision(13, 3);
        builder.ToTable("InventariosFisicos");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Situacao).HasConversion<int>();
        builder.Property(i => i.VersaoProduto).IsRequired().HasMaxLength(8);
        builder.Property(i => i.Versao).IsRowVersion();
        builder.Property(i => i.AbertoPorNome).HasMaxLength(120).IsRequired();
        builder.Property(i => i.EncerradoPorNome).HasMaxLength(120);
        builder.Property(i => i.Motivo).HasMaxLength(150);
        builder.Ignore(i => i.Diferenca);
        builder.HasOne<Produto>().WithMany().HasForeignKey(i => i.ProdutoId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Usuario>().WithMany().HasForeignKey(i => i.AbertoPorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Usuario>().WithMany().HasForeignKey(i => i.EncerradoPorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(i => i.ProdutoId).IsUnique().HasFilter("[Situacao] = 1");
    }
}
