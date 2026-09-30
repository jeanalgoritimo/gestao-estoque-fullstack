using GestaoEstoque.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace GestaoEstoque.Domain.Tests;

public class RequisicaoMigrationTests
{
    [Fact]
    public void MigrationGeraSqlComReservasEVinculosSemConectarAoBanco()
    {
        using var db = new GestaoEstoqueDbContext(new DbContextOptionsBuilder<GestaoEstoqueDbContext>()
            .UseSqlServer("Server=localhost;Database=MigrationScriptOnly;Integrated Security=true;TrustServerCertificate=true")
            .Options);
        var sql = db.GetService<IMigrator>().GenerateScript("20260929180000_ReceiptMovementLink",
            "20260930173000_MaterialRequisitions");
        Assert.Contains("CREATE TABLE [RequisicoesMaterial]", sql);
        Assert.Contains("CREATE TABLE [ItensRequisicaoMaterial]", sql);
        Assert.Contains("[EstoqueReservado] int NOT NULL DEFAULT 0", sql);
        Assert.Contains("REFERENCES [RequisicoesMaterial] ([Id])", sql);
        Assert.DoesNotContain("[Id].[Produtos]", sql);
        Assert.DoesNotContain("DROP TABLE", sql);
    }
}
