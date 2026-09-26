using GestaoEstoque.Api.Security;
using GestaoEstoque.Domain.Entities;
using GestaoEstoque.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GestaoEstoque.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(GestaoEstoqueDbContext db, IPasswordHasher<Usuario> hasher, JwtTokenService tokens) : ControllerBase
{
    public record LoginRequest(string Email, string Senha);
    public record AlterarSenhaRequest(string SenhaAtual, string NovaSenha);

    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
    {
        var email = request.Email?.Trim().ToLowerInvariant();
        var usuario = await db.Usuarios.Include(u => u.PerfilAcesso).SingleOrDefaultAsync(u => u.Email == email, ct);
        var perfil = usuario?.PerfilAcesso;
        if (usuario is null || !usuario.Ativo || perfil is null || !perfil.Ativo ||
            hasher.VerifyHashedPassword(usuario, usuario.SenhaHash, request.Senha ?? "") == PasswordVerificationResult.Failed)
            return Unauthorized(new { erro = "E-mail ou senha inválidos." });
        var (token, expiraUtc) = tokens.Criar(usuario);
        return Ok(new { token, expiraUtc, usuario = new { usuario.Id, usuario.Nome, usuario.Email,
            PerfilId = perfil.Id, Perfil = perfil.Nome,
            Permissoes = new { perfil.GerenciarProdutos, perfil.GerenciarCategorias, perfil.MovimentarEstoque } } });
    }

    [Authorize]
    [HttpPost("alterar-senha")]
    public async Task<IActionResult> AlterarSenha(AlterarSenhaRequest request, CancellationToken ct)
    {
        if (request.NovaSenha is null || request.NovaSenha.Length < 12 || request.NovaSenha.Length > 128)
            return BadRequest(new { erro = "A nova senha deve ter entre 12 e 128 caracteres." });
        var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var usuario = await db.Usuarios.FindAsync([id], ct);
        if (usuario is null) return Unauthorized();
        if (hasher.VerifyHashedPassword(usuario, usuario.SenhaHash, request.SenhaAtual ?? "") == PasswordVerificationResult.Failed)
            return BadRequest(new { erro = "Senha atual inválida." });
        usuario.AlterarSenha(hasher.HashPassword(usuario, request.NovaSenha));
        await db.SaveChangesAsync(ct);
        return NoContent(); // A versão do token anterior foi revogada; o usuário deve entrar novamente.
    }
}
