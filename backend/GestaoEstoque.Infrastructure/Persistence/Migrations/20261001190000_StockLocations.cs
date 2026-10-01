using GestaoEstoque.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace GestaoEstoque.Infrastructure.Persistence.Migrations;
[DbContext(typeof(GestaoEstoqueDbContext))]
[Migration("20261001190000_StockLocations")]
public partial class StockLocations : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.CreateTable(name: "Almoxarifados", columns: t => new
        {
            Id = t.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
            Nome = t.Column<string>(maxLength: 100, nullable: false),
            NomeNormalizado = t.Column<string>(maxLength: 100, nullable: false),
            Ativo = t.Column<bool>(nullable: false)
        }, constraints: t => t.PrimaryKey("PK_Almoxarifados", x => x.Id));
        m.CreateTable(name: "PosicoesEstoque", columns: t => new
        {
            Id = t.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
            AlmoxarifadoId = t.Column<int>(nullable: false),
            Corredor = t.Column<string>(maxLength: 40, nullable: false),
            Estante = t.Column<string>(maxLength: 40, nullable: false),
            Prateleira = t.Column<string>(maxLength: 40, nullable: false),
            CorredorNormalizado = t.Column<string>(maxLength: 40, nullable: false),
            EstanteNormalizada = t.Column<string>(maxLength: 40, nullable: false),
            PrateleiraNormalizada = t.Column<string>(maxLength: 40, nullable: false),
            Ativo = t.Column<bool>(nullable: false)
        }, constraints: t =>
        {
            t.PrimaryKey("PK_PosicoesEstoque", x => x.Id);
            t.ForeignKey(name: "FK_PosicoesEstoque_Almoxarifados_AlmoxarifadoId", column: x => x.AlmoxarifadoId,
                principalTable: "Almoxarifados", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        });
        m.CreateIndex("IX_Almoxarifados_NomeNormalizado", "Almoxarifados", "NomeNormalizado", unique: true);
        m.CreateIndex("IX_PosicoesEstoque_AlmoxarifadoId_CorredorNormalizado_EstanteNormalizada_PrateleiraNormalizada",
            "PosicoesEstoque", new[] { "AlmoxarifadoId", "CorredorNormalizado", "EstanteNormalizada", "PrateleiraNormalizada" }, unique: true);
        m.AddColumn<int>(name: "PosicaoEstoqueId", table: "Produtos", type: "int", nullable: true);
        m.CreateIndex("IX_Produtos_PosicaoEstoqueId", "Produtos", "PosicaoEstoqueId");
        m.AddForeignKey(name: "FK_Produtos_PosicoesEstoque_PosicaoEstoqueId", table: "Produtos", column: "PosicaoEstoqueId",
            principalTable: "PosicoesEstoque", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
    }
    protected override void Down(MigrationBuilder m)
    {
        m.DropForeignKey("FK_Produtos_PosicoesEstoque_PosicaoEstoqueId", "Produtos");
        m.DropIndex("IX_Produtos_PosicaoEstoqueId", "Produtos"); m.DropColumn("PosicaoEstoqueId", "Produtos");
        m.DropTable("PosicoesEstoque"); m.DropTable("Almoxarifados");
    }
}
