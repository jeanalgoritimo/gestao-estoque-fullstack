using GestaoEstoque.Api.Security;
using GestaoEstoque.Domain.Entities;
using GestaoEstoque.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GestaoEstoque.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/requisicoes")]
public class RequisicoesController(GestaoEstoqueDbContext db) : ControllerBase
{
    public record ItemRequest(int ProdutoId, int Quantidade);
    public record CriarRequest(string Finalidade, List<ItemRequest> Itens);
    public record EntregarRequest(List<ItemRequest> Itens);
    public record CancelarRequest(string Motivo);
    public record ItemResponse(int ProdutoId, string Produto, int Quantidade, int QuantidadeEntregue);
    public record RequisicaoResponse(long Id, string Finalidade, int SolicitanteId, string SolicitanteNome,
        DateTime CriadoUtc, SituacaoRequisicao Situacao, DateTime? AprovadoUtc, string? AprovadoPorNome,
        DateTime? EncerradoUtc, string? EncerradoPorNome, string? MotivoCancelamento, List<ItemResponse> Itens);
    private static RequisicaoResponse Map(RequisicaoMaterial r) => new(r.Id, r.Finalidade, r.SolicitanteId,
        r.SolicitanteNome, r.CriadoUtc, r.Situacao, r.AprovadoUtc, r.AprovadoPorNome, r.EncerradoUtc,
        r.EncerradoPorNome, r.MotivoCancelamento,
        r.Itens.Select(i => new ItemResponse(i.ProdutoId, i.Produto.Nome, i.Quantidade, i.QuantidadeEntregue)).ToList());
    private IQueryable<RequisicaoMaterial> Consulta() => db.RequisicoesMaterial.AsSplitQuery()
        .Include(r => r.Itens).ThenInclude(i => i.Produto);
    private bool Usuario(out int id, out string nome)
    {
        nome = User.Identity?.Name ?? "";
        return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out id) && !string.IsNullOrWhiteSpace(nome);
    }
    private static bool ItensValidos(List<ItemRequest>? itens) => itens is { Count: > 0 and <= 100 }
        && itens.All(i => i.ProdutoId > 0 && i.Quantidade > 0)
        && itens.Select(i => i.ProdutoId).Distinct().Count() == itens.Count;

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct) => Ok((await Consulta().AsNoTracking()
        .OrderByDescending(r => r.Id).Take(100).ToListAsync(ct)).Select(Map));
    [HttpGet("{id:long}")]
    public async Task<IActionResult> Obter(long id, CancellationToken ct)
    {
        var r = await Consulta().AsNoTracking().FirstOrDefaultAsync(r => r.Id == id, ct);
        return r is null ? NotFound() : Ok(Map(r));
    }
    [HttpPost]
    public async Task<IActionResult> Criar(CriarRequest request, CancellationToken ct)
    {
        if (!Usuario(out var usuarioId, out var nome)) return Unauthorized();
        if (!ItensValidos(request.Itens)) return BadRequest(new { erro = "Informe de 1 a 100 produtos distintos com quantidades positivas." });
        var ids = request.Itens.Select(i => i.ProdutoId).ToList();
        if (await db.Produtos.CountAsync(p => ids.Contains(p.Id) && p.Ativo, ct) != ids.Count)
            return BadRequest(new { erro = "Selecione somente produtos ativos." });
        try
        {
            var r = new RequisicaoMaterial(request.Finalidade, usuarioId, nome);
            foreach (var item in request.Itens) r.AdicionarItem(item.ProdutoId, item.Quantidade);
            db.RequisicoesMaterial.Add(r); await db.SaveChangesAsync(ct);
            return CreatedAtAction(nameof(Obter), new { id = r.Id }, new { r.Id });
        }
        catch (ArgumentException ex) { return BadRequest(new { erro = ex.Message }); }
    }
    [HttpPost("{id:long}/enviar")]
    public async Task<IActionResult> Enviar(long id, CancellationToken ct)
    {
        if (!Usuario(out var usuarioId, out _)) return Unauthorized();
        var r = await Consulta().FirstOrDefaultAsync(r => r.Id == id, ct);
        if (r is null) return NotFound();
        if (r.SolicitanteId != usuarioId && !User.IsInRole("Administrador")) return Forbid();
        return await Salvar(() => r.Enviar(), ct);
    }
    [HttpPost("{id:long}/aprovar")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Aprovar(long id, CancellationToken ct)
    {
        if (!Usuario(out var usuarioId, out var nome)) return Unauthorized();
        var r = await Consulta().FirstOrDefaultAsync(r => r.Id == id, ct);
        if (r is null) return NotFound();
        return await Salvar(() =>
        {
            r.Aprovar(usuarioId, nome);
            foreach (var item in r.Itens) item.Produto.Reservar(item.Quantidade);
        }, ct);
    }
    [HttpPost("{id:long}/cancelar")]
    public async Task<IActionResult> Cancelar(long id, CancelarRequest request, CancellationToken ct)
    {
        if (!Usuario(out var usuarioId, out var nome)) return Unauthorized();
        var r = await Consulta().FirstOrDefaultAsync(r => r.Id == id, ct);
        if (r is null) return NotFound();
        // Após aprovação, somente administrador pode liberar o compromisso de atendimento.
        if (!User.IsInRole("Administrador") && (r.SolicitanteId != usuarioId ||
            r.Situacao is not (SituacaoRequisicao.Rascunho or SituacaoRequisicao.Pendente))) return Forbid();
        return await Salvar(() =>
        {
            var reservada = r.Situacao is SituacaoRequisicao.Aprovada or SituacaoRequisicao.ParcialmenteAtendida;
            r.Cancelar(usuarioId, nome, request.Motivo);
            if (reservada)
                foreach (var item in r.Itens.Where(i => i.Quantidade > i.QuantidadeEntregue))
                    item.Produto.LiberarReserva(item.Quantidade - item.QuantidadeEntregue);
        }, ct);
    }
    [HttpPost("{id:long}/entregar")]
    [Authorize(Policy = Permissoes.MovimentarEstoque)]
    public async Task<IActionResult> Entregar(long id, EntregarRequest request, CancellationToken ct)
    {
        if (!Usuario(out var usuarioId, out var nome)) return Unauthorized();
        if (!ItensValidos(request.Itens)) return BadRequest(new { erro = "Informe produtos distintos com quantidades positivas." });
        var r = await Consulta().FirstOrDefaultAsync(r => r.Id == id, ct);
        if (r is null) return NotFound();
        return await Salvar(() =>
        {
            var entregaId = Guid.NewGuid();
            foreach (var item in request.Itens)
            {
                var linha = r.Itens.SingleOrDefault(i => i.ProdutoId == item.ProdutoId)
                    ?? throw new ArgumentException("Produto não pertence à requisição.");
                r.Entregar(item.ProdutoId, item.Quantidade, usuarioId, nome);
                linha.Produto.EntregarReserva(item.Quantidade);
                var movimento = new MovimentoEstoque(item.ProdutoId, TipoMovimento.Saida, item.Quantidade,
                    null, DateTime.UtcNow, $"REQ-{r.Id}-E-{entregaId:N}", "Entrega de requisição de material",
                    usuarioId, nome, null, linha.Produto.Estoque);
                movimento.VincularEntrega(r.Id, entregaId); db.MovimentosEstoque.Add(movimento);
            }
            // Força a concorrência do cabeçalho mesmo em duas entregas parciais consecutivas.
            db.Entry(r).Property(r => r.Situacao).IsModified = true;
        }, ct);
    }
    [HttpGet("{id:long}/entregas")]
    public async Task<IActionResult> Entregas(long id, CancellationToken ct)
    {
        if (!await db.RequisicoesMaterial.AnyAsync(r => r.Id == id, ct)) return NotFound();
        return Ok(await (from m in db.MovimentosEstoque.AsNoTracking()
            join p in db.Produtos on m.ProdutoId equals p.Id
            where m.RequisicaoMaterialId == id
            orderby m.DataUtc descending, m.Id descending
            select new { m.Id, m.EntregaId, m.ProdutoId, Produto = p.Nome, m.Quantidade, m.DataUtc, m.UsuarioNome, m.SaldoApos })
            .ToListAsync(ct));
    }
    private async Task<IActionResult> Salvar(Action alterar, CancellationToken ct)
    {
        try
        {
            alterar();
            // SaveChanges inclui requisição, reservas, saldos e movimentos em uma transação.
            await db.SaveChangesAsync(ct); return Ok();
        }
        catch (ArgumentException ex) { return BadRequest(new { erro = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { erro = ex.Message }); }
        catch (DbUpdateConcurrencyException) { return Conflict(new { erro = "Requisição ou estoque alterados por outra operação. Recarregue e tente novamente." }); }
        catch (OverflowException) { return Conflict(new { erro = "Quantidade excede o limite permitido." }); }
    }
}
