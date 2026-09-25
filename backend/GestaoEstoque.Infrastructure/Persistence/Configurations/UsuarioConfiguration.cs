using GestaoEstoque.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestaoEstoque.Infrastructure.Persistence.Configurations;

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("Usuarios");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Nome).HasMaxLength(120).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(254).IsRequired();
        builder.HasIndex(u => u.Email).IsUnique();
        builder.Property(u => u.SenhaHash).HasMaxLength(500).IsRequired();
        builder.Property(u => u.Perfil).HasMaxLength(20).IsRequired();
        builder.Property(u => u.VersaoSeguranca).HasMaxLength(32).IsRequired();
    }
}
