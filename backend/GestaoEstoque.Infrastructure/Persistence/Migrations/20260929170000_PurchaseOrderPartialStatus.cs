using GestaoEstoque.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestaoEstoque.Infrastructure.Persistence.Migrations;

[DbContext(typeof(GestaoEstoqueDbContext))]
[Migration("20260929170000_PurchaseOrderPartialStatus")]
public partial class PurchaseOrderPartialStatus : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.AddColumn<string>("MotivoCancelamento", "PedidosCompra", type: "nvarchar(150)", maxLength: 150, nullable: true);
        // Pedidos parciais anteriores eram armazenados como abertos ou cancelados.
        m.Sql("UPDATE p SET Situacao = 4 FROM PedidosCompra p WHERE p.Situacao = 1 AND EXISTS (SELECT 1 FROM ItensPedidoCompra i WHERE i.PedidoCompraId = p.Id AND i.QuantidadeRecebida > 0)");
        m.Sql("UPDATE p SET Situacao = 5 FROM PedidosCompra p WHERE p.Situacao = 3 AND EXISTS (SELECT 1 FROM ItensPedidoCompra i WHERE i.PedidoCompraId = p.Id AND i.QuantidadeRecebida > 0)");
    }

    protected override void Down(MigrationBuilder m)
    {
        m.Sql("UPDATE PedidosCompra SET Situacao = 1 WHERE Situacao = 4");
        m.Sql("UPDATE PedidosCompra SET Situacao = 3 WHERE Situacao = 5");
        m.DropColumn("MotivoCancelamento", "PedidosCompra");
    }
}
