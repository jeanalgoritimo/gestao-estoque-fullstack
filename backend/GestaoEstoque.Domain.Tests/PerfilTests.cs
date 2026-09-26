using GestaoEstoque.Domain.Entities;
using Xunit;

namespace GestaoEstoque.Domain.Tests;

public class PerfilTests
{
    [Fact]
    public void AlterarPermissoesRevogaSessoesDoPerfil()
    {
        var perfil = new PerfilAcesso("Conferente", false, false, true);
        var versao = perfil.VersaoSeguranca;
        perfil.Atualizar("Conferente", true, false, false);
        Assert.True(perfil.GerenciarProdutos);
        Assert.False(perfil.MovimentarEstoque);
        Assert.NotEqual(versao, perfil.VersaoSeguranca);
    }

    [Fact]
    public void PerfilDeSistemaNaoPodeSerDesativado()
    {
        var perfil = new PerfilAcesso(Perfis.Administrador, true, true, true, sistema: true);
        Assert.Throws<InvalidOperationException>(() => perfil.DefinirAtivo(false));
    }
}
