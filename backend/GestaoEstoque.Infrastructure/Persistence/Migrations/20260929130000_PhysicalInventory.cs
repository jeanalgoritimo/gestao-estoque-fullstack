using GestaoEstoque.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestaoEstoque.Infrastructure.Persistence.Migrations;

[DbContext(typeof(GestaoEstoqueDbContext))]
[Migration("20260929130000_PhysicalInventory")]
public partial class PhysicalInventory : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "InventariosFisicos",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                ProdutoId = table.Column<int>(type: "int", nullable: false),
                SaldoInicial = table.Column<int>(type: "int", nullable: false),
                VersaoProduto = table.Column<byte[]>(type: "varbinary(8)", maxLength: 8, nullable: false),
                QuantidadeContada = table.Column<int>(type: "int", nullable: true),
                Situacao = table.Column<int>(type: "int", nullable: false),
                AbertoUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                AbertoPorId = table.Column<int>(type: "int", nullable: false),
                AbertoPorNome = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                EncerradoUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                EncerradoPorId = table.Column<int>(type: "int", nullable: true),
                EncerradoPorNome = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                Motivo = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                Versao = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_InventariosFisicos", x => x.Id);
                table.ForeignKey("FK_InventariosFisicos_Produtos_ProdutoId", x => x.ProdutoId, "Produtos", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_InventariosFisicos_Usuarios_AbertoPorId", x => x.AbertoPorId, "Usuarios", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_InventariosFisicos_Usuarios_EncerradoPorId", x => x.EncerradoPorId, "Usuarios", "Id", onDelete: ReferentialAction.Restrict);
            });
        migrationBuilder.CreateIndex("IX_InventariosFisicos_ProdutoId", "InventariosFisicos", "ProdutoId", unique: true, filter: "[Situacao] = 1");
        migrationBuilder.CreateIndex("IX_InventariosFisicos_AbertoPorId", "InventariosFisicos", "AbertoPorId");
        migrationBuilder.CreateIndex("IX_InventariosFisicos_EncerradoPorId", "InventariosFisicos", "EncerradoPorId");
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("InventariosFisicos");
}
