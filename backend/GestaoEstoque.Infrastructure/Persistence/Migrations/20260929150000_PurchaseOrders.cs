using GestaoEstoque.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestaoEstoque.Infrastructure.Persistence.Migrations;

[DbContext(typeof(GestaoEstoqueDbContext))]
[Migration("20260929150000_PurchaseOrders")]
public partial class PurchaseOrders : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.CreateTable("PedidosCompra", table => new
        {
            Id = table.Column<long>(type: "bigint", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
            FornecedorId = table.Column<int>(type: "int", nullable: false),
            Situacao = table.Column<int>(type: "int", nullable: false),
            CriadoUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
            CriadoPorId = table.Column<int>(type: "int", nullable: false),
            CriadoPorNome = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
            EncerradoUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
            EncerradoPorId = table.Column<int>(type: "int", nullable: true),
            EncerradoPorNome = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
            Versao = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_PedidosCompra", x => x.Id);
            table.ForeignKey("FK_PedidosCompra_Fornecedores_FornecedorId", x => x.FornecedorId, "Fornecedores", "Id", onDelete: ReferentialAction.Restrict);
            table.ForeignKey("FK_PedidosCompra_Usuarios_CriadoPorId", x => x.CriadoPorId, "Usuarios", "Id", onDelete: ReferentialAction.Restrict);
            table.ForeignKey("FK_PedidosCompra_Usuarios_EncerradoPorId", x => x.EncerradoPorId, "Usuarios", "Id", onDelete: ReferentialAction.Restrict);
        });
        m.CreateIndex("IX_PedidosCompra_FornecedorId", "PedidosCompra", "FornecedorId");
        m.CreateIndex("IX_PedidosCompra_CriadoPorId", "PedidosCompra", "CriadoPorId");
        m.CreateIndex("IX_PedidosCompra_EncerradoPorId", "PedidosCompra", "EncerradoPorId");
        m.CreateTable("ItensPedidoCompra", table => new
        {
            Id = table.Column<long>(type: "bigint", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
            PedidoCompraId = table.Column<long>(type: "bigint", nullable: false),
            ProdutoId = table.Column<int>(type: "int", nullable: false),
            Quantidade = table.Column<int>(type: "int", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_ItensPedidoCompra", x => x.Id);
            table.ForeignKey("FK_ItensPedidoCompra_PedidosCompra_PedidoCompraId", x => x.PedidoCompraId, "PedidosCompra", "Id", onDelete: ReferentialAction.Cascade);
            table.ForeignKey("FK_ItensPedidoCompra_Produtos_ProdutoId", x => x.ProdutoId, "Produtos", "Id", onDelete: ReferentialAction.Restrict);
        });
        m.CreateIndex("IX_ItensPedidoCompra_PedidoCompraId_ProdutoId", "ItensPedidoCompra", new[] { "PedidoCompraId", "ProdutoId" }, unique: true);
        m.CreateIndex("IX_ItensPedidoCompra_ProdutoId", "ItensPedidoCompra", "ProdutoId");
    }

    protected override void Down(MigrationBuilder m)
    {
        m.DropTable("ItensPedidoCompra");
        m.DropTable("PedidosCompra");
    }
}
