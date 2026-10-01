using GestaoEstoque.Domain.Entities;
namespace GestaoEstoque.Infrastructure.Reports;

public class FiltroRequisicoes
{
    public int Pagina { get; set; } = 1;
    public int TamanhoPagina { get; set; } = 10;
    public string? Busca { get; set; }
    public string? Solicitante { get; set; }
    public SituacaoRequisicao? Situacao { get; set; }
    public int? CentroCustoId { get; set; }
    public bool SemCentro { get; set; }
    public DateOnly? Inicio { get; set; }
    public DateOnly? Fim { get; set; }
    public void Validar()
    {
        if (Pagina is < 1 or > 1000000 || TamanhoPagina is not (10 or 25 or 50 or 100))
            throw new ArgumentException("Informe página positiva e tamanho de 10, 25, 50 ou 100.");
        if (Busca?.Length > 150 || Solicitante?.Length > 120)
            throw new ArgumentException("Busca limitada a 150 caracteres e solicitante a 120.");
        if (Situacao is not null && !Enum.IsDefined(Situacao.Value)) throw new ArgumentException("Situação inválida.");
        if (CentroCustoId <= 0 || (SemCentro && CentroCustoId is not null)) throw new ArgumentException("Filtro de centro de custo inválido.");
        if (Inicio == DateOnly.MinValue || Fim == DateOnly.MinValue || Fim == DateOnly.MaxValue ||
            (Inicio is not null && Fim is not null && Fim < Inicio)) throw new ArgumentException("Informe um período de criação válido.");
    }
}
public static class RequisicoesConsulta
{
    public static IQueryable<RequisicaoMaterial> Filtrar(IQueryable<RequisicaoMaterial> consulta, FiltroRequisicoes filtro)
    {
        filtro.Validar();
        if (filtro.Situacao is not null) consulta = consulta.Where(r => r.Situacao == filtro.Situacao);
        if (filtro.SemCentro) consulta = consulta.Where(r => r.CentroCustoId == null);
        else if (filtro.CentroCustoId is not null) consulta = consulta.Where(r => r.CentroCustoId == filtro.CentroCustoId);
        var busca = filtro.Busca?.Trim().ToLowerInvariant();
        if (!string.IsNullOrEmpty(busca))
        {
            if (long.TryParse(busca.TrimStart('#'), out var numero)) consulta = consulta.Where(r => r.Id == numero);
            else consulta = consulta.Where(r => r.Finalidade.ToLower().Contains(busca) || r.SolicitanteNome.ToLower().Contains(busca));
        }
        var solicitante = filtro.Solicitante?.Trim().ToLowerInvariant();
        if (!string.IsNullOrEmpty(solicitante)) consulta = consulta.Where(r => r.SolicitanteNome.ToLower().Contains(solicitante));
        var zona = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
        if (filtro.Inicio is not null)
        {
            var inicioUtc = TimeZoneInfo.ConvertTimeToUtc(filtro.Inicio.Value.ToDateTime(TimeOnly.MinValue), zona);
            consulta = consulta.Where(r => r.CriadoUtc >= inicioUtc);
        }
        if (filtro.Fim is not null)
        {
            var fimUtc = TimeZoneInfo.ConvertTimeToUtc(filtro.Fim.Value.AddDays(1).ToDateTime(TimeOnly.MinValue), zona);
            consulta = consulta.Where(r => r.CriadoUtc < fimUtc);
        }
        return consulta;
    }
}
