using GestaoEstoque.Domain.Entities;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace GestaoEstoque.Api.Security;

public class JwtTokenService(SymmetricSecurityKey key, string issuer, string audience)
{
    public (string Token, DateTime ExpiraUtc) Criar(Usuario usuario)
    {
        var perfil = usuario.PerfilAcesso ?? throw new InvalidOperationException("Perfil não carregado.");
        var expira = DateTime.UtcNow.AddHours(2);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Name, usuario.Nome),
            new Claim(ClaimTypes.Email, usuario.Email),
            new Claim(ClaimTypes.Role, perfil.Nome),
            new Claim("security_version", usuario.VersaoSeguranca),
            new Claim("profile_version", perfil.VersaoSeguranca)
        };
        var token = new JwtSecurityToken(issuer, audience, claims, expires: expira,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return (new JwtSecurityTokenHandler().WriteToken(token), expira);
    }
}
