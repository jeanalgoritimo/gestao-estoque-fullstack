using GestaoEstoque.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace GestaoEstoque.Infrastructure.Persistence.Configurations;

public class AlmoxarifadoConfiguration : IEntityTypeConfiguration<Almoxarifado>
{
    public void Configure(EntityTypeBuilder<Almoxarifado> b)
    {
        b.ToTable("Almoxarifados"); b.HasKey(x => x.Id);
        b.Property(x => x.Nome).HasMaxLength(100).IsRequired();
        b.Property(x => x.NomeNormalizado).HasMaxLength(100).IsRequired();
        b.HasIndex(x => x.NomeNormalizado).IsUnique();
    }
}
public class PosicaoEstoqueConfiguration : IEntityTypeConfiguration<PosicaoEstoque>
{
    public void Configure(EntityTypeBuilder<PosicaoEstoque> b)
    {
        b.ToTable("PosicoesEstoque"); b.HasKey(x => x.Id);
        b.Property(x => x.Corredor).HasMaxLength(40).IsRequired();
        b.Property(x => x.Estante).HasMaxLength(40).IsRequired();
        b.Property(x => x.Prateleira).HasMaxLength(40).IsRequired();
        b.Property(x => x.CorredorNormalizado).HasMaxLength(40).IsRequired();
        b.Property(x => x.EstanteNormalizada).HasMaxLength(40).IsRequired();
        b.Property(x => x.PrateleiraNormalizada).HasMaxLength(40).IsRequired();
        b.HasIndex(x => new { x.AlmoxarifadoId, x.CorredorNormalizado, x.EstanteNormalizada, x.PrateleiraNormalizada }).IsUnique();
        b.HasOne(x => x.Almoxarifado).WithMany().HasForeignKey(x => x.AlmoxarifadoId).OnDelete(DeleteBehavior.Restrict);
    }
}
