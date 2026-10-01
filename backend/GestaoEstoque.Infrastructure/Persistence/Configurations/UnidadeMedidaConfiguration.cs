using GestaoEstoque.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace GestaoEstoque.Infrastructure.Persistence.Configurations;
public class UnidadeMedidaConfiguration : IEntityTypeConfiguration<UnidadeMedida>
{
    public void Configure(EntityTypeBuilder<UnidadeMedida> b)
    {
        b.ToTable("UnidadesMedida"); b.HasKey(x => x.Id);
        b.Property(x => x.Sigla).HasMaxLength(10).IsRequired();
        b.Property(x => x.Nome).HasMaxLength(80).IsRequired(); b.HasIndex(x => x.Sigla).IsUnique();
        b.HasData(new { Id = 1, Sigla = "UN", Nome = "Unidade", Ativo = true, Sistema = true });
    }
}
