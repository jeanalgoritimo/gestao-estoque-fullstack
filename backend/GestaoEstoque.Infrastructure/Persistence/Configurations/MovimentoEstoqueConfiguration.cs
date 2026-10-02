using GestaoEstoque.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestaoEstoque.Infrastructure.Persistence.Configurations;

public class MovimentoEstoqueConfiguration : IEntityTypeConfiguration<MovimentoEstoque>
{
    public void Configure(EntityTypeBuilder<MovimentoEstoque> builder)
    {
        builder.Property(e => e.Quantidade).HasPrecision(13, 3);
        builder.Property(e => e.SaldoApos).HasPrecision(13, 3);
        builder.HasOne<RequisicaoMaterial>().WithMany().HasForeignKey(m => m.RequisicaoMaterialId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(m => new { m.RequisicaoMaterialId, m.EntregaId });
        builder.HasIndex(m => new { m.RequisicaoMaterialId, m.DevolucaoId });
        builder.ToTable("MovimentosEstoque");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Tipo).HasConversion<int>();
        builder.Property(m => m.Observacao).HasMaxLength(300);
        builder.Property(m => m.DocumentoOrigem).HasMaxLength(100).IsRequired();
        builder.Property(m => m.Motivo).HasMaxLength(150).IsRequired();
        builder.Property(m => m.UsuarioNome).HasMaxLength(120).IsRequired();
        builder.Property(m => m.CustoUnitario).HasPrecision(18, 4);
        builder.HasOne<Usuario>().WithMany().HasForeignKey(m => m.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Produto>().WithMany().HasForeignKey(m => m.ProdutoId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PedidoCompra>().WithMany().HasForeignKey(m => m.PedidoCompraId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(m => new { m.PedidoCompraId, m.RecebimentoId });
        builder.HasIndex(m => new { m.ProdutoId, m.DataUtc });
    }
}
