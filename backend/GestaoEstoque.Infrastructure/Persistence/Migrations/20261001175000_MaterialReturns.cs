using GestaoEstoque.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace GestaoEstoque.Infrastructure.Persistence.Migrations;
[DbContext(typeof(GestaoEstoqueDbContext))]
[Migration("20261001175000_MaterialReturns")]
public partial class MaterialReturns : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.AddColumn<int>(name: "QuantidadeDevolvida", table: "ItensRequisicaoMaterial", type: "int", nullable: false, defaultValue: 0);
        m.AddColumn<Guid>(name: "DevolucaoId", table: "MovimentosEstoque", type: "uniqueidentifier", nullable: true);
        m.CreateIndex("IX_MovimentosEstoque_RequisicaoMaterialId_DevolucaoId", "MovimentosEstoque", new[] { "RequisicaoMaterialId", "DevolucaoId" });
    }
    protected override void Down(MigrationBuilder m)
    {
        m.DropIndex("IX_MovimentosEstoque_RequisicaoMaterialId_DevolucaoId", "MovimentosEstoque");
        m.DropColumn("DevolucaoId", "MovimentosEstoque"); m.DropColumn("QuantidadeDevolvida", "ItensRequisicaoMaterial");
    }
}
