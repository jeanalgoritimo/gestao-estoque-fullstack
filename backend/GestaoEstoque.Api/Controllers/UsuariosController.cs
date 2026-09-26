using GestaoEstoque.Domain.Entities;
using GestaoEstoque.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GestaoEstoque.Api.Controllers;

[ApiController]
[Authorize(Roles = Perfis.Administrador)]
[Route("api/usuarios")]
public class UsuariosController(GestaoEstoqueDbContext db, IPasswordHasher<Usuario> hasher) : ControllerBase
{
    public record NovoUsuarioRequest(string Nome, string Email, string Senha, int PerfilId);
    public record UsuarioResponse(int Id, string Nome, string Email, int PerfilId, string Perfil, bool Ativo);
    private static UsuarioResponse Map(Usuario u) => new(u.Id, u.Nome, u.Email, u.PerfilId,
        u.PerfilAcesso?.Nome ?? "", u.Ativo);

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct) =>
        Ok((await db.Usuarios.AsNoTracking().Include(u => u.PerfilAcesso).OrderBy(u => u.Nome).ToListAsync(ct)).Select(Map));

    [HttpPost]
    public async Task<IActionResult> Criar(NovoUsuarioRequest request, CancellationToken ct)
    {
        if (request.Senha is null || request.Senha.Length < 12 || request.Senha.Length > 128)
            return BadRequest(new { erro = "A senha deve ter entre 12 e 128 caracteres." });
        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
            return BadRequest(new { erro = "E-mail inválido." });
        var perfil = await db.PerfisAcesso.FirstOrDefaultAsync(p => p.Id == request.PerfilId && p.Ativo, ct);
        if (perfil is null) return BadRequest(new { erro = "Selecione um perfil ativo." });
        try
        {
            var usuario = new Usuario(request.Nome, request.Email, "pendente", request.PerfilId);
            if (await db.Usuarios.AnyAsync(u => u.Email == usuario.Email, ct))
                return Conflict(new { erro = "E-mail já cadastrado." });
            usuario.AlterarSenha(hasher.HashPassword(usuario, request.Senha));
            db.Usuarios.Add(usuario);
            await db.SaveChangesAsync(ct);
            return Created($"/api/usuarios/{usuario.Id}", new UsuarioResponse(usuario.Id, usuario.Nome, usuario.Email,
                perfil.Id, perfil.Nome, usuario.Ativo));
        }
        catch (ArgumentException ex) { return BadRequest(new { erro = ex.Message }); }
        catch (DbUpdateException) { return Conflict(new { erro = "Não foi possível cadastrar; confira se o e-mail já existe." }); }
    }

    [HttpPut("{id:int}/perfil")]
    public async Task<IActionResult> AlterarPerfil(int id, [FromBody] int perfilId, CancellationToken ct)
    {
        if (User.FindFirstValue(ClaimTypes.NameIdentifier) == id.ToString())
            return Conflict(new { erro = "Você não pode alterar seu próprio perfil." });
        var usuario = await db.Usuarios.Include(u => u.PerfilAcesso).FirstOrDefaultAsync(u => u.Id == id, ct);
        if (usuario is null) return NotFound();
        var perfil = await db.PerfisAcesso.FirstOrDefaultAsync(p => p.Id == perfilId && p.Ativo, ct);
        if (perfil is null) return BadRequest(new { erro = "Selecione um perfil ativo." });
        usuario.AlterarPerfil(perfilId);
        await db.SaveChangesAsync(ct);
        return Ok(new UsuarioResponse(usuario.Id, usuario.Nome, usuario.Email, perfil.Id, perfil.Nome, usuario.Ativo));
    }

    [HttpPatch("{id:int}/ativo")]
    public async Task<IActionResult> DefinirAtivo(int id, [FromBody] bool ativo, CancellationToken ct)
    {
        var usuario = await db.Usuarios.Include(u => u.PerfilAcesso).FirstOrDefaultAsync(u => u.Id == id, ct);
        if (usuario is null) return NotFound();
        if (!ativo && User.FindFirstValue(ClaimTypes.NameIdentifier) == id.ToString())
            return Conflict(new { erro = "Você não pode desativar sua própria conta." });
        if (ativo) usuario.Ativar(); else usuario.Desativar();
        await db.SaveChangesAsync(ct);
        return Ok(Map(usuario));
    }
}
