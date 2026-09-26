using GestaoEstoque.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestaoEstoque.Infrastructure.Persistence.Migrations;

[DbContext(typeof(GestaoEstoqueDbContext))]
[Migration("20260926173000_AllowOperatorRegistration")]
public partial class AllowOperatorRegistration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(name: "CadastrarProdutos", table: "PerfisAcesso", type: "bit", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<bool>(name: "CadastrarCategorias", table: "PerfisAcesso", type: "bit", nullable: false, defaultValue: false);
        migrationBuilder.Sql(@"
UPDATE PerfisAcesso
SET CadastrarProdutos = GerenciarProdutos, CadastrarCategorias = GerenciarCategorias,
    VersaoSeguranca = REPLACE(CONVERT(varchar(36), NEWID()), '-', '');
UPDATE PerfisAcesso
SET CadastrarProdutos = 1, CadastrarCategorias = 1,
    VersaoSeguranca = REPLACE(CONVERT(varchar(36), NEWID()), '-', '')
WHERE NomeNormalizado IN (N'ADMINISTRADOR', N'OPERADOR') AND Sistema = 1;");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "CadastrarProdutos", table: "PerfisAcesso");
        migrationBuilder.DropColumn(name: "CadastrarCategorias", table: "PerfisAcesso");
    }
}
