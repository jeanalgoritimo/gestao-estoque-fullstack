using GestaoEstoque.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace GestaoEstoque.Infrastructure.Persistence.Migrations;

[DbContext(typeof(GestaoEstoqueDbContext))]
[Migration("20260930173000_MaterialRequisitions")]
public partial class MaterialRequisitions : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.AddColumn<int>(name: "EstoqueReservado", table: "Produtos", type: "int", nullable: false, defaultValue: 0);
        m.CreateTable("RequisicoesMaterial", t => new
        {
            Id = t.Column<long>(type: "bigint", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
            Finalidade = t.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
            SolicitanteId = t.Column<int>(type: "int", nullable: false),
            SolicitanteNome = t.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
            CriadoUtc = t.Column<DateTime>(type: "datetime2", nullable: false),
            Situacao = t.Column<int>(type: "int", nullable: false),
            AprovadoUtc = t.Column<DateTime>(type: "datetime2", nullable: true),
            AprovadoPorId = t.Column<int>(type: "int", nullable: true),
            AprovadoPorNome = t.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
            EncerradoUtc = t.Column<DateTime>(type: "datetime2", nullable: true),
            EncerradoPorId = t.Column<int>(type: "int", nullable: true),
            EncerradoPorNome = t.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
            MotivoCancelamento = t.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
            Versao = t.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
        }, constraints: t =>
        {
            t.PrimaryKey("PK_RequisicoesMaterial", r => r.Id);
            t.ForeignKey("FK_RequisicoesMaterial_Usuarios_SolicitanteId", r => r.SolicitanteId, "Usuarios", "Id", onDelete: ReferentialAction.Restrict);
            t.ForeignKey("FK_RequisicoesMaterial_Usuarios_AprovadoPorId", r => r.AprovadoPorId, "Usuarios", "Id", onDelete: ReferentialAction.Restrict);
            t.ForeignKey("FK_RequisicoesMaterial_Usuarios_EncerradoPorId", r => r.EncerradoPorId, "Usuarios", "Id", onDelete: ReferentialAction.Restrict);
        });
        m.CreateIndex("IX_RequisicoesMaterial_SolicitanteId", "RequisicoesMaterial", "SolicitanteId");
        m.CreateIndex("IX_RequisicoesMaterial_AprovadoPorId", "RequisicoesMaterial", "AprovadoPorId");
        m.CreateIndex("IX_RequisicoesMaterial_EncerradoPorId", "RequisicoesMaterial", "EncerradoPorId");
        m.CreateTable("ItensRequisicaoMaterial", t => new
        {
            Id = t.Column<long>(type: "bigint", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
            RequisicaoMaterialId = t.Column<long>(type: "bigint", nullable: false),
            ProdutoId = t.Column<int>(type: "int", nullable: false),
            Quantidade = t.Column<int>(type: "int", nullable: false),
            QuantidadeEntregue = t.Column<int>(type: "int", nullable: false)
        }, constraints: t =>
        {
            t.PrimaryKey("PK_ItensRequisicaoMaterial", i => i.Id);
            t.ForeignKey("FK_ItensRequisicaoMaterial_RequisicoesMaterial_RequisicaoMaterialId", i => i.RequisicaoMaterialId,
                "RequisicoesMaterial", "Id", onDelete: ReferentialAction.Cascade);
            t.ForeignKey("FK_ItensRequisicaoMaterial_Produtos_ProdutoId", i => i.ProdutoId, "Produtos", "Id", onDelete: ReferentialAction.Restrict);
        });
        m.CreateIndex("IX_ItensRequisicaoMaterial_RequisicaoMaterialId_ProdutoId", "ItensRequisicaoMaterial", new[] { "RequisicaoMaterialId", "ProdutoId" }, unique: true);
        m.CreateIndex("IX_ItensRequisicaoMaterial_ProdutoId", "ItensRequisicaoMaterial", "ProdutoId");
        m.AddColumn<long>(name: "RequisicaoMaterialId", table: "MovimentosEstoque", type: "bigint", nullable: true);
        m.AddColumn<Guid>(name: "EntregaId", table: "MovimentosEstoque", type: "uniqueidentifier", nullable: true);
        m.CreateIndex("IX_MovimentosEstoque_RequisicaoMaterialId_EntregaId", "MovimentosEstoque", new[] { "RequisicaoMaterialId", "EntregaId" });
        m.AddForeignKey(name: "FK_MovimentosEstoque_RequisicoesMaterial_RequisicaoMaterialId", table: "MovimentosEstoque",
            column: "RequisicaoMaterialId", principalTable: "RequisicoesMaterial", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
    }
    protected override void Down(MigrationBuilder m)
    {
        m.DropForeignKey("FK_MovimentosEstoque_RequisicoesMaterial_RequisicaoMaterialId", "MovimentosEstoque");
        m.DropIndex("IX_MovimentosEstoque_RequisicaoMaterialId_EntregaId", "MovimentosEstoque");
        m.DropColumn("RequisicaoMaterialId", "MovimentosEstoque"); m.DropColumn("EntregaId", "MovimentosEstoque");
        m.DropTable("ItensRequisicaoMaterial"); m.DropTable("RequisicoesMaterial");
        m.DropColumn("EstoqueReservado", "Produtos");
    }
}
