using GestaoEstoque.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace GestaoEstoque.Infrastructure.Persistence.Configurations;
public class SaldoLocalConfiguration : IEntityTypeConfiguration<SaldoLocal>
{
    public void Configure(EntityTypeBuilder<SaldoLocal> b)
    {
        b.ToTable("SaldosLocais", t => { t.HasCheckConstraint("CK_SaldosLocais_Quantidades", "[Quantidade] >= 0 AND [Reservado] >= 0 AND [Reservado] <= [Quantidade]"); });
        b.HasKey(s => s.Id); b.Property(s => s.Quantidade).HasPrecision(13, 3); b.Property(s => s.Reservado).HasPrecision(13, 3);
        b.Property(s => s.Versao).IsRowVersion(); b.Ignore(s => s.Disponivel);
        b.HasOne<Produto>().WithMany().HasForeignKey(s => s.ProdutoId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(s => s.PosicaoEstoque).WithMany().HasForeignKey(s => s.PosicaoEstoqueId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(s => new { s.ProdutoId, s.PosicaoEstoqueId }).IsUnique().HasFilter("[PosicaoEstoqueId] IS NOT NULL");
        b.HasIndex(s => s.ProdutoId).HasDatabaseName("IX_SaldosLocais_ProdutoSemLocal").IsUnique().HasFilter("[PosicaoEstoqueId] IS NULL");
    }
}
public class TransferenciaEstoqueConfiguration : IEntityTypeConfiguration<TransferenciaEstoque>
{
    public void Configure(EntityTypeBuilder<TransferenciaEstoque> b)
    {
        b.ToTable("TransferenciasEstoque"); b.HasKey(t => t.Id); b.Property(t => t.Quantidade).HasPrecision(13, 3);
        b.Property(t => t.UsuarioNome).HasMaxLength(120).IsRequired(); b.Property(t => t.Motivo).HasMaxLength(150).IsRequired();
        b.Property(t => t.OrigemDescricao).HasMaxLength(300).IsRequired(); b.Property(t => t.DestinoDescricao).HasMaxLength(300).IsRequired();
        b.HasOne<Produto>().WithMany().HasForeignKey(t => t.ProdutoId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Usuario>().WithMany().HasForeignKey(t => t.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<PosicaoEstoque>().WithMany().HasForeignKey(t => t.OrigemId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<PosicaoEstoque>().WithMany().HasForeignKey(t => t.DestinoId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(t => new { t.ProdutoId, t.DataUtc });
    }
}
