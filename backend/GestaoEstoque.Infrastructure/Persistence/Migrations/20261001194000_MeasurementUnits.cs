using GestaoEstoque.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace GestaoEstoque.Infrastructure.Persistence.Migrations;
[DbContext(typeof(GestaoEstoqueDbContext))]
[Migration("20261001194000_MeasurementUnits")]
public partial class MeasurementUnits : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.CreateTable(name: "UnidadesMedida", columns: t => new
        {
            Id = t.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
            Sigla = t.Column<string>(maxLength: 10, nullable: false),
            Nome = t.Column<string>(maxLength: 80, nullable: false),
            Ativo = t.Column<bool>(nullable: false), Sistema = t.Column<bool>(nullable: false)
        }, constraints: t => t.PrimaryKey("PK_UnidadesMedida", x => x.Id));
        m.CreateIndex("IX_UnidadesMedida_Sigla", "UnidadesMedida", "Sigla", unique: true);
        m.InsertData(table: "UnidadesMedida", columns: new[] { "Id", "Sigla", "Nome", "Ativo", "Sistema" }, values: new object[] { 1, "UN", "Unidade", true, true });
        m.AddColumn<int>(name: "UnidadeMedidaId", table: "Produtos", type: "int", nullable: false, defaultValue: 1);
        m.CreateIndex("IX_Produtos_UnidadeMedidaId", "Produtos", "UnidadeMedidaId");
        m.AddForeignKey(name: "FK_Produtos_UnidadesMedida_UnidadeMedidaId", table: "Produtos", column: "UnidadeMedidaId",
            principalTable: "UnidadesMedida", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
    }
    protected override void Down(MigrationBuilder m)
    {
        m.DropForeignKey("FK_Produtos_UnidadesMedida_UnidadeMedidaId", "Produtos");
        m.DropIndex("IX_Produtos_UnidadeMedidaId", "Produtos"); m.DropColumn("UnidadeMedidaId", "Produtos"); m.DropTable("UnidadesMedida");
    }
}
