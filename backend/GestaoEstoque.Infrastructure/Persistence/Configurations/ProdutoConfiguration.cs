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

        builder.HasOne(produto => produto.CategoriaProduto)
            .WithMany()
            .HasForeignKey(produto => produto.CategoriaId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.Property(produto => produto.Preco)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(produto => produto.Estoque)
            .IsRequired();

        builder.Property(produto => produto.EstoqueMinimo).IsRequired();
        builder.Property(produto => produto.Versao).IsRowVersion();

        builder.Property(produto => produto.Ativo)
            .IsRequired();
    }
}
