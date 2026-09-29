using GestaoEstoque.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestaoEstoque.Infrastructure.Persistence.Configurations;

public class FornecedorConfiguration : IEntityTypeConfiguration<Fornecedor>
{
    public void Configure(EntityTypeBuilder<Fornecedor> builder)
    {
        builder.ToTable("Fornecedores");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Nome).HasMaxLength(150).IsRequired();
        builder.Property(f => f.NomeNormalizado).HasMaxLength(150).IsRequired();
        builder.HasIndex(f => f.NomeNormalizado).IsUnique();
        builder.Property(f => f.Contato).HasMaxLength(120);
        builder.Property(f => f.Email).HasMaxLength(254);
        builder.Property(f => f.Telefone).HasMaxLength(30);
    }
}
