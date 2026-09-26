using GestaoEstoque.Domain.Entities;
using Xunit;

namespace GestaoEstoque.Domain.Tests;

public class UsuarioTests
{
    [Fact]
    public void DesativacaoRevogaVersaoDeSeguranca()
    {
        var usuario = new Usuario("Maria", "MARIA@EXEMPLO.COM", "hash", 2);
        var versao = usuario.VersaoSeguranca;
        usuario.Desativar();
        Assert.False(usuario.Ativo);
        Assert.NotEqual(versao, usuario.VersaoSeguranca);
        Assert.Equal("maria@exemplo.com", usuario.Email);
    }

    [Fact]
    public void PerfilInexistenteERecusado() =>
        Assert.Throws<ArgumentException>(() => new Usuario("Maria", "maria@exemplo.com", "hash", 0));
}
