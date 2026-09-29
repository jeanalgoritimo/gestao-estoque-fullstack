using GestaoEstoque.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestaoEstoque.Infrastructure.Persistence.Configurations;

public class PedidoCompraConfiguration : IEntityTypeConfiguration<PedidoCompra>
{
    public void Configure(EntityTypeBuilder<PedidoCompra> b)
    {
        b.ToTable("PedidosCompra");
        b.HasKey(p => p.Id);
        b.Property(p => p.Situacao).HasConversion<int>();
        b.Property(p => p.CriadoPorNome).HasMaxLength(120).IsRequired();
        b.Property(p => p.EncerradoPorNome).HasMaxLength(120);
        b.Property(p => p.Versao).IsRowVersion();
        b.HasOne(p => p.Fornecedor).WithMany().HasForeignKey(p => p.FornecedorId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Usuario>().WithMany().HasForeignKey(p => p.CriadoPorId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Usuario>().WithMany().HasForeignKey(p => p.EncerradoPorId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(p => p.Itens).WithOne().HasForeignKey(i => i.PedidoCompraId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ItemPedidoCompraConfiguration : IEntityTypeConfiguration<ItemPedidoCompra>
{
    public void Configure(EntityTypeBuilder<ItemPedidoCompra> b)
    {
        b.ToTable("ItensPedidoCompra");
        b.HasKey(i => i.Id);
        b.HasOne(i => i.Produto).WithMany().HasForeignKey(i => i.ProdutoId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(i => new { i.PedidoCompraId, i.ProdutoId }).IsUnique();
    }
}
