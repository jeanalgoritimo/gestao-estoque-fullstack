using GestaoEstoque.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestaoEstoque.Infrastructure.Persistence.Configurations;

public class PerfilAcessoConfiguration : IEntityTypeConfiguration<PerfilAcesso>
{
    public void Configure(EntityTypeBuilder<PerfilAcesso> builder)
    {
        builder.ToTable("PerfisAcesso");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Nome).HasMaxLength(80).IsRequired();
        builder.Property(p => p.NomeNormalizado).HasMaxLength(80).IsRequired();
        builder.HasIndex(p => p.NomeNormalizado).IsUnique();
        builder.Property(p => p.VersaoSeguranca).HasMaxLength(32).IsRequired();
    }
}
