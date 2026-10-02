using GestaoEstoque.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace GestaoEstoque.Infrastructure.Persistence.Migrations;
[DbContext(typeof(GestaoEstoqueDbContext))]
[Migration("20261002140000_StockTransfers")]
public partial class StockTransfers : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.CreateTable(name: "SaldosLocais", columns: t => new
        {
            Id = t.Column<long>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
            ProdutoId = t.Column<int>(nullable: false), PosicaoEstoqueId = t.Column<int>(nullable: true),
            Quantidade = t.Column<decimal>(type: "decimal(13,3)", precision: 13, scale: 3, nullable: false),
            Reservado = t.Column<decimal>(type: "decimal(13,3)", precision: 13, scale: 3, nullable: false),
            Versao = t.Column<byte[]>(rowVersion: true, nullable: true)
        }, constraints: t =>
        {
            t.PrimaryKey("PK_SaldosLocais", x => x.Id);
            t.ForeignKey("FK_SaldosLocais_Produtos_ProdutoId", x => x.ProdutoId, "Produtos", "Id", onDelete: ReferentialAction.Restrict);
            t.ForeignKey("FK_SaldosLocais_PosicoesEstoque_PosicaoEstoqueId", x => x.PosicaoEstoqueId, "PosicoesEstoque", "Id", onDelete: ReferentialAction.Restrict);
            t.CheckConstraint("CK_SaldosLocais_Quantidades", "[Quantidade] >= 0 AND [Reservado] >= 0 AND [Reservado] <= [Quantidade]");
        });
        m.CreateIndex("IX_SaldosLocais_PosicaoEstoqueId", "SaldosLocais", "PosicaoEstoqueId");
        m.CreateIndex("IX_SaldosLocais_ProdutoSemLocal", "SaldosLocais", "ProdutoId", unique: true, filter: "[PosicaoEstoqueId] IS NULL");
        m.CreateIndex("IX_SaldosLocais_ProdutoId_PosicaoEstoqueId", "SaldosLocais", new[] { "ProdutoId", "PosicaoEstoqueId" }, unique: true, filter: "[PosicaoEstoqueId] IS NOT NULL");
        // Distribuição inicial preserva exatamente saldos e reservas; não cria movimentações fictícias.
        m.Sql("INSERT INTO [SaldosLocais] ([ProdutoId], [PosicaoEstoqueId], [Quantidade], [Reservado]) SELECT [Id], [PosicaoEstoqueId], [Estoque], [EstoqueReservado] FROM [Produtos];");
        m.CreateTable(name: "TransferenciasEstoque", columns: t => new
        {
            Id = t.Column<long>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
            ProdutoId = t.Column<int>(nullable: false), OrigemId = t.Column<int>(nullable: true), DestinoId = t.Column<int>(nullable: false),
            Quantidade = t.Column<decimal>(type: "decimal(13,3)", precision: 13, scale: 3, nullable: false),
            DataUtc = t.Column<DateTime>(nullable: false), UsuarioId = t.Column<int>(nullable: false),
            UsuarioNome = t.Column<string>(maxLength: 120, nullable: false), Motivo = t.Column<string>(maxLength: 150, nullable: false),
            OrigemDescricao = t.Column<string>(maxLength: 300, nullable: false), DestinoDescricao = t.Column<string>(maxLength: 300, nullable: false)
        }, constraints: t =>
        {
            t.PrimaryKey("PK_TransferenciasEstoque", x => x.Id);
            t.ForeignKey("FK_TransferenciasEstoque_Produtos_ProdutoId", x => x.ProdutoId, "Produtos", "Id", onDelete: ReferentialAction.Restrict);
            t.ForeignKey("FK_TransferenciasEstoque_Usuarios_UsuarioId", x => x.UsuarioId, "Usuarios", "Id", onDelete: ReferentialAction.Restrict);
            t.ForeignKey("FK_TransferenciasEstoque_PosicoesEstoque_OrigemId", x => x.OrigemId, "PosicoesEstoque", "Id", onDelete: ReferentialAction.Restrict);
            t.ForeignKey("FK_TransferenciasEstoque_PosicoesEstoque_DestinoId", x => x.DestinoId, "PosicoesEstoque", "Id", onDelete: ReferentialAction.Restrict);
        });
        m.CreateIndex("IX_TransferenciasEstoque_OrigemId", "TransferenciasEstoque", "OrigemId");
        m.CreateIndex("IX_TransferenciasEstoque_DestinoId", "TransferenciasEstoque", "DestinoId");
        m.CreateIndex("IX_TransferenciasEstoque_UsuarioId", "TransferenciasEstoque", "UsuarioId");
        m.CreateIndex("IX_TransferenciasEstoque_ProdutoId_DataUtc", "TransferenciasEstoque", new[] { "ProdutoId", "DataUtc" });
    }
    protected override void Down(MigrationBuilder m)
    {
        m.Sql("IF EXISTS (SELECT 1 FROM [TransferenciasEstoque]) THROW 51000, 'Há transferências registradas; reversão bloqueada para preservar saldos e histórico por local.', 1;");
        m.DropTable("TransferenciasEstoque"); m.DropTable("SaldosLocais");
    }
}
