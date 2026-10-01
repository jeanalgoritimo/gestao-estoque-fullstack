using GestaoEstoque.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestaoEstoque.Infrastructure.Persistence.Configurations;

public class RequisicaoMaterialConfiguration : IEntityTypeConfiguration<RequisicaoMaterial>
{
    public void Configure(EntityTypeBuilder<RequisicaoMaterial> b)
    {
        b.ToTable("RequisicoesMaterial"); b.HasKey(r => r.Id);
        b.Property(r => r.Finalidade).HasMaxLength(300).IsRequired();
        b.Property(r => r.SolicitanteNome).HasMaxLength(120).IsRequired();
        b.Property(r => r.AprovadoPorNome).HasMaxLength(120);
        b.Property(r => r.EncerradoPorNome).HasMaxLength(120);
        b.Property(r => r.MotivoCancelamento).HasMaxLength(300);
        b.Property(r => r.Versao).IsRowVersion();
        b.HasOne<Usuario>().WithMany().HasForeignKey(r => r.SolicitanteId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Usuario>().WithMany().HasForeignKey(r => r.AprovadoPorId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Usuario>().WithMany().HasForeignKey(r => r.EncerradoPorId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(r => r.Itens).WithOne().HasForeignKey(i => i.RequisicaoMaterialId).OnDelete(DeleteBehavior.Cascade);
    }
}
public class ItemRequisicaoMaterialConfiguration : IEntityTypeConfiguration<ItemRequisicaoMaterial>
{
    public void Configure(EntityTypeBuilder<ItemRequisicaoMaterial> b)
    {
        b.ToTable("ItensRequisicaoMaterial"); b.HasKey(i => i.Id);
        b.HasIndex(i => new { i.RequisicaoMaterialId, i.ProdutoId }).IsUnique();
        b.HasOne(i => i.Produto).WithMany().HasForeignKey(i => i.ProdutoId).OnDelete(DeleteBehavior.Restrict);
    }
}
