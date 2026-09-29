using GestaoEstoque.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestaoEstoque.Infrastructure.Persistence.Migrations;

[DbContext(typeof(GestaoEstoqueDbContext))]
[Migration("20260929110000_MovementTraceability")]
public partial class MovementTraceability : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>("DataEfetivaUtc", "MovimentosEstoque", type: "datetime2", nullable: false, defaultValue: new DateTime(1, 1, 1));
        migrationBuilder.AddColumn<string>("DocumentoOrigem", "MovimentosEstoque", type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>("Motivo", "MovimentosEstoque", type: "nvarchar(150)", maxLength: 150, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<int>("UsuarioId", "MovimentosEstoque", type: "int", nullable: true);
        migrationBuilder.AddColumn<string>("UsuarioNome", "MovimentosEstoque", type: "nvarchar(120)", maxLength: 120, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<decimal>("CustoUnitario", "MovimentosEstoque", type: "decimal(18,4)", nullable: true);
        migrationBuilder.AddColumn<int>("SaldoApos", "MovimentosEstoque", type: "int", nullable: true);
        migrationBuilder.Sql("UPDATE MovimentosEstoque SET DataEfetivaUtc = DataUtc, Motivo = COALESCE(NULLIF(Observacao, N''), N'Movimentação anterior à rastreabilidade')");
        migrationBuilder.CreateIndex("IX_MovimentosEstoque_UsuarioId", "MovimentosEstoque", "UsuarioId");
        migrationBuilder.AddForeignKey("FK_MovimentosEstoque_Usuarios_UsuarioId", "MovimentosEstoque", "UsuarioId", "Usuarios", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey("FK_MovimentosEstoque_Usuarios_UsuarioId", "MovimentosEstoque");
        migrationBuilder.DropIndex("IX_MovimentosEstoque_UsuarioId", "MovimentosEstoque");
        foreach (var column in new[] { "DataEfetivaUtc", "DocumentoOrigem", "Motivo", "UsuarioId", "UsuarioNome", "CustoUnitario", "SaldoApos" })
            migrationBuilder.DropColumn(column, "MovimentosEstoque");
    }
}
