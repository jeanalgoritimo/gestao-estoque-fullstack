using GestaoEstoque.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace GestaoEstoque.Infrastructure.Persistence.Migrations;
[DbContext(typeof(GestaoEstoqueDbContext))]
[Migration("20261001143000_CostCenters")]
public partial class CostCenters : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.CreateTable("CentrosCusto", t => new
        {
            Id = t.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
            Nome = t.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
            NomeNormalizado = t.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
            Ativo = t.Column<bool>(type: "bit", nullable: false)
        }, constraints: t => t.PrimaryKey("PK_CentrosCusto", c => c.Id));
        m.CreateIndex("IX_CentrosCusto_NomeNormalizado", "CentrosCusto", "NomeNormalizado", unique: true);
        m.AddColumn<int>(name: "CentroCustoId", table: "RequisicoesMaterial", type: "int", nullable: true);
        m.CreateIndex("IX_RequisicoesMaterial_CentroCustoId", "RequisicoesMaterial", "CentroCustoId");
        m.AddForeignKey(name: "FK_RequisicoesMaterial_CentrosCusto_CentroCustoId", table: "RequisicoesMaterial",
            column: "CentroCustoId", principalTable: "CentrosCusto", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
    }
    protected override void Down(MigrationBuilder m)
    {
        m.DropForeignKey("FK_RequisicoesMaterial_CentrosCusto_CentroCustoId", "RequisicoesMaterial");
        m.DropIndex("IX_RequisicoesMaterial_CentroCustoId", "RequisicoesMaterial");
        m.DropColumn("CentroCustoId", "RequisicoesMaterial"); m.DropTable("CentrosCusto");
    }
}
