using GestaoEstoque.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestaoEstoque.Infrastructure.Persistence.Migrations;

[DbContext(typeof(GestaoEstoqueDbContext))]
[Migration("20260929160000_PartialPurchaseReceipts")]
public partial class PartialPurchaseReceipts : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.AddColumn<int>("QuantidadeRecebida", "ItensPedidoCompra", type: "int", nullable: false, defaultValue: 0);
        // Pedidos já concluídos antes desta migração foram integralmente recebidos.
        m.Sql("UPDATE i SET QuantidadeRecebida = i.Quantidade FROM ItensPedidoCompra i INNER JOIN PedidosCompra p ON p.Id = i.PedidoCompraId WHERE p.Situacao = 2");
    }

    protected override void Down(MigrationBuilder m) => m.DropColumn("QuantidadeRecebida", "ItensPedidoCompra");
}
