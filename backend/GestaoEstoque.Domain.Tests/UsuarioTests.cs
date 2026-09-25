using GestaoEstoque.Domain.Entities;
using Xunit;

namespace GestaoEstoque.Domain.Tests;

public class UsuarioTests
{
    [Fact]
    public void DesativacaoRevogaVersaoDeSeguranca()
    {
        var usuario = new Usuario("Maria", "MARIA@EXEMPLO.COM", "hash", Perfis.Operador);
        var versao = usuario.VersaoSeguranca;
        usuario.Desativar();
        Assert.False(usuario.Ativo);
        Assert.NotEqual(versao, usuario.VersaoSeguranca);
        Assert.Equal("maria@exemplo.com", usuario.Email);
    }

    [Fact]
    public void PerfilDesconhecidoERecusado() =>
        Assert.Throws<ArgumentException>(() => new Usuario("Maria", "maria@exemplo.com", "hash", "Dono"));
}
