using GestaoEstoque.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GestaoEstoque.Api.Security;

public static class Permissoes
{
    public const string GerenciarProdutos = "GerenciarProdutos";
    public const string CadastrarProdutos = "CadastrarProdutos";
    public const string GerenciarCategorias = "GerenciarCategorias";
    public const string CadastrarCategorias = "CadastrarCategorias";
    public const string MovimentarEstoque = "MovimentarEstoque";
}

public record RequisitoPermissao(string Nome) : IAuthorizationRequirement;

public class PermissaoHandler(GestaoEstoqueDbContext db) : AuthorizationHandler<RequisitoPermissao>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, RequisitoPermissao requirement)
    {
        if (!int.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)) return;
        var perfil = await db.Usuarios.AsNoTracking().Where(u => u.Id == id && u.Ativo)
            .Select(u => u.PerfilAcesso).FirstOrDefaultAsync();
        if (perfil is null || !perfil.Ativo) return;
        var permitido = requirement.Nome switch
        {
            Permissoes.GerenciarProdutos => perfil.GerenciarProdutos,
            Permissoes.CadastrarProdutos => perfil.CadastrarProdutos,
            Permissoes.GerenciarCategorias => perfil.GerenciarCategorias,
            Permissoes.CadastrarCategorias => perfil.CadastrarCategorias,
            Permissoes.MovimentarEstoque => perfil.MovimentarEstoque,
            _ => false
        };
        if (permitido) context.Succeed(requirement);
    }
}
