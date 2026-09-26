using GestaoEstoque.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using GestaoEstoque.Application.Abstractions;
using GestaoEstoque.Infrastructure.Repositories;
using GestaoEstoque.Domain.Entities;
using GestaoEstoque.Api.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

var jwtKey = builder.Configuration["Auth:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
    throw new InvalidOperationException("Configure Auth:Key com pelo menos 32 bytes via variável de ambiente ou user-secrets.");
const string issuer = "GestaoEstoque.Api";
const string audience = "GestaoEstoque.Frontend";
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

var connectionString = builder.Configuration.GetConnectionString(
    "DefaultConnection")
    ?? throw new InvalidOperationException(
        "A connection string 'DefaultConnection' não foi configurada.");

builder.Services.AddDbContext<GestaoEstoqueDbContext>(options =>
    options.UseSqlServer(connectionString));

 builder.Services.AddScoped<IProdutoRepository, ProdutoRepository>();
builder.Services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
builder.Services.AddScoped<IAuthorizationHandler, PermissaoHandler>();
builder.Services.AddSingleton(new JwtTokenService(signingKey, issuer, audience));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidIssuer = issuer,
        ValidateAudience = true, ValidAudience = audience,
        ValidateIssuerSigningKey = true, IssuerSigningKey = signingKey,
        ValidateLifetime = true, ClockSkew = TimeSpan.FromSeconds(30),
        ValidAlgorithms = [SecurityAlgorithms.HmacSha256]
    };
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var id = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            var stamp = context.Principal?.FindFirstValue("security_version");
            var role = context.Principal?.FindFirstValue(ClaimTypes.Role);
            var db = context.HttpContext.RequestServices.GetRequiredService<GestaoEstoqueDbContext>();
            var profileStamp = context.Principal?.FindFirstValue("profile_version");
            if (!int.TryParse(id, out var userId) ||
                !await db.Usuarios.AsNoTracking().AnyAsync(u => u.Id == userId && u.Ativo &&
                    u.VersaoSeguranca == stamp && u.PerfilAcesso != null && u.PerfilAcesso.Ativo &&
                    u.PerfilAcesso.VersaoSeguranca == profileStamp && u.PerfilAcesso.Nome == role))
                context.Fail("Credenciais expiradas ou usuário desativado.");
        }
    };
});
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Permissoes.GerenciarProdutos, p => p.RequireAuthenticatedUser().AddRequirements(new RequisitoPermissao(Permissoes.GerenciarProdutos)));
    options.AddPolicy(Permissoes.CadastrarProdutos, p => p.RequireAuthenticatedUser().AddRequirements(new RequisitoPermissao(Permissoes.CadastrarProdutos)));
    options.AddPolicy(Permissoes.GerenciarCategorias, p => p.RequireAuthenticatedUser().AddRequirements(new RequisitoPermissao(Permissoes.GerenciarCategorias)));
    options.AddPolicy(Permissoes.CadastrarCategorias, p => p.RequireAuthenticatedUser().AddRequirements(new RequisitoPermissao(Permissoes.CadastrarCategorias)));
    options.AddPolicy(Permissoes.MovimentarEstoque, p => p.RequireAuthenticatedUser().AddRequirements(new RequisitoPermissao(Permissoes.MovimentarEstoque)));
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
 
// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Somente o primeiro administrador é criado por configuração externa.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GestaoEstoqueDbContext>();
    if (!await db.Usuarios.AnyAsync())
    {
        var email = builder.Configuration["Bootstrap:AdminEmail"];
        var password = builder.Configuration["Bootstrap:AdminPassword"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || password.Length < 12)
            throw new InvalidOperationException("Banco sem usuários. Configure Bootstrap:AdminEmail e Bootstrap:AdminPassword (mínimo 12 caracteres) antes de iniciar a API.");
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Usuario>>();
        var admin = new Usuario("Administrador", email, "pendente", 1);
        admin.AlterarSenha(hasher.HashPassword(admin, password));
        db.Usuarios.Add(admin);
        await db.SaveChangesAsync();
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
