using GestaoEstoque.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestaoEstoque.Infrastructure.Persistence.Configurations;

public class ProdutoConfiguration : IEntityTypeConfiguration<Produto>
{
    public void Configure(EntityTypeBuilder<Produto> builder)
    {
        builder.ToTable("Produtos");

        builder.HasKey(produto => produto.Id);

        builder.Property(produto => produto.Id)
            .ValueGeneratedOnAdd();

        builder.Property(produto => produto.Nome)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(produto => produto.Categoria)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(produto => produto.Preco)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(produto => produto.Estoque)
            .IsRequired();

        builder.Property(produto => produto.Ativo)
            .IsRequired();
    }
}