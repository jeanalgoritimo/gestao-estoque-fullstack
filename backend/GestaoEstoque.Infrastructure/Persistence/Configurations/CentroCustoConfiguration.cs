using GestaoEstoque.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace GestaoEstoque.Infrastructure.Persistence.Configurations;
public class CentroCustoConfiguration : IEntityTypeConfiguration<CentroCusto>
{
    public void Configure(EntityTypeBuilder<CentroCusto> b)
    {
        b.ToTable("CentrosCusto"); b.HasKey(c => c.Id);
        b.Property(c => c.Nome).HasMaxLength(100).IsRequired();
        b.Property(c => c.NomeNormalizado).HasMaxLength(100).IsRequired();
        b.HasIndex(c => c.NomeNormalizado).IsUnique();
    }
}
