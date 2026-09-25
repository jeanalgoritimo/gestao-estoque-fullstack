using GestaoEstoque.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestaoEstoque.Infrastructure.Persistence.Migrations;

[DbContext(typeof(GestaoEstoqueDbContext))]
[Migration("20260925180000_StockMovements")]
public partial class StockMovements : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(name: "EstoqueMinimo", table: "Produtos", type: "int", nullable: false, defaultValue: 5);
        migrationBuilder.AddColumn<byte[]>(name: "Versao", table: "Produtos", type: "rowversion", rowVersion: true, nullable: false);
        migrationBuilder.CreateTable(
            name: "MovimentosEstoque",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                ProdutoId = table.Column<int>(type: "int", nullable: false),
                Tipo = table.Column<int>(type: "int", nullable: false),
                Quantidade = table.Column<int>(type: "int", nullable: false),
                DataUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                Observacao = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MovimentosEstoque", x => x.Id);
                table.ForeignKey("FK_MovimentosEstoque_Produtos_ProdutoId", x => x.ProdutoId, "Produtos", "Id", onDelete: ReferentialAction.Restrict);
            });
        migrationBuilder.CreateIndex(name: "IX_MovimentosEstoque_ProdutoId_DataUtc", table: "MovimentosEstoque", columns: new[] { "ProdutoId", "DataUtc" });
        migrationBuilder.Sql("INSERT INTO MovimentosEstoque (ProdutoId, Tipo, Quantidade, DataUtc, Observacao) SELECT Id, 1, Estoque, SYSUTCDATETIME(), N'Saldo anterior à implantação do histórico' FROM Produtos WHERE Estoque > 0");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "MovimentosEstoque");
        migrationBuilder.DropColumn(name: "EstoqueMinimo", table: "Produtos");
        migrationBuilder.DropColumn(name: "Versao", table: "Produtos");
    }
}
