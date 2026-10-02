using GestaoEstoque.Domain.Entities;
using GestaoEstoque.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace GestaoEstoque.Domain.Tests;

// SQLite verifica persistência e atomicidade. Rowversion nativo e migrations SQL Server
// são verificados pelo modelo/script; validação conectada ao SQL Express segue pendente.
public class SaldosLocaisPersistenciaTests
{
    private sealed class ContextoTeste(SqliteConnection connection) : GestaoEstoqueDbContext(
        new DbContextOptionsBuilder<GestaoEstoqueDbContext>().UseSqlite(connection).Options)
    {
        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);
            // SQLite não gera rowversion. Mantém a coluna para persistência dos demais campos.
            foreach (var entidade in b.Model.GetEntityTypes().Where(e => e.FindProperty("Versao") is not null))
                b.Entity(entidade.ClrType).Property<byte[]>("Versao").ValueGeneratedNever();
        }
    }
    private static async Task<Produto> Preparar(ContextoTeste db)
    {
        await db.Database.EnsureCreatedAsync();
        var categoria = new CategoriaProduto("Materiais"); db.CategoriasProduto.Add(categoria); await db.SaveChangesAsync();
        var produto = new Produto("Cabo", categoria.Id, 2m); db.Produtos.Add(produto); await db.SaveChangesAsync();
        produto.RegistrarEntrada(10.001m); await db.SaveChangesAsync(); return produto;
    }
    [Fact]
    public async Task SaveChangesDistribuiReservaEEntregaDepoisDeTransferenciaPersistida()
    {
        await using var conexao = new SqliteConnection("Data Source=:memory:"); await conexao.OpenAsync();
        await using var db = new ContextoTeste(conexao); var produto = await Preparar(db);
        var almoxarifado = new Almoxarifado("Destino"); db.Almoxarifados.Add(almoxarifado); await db.SaveChangesAsync();
        var posicao = new PosicaoEstoque(almoxarifado.Id, "A", "1", "1"); db.PosicoesEstoque.Add(posicao); await db.SaveChangesAsync();
        var origem = await db.SaldosLocais.SingleAsync(s => s.ProdutoId == produto.Id);
        var destino = new SaldoLocal(produto.Id, posicao.Id); db.SaldosLocais.Add(destino);
        origem.TransferirPara(destino, 8m); db.Entry(produto).Property(p => p.Estoque).IsModified = true; await db.SaveChangesAsync();
        produto.Reservar(6m); await db.SaveChangesAsync();
        produto.EntregarReserva(3m); await db.SaveChangesAsync();
        await using var verificacao = new ContextoTeste(conexao);
        var salvo = await verificacao.Produtos.SingleAsync(p => p.Id == produto.Id);
        var locais = await verificacao.SaldosLocais.Where(s => s.ProdutoId == produto.Id).ToListAsync();
        Assert.Equal(7.001m, salvo.Estoque); Assert.Equal(3m, salvo.EstoqueReservado);
        Assert.Equal(salvo.Estoque, locais.Sum(s => s.Quantidade)); Assert.Equal(salvo.EstoqueReservado, locais.Sum(s => s.Reservado));
        Assert.All(locais, s => Assert.True(s.Disponivel >= 0));
    }
    [Fact]
    public async Task FalhaNoHistoricoDesfazTambemSaldoTotalELocais()
    {
        await using var conexao = new SqliteConnection("Data Source=:memory:"); await conexao.OpenAsync();
        await using var db = new ContextoTeste(conexao); var produto = await Preparar(db);
        produto.RegistrarEntrada(0.5m);
        // FKs inexistentes provocam falha real de gravação do histórico na mesma transação.
        db.TransferenciasEstoque.Add(new TransferenciaEstoque(produto.Id, null, 999, 0.5m, 999, "Operador", "Organização", "Sem localização", "Destino"));
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        await using var verificacao = new ContextoTeste(conexao);
        var saldo = await verificacao.Produtos.Where(p => p.Id == produto.Id).Select(p => p.Estoque).SingleAsync();
        var locais = await verificacao.SaldosLocais.Where(s => s.ProdutoId == produto.Id).ToListAsync();
        Assert.Equal(10.001m, saldo); Assert.Equal(saldo, locais.Sum(s => s.Quantidade));
        Assert.Empty(await verificacao.TransferenciasEstoque.ToListAsync());
    }
}
