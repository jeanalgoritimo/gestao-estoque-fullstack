using GestaoEstoque.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestaoEstoque.Infrastructure.Persistence.Migrations;

[DbContext(typeof(GestaoEstoqueDbContext))]
[Migration("20260929180000_ReceiptMovementLink")]
public partial class ReceiptMovementLink : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.AddColumn<long>("PedidoCompraId", "MovimentosEstoque", type: "bigint", nullable: true);
        m.AddColumn<Guid>("RecebimentoId", "MovimentosEstoque", type: "uniqueidentifier", nullable: true);
        m.CreateIndex("IX_MovimentosEstoque_PedidoCompraId_RecebimentoId", "MovimentosEstoque",
            new[] { "PedidoCompraId", "RecebimentoId" });
        m.AddForeignKey("FK_MovimentosEstoque_PedidosCompra_PedidoCompraId", "MovimentosEstoque", "PedidoCompraId",
            "PedidosCompra", "Id", onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder m)
    {
        m.DropForeignKey("FK_MovimentosEstoque_PedidosCompra_PedidoCompraId", "MovimentosEstoque");
        m.DropIndex("IX_MovimentosEstoque_PedidoCompraId_RecebimentoId", "MovimentosEstoque");
        m.DropColumn("PedidoCompraId", "MovimentosEstoque");
        m.DropColumn("RecebimentoId", "MovimentosEstoque");
    }
}
