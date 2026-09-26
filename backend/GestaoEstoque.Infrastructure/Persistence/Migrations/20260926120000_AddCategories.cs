using GestaoEstoque.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestaoEstoque.Infrastructure.Persistence.Migrations;

[DbContext(typeof(GestaoEstoqueDbContext))]
[Migration("20260926120000_AddCategories")]
public partial class AddCategories : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CategoriasProduto",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                NomeNormalizado = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Ativo = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_CategoriasProduto", x => x.Id));
        migrationBuilder.CreateIndex(name: "IX_CategoriasProduto_NomeNormalizado", table: "CategoriasProduto", column: "NomeNormalizado", unique: true);

        migrationBuilder.AddColumn<int>(name: "CategoriaId", table: "Produtos", type: "int", nullable: true);
        migrationBuilder.Sql(@"
INSERT INTO CategoriasProduto (Nome, NomeNormalizado, Ativo)
SELECT MIN(LTRIM(RTRIM(Categoria))), UPPER(LTRIM(RTRIM(Categoria))), 1
FROM Produtos
GROUP BY UPPER(LTRIM(RTRIM(Categoria)));
UPDATE p SET CategoriaId = c.Id
FROM Produtos p JOIN CategoriasProduto c
ON UPPER(LTRIM(RTRIM(p.Categoria))) = c.NomeNormalizado;");
        migrationBuilder.AlterColumn<int>(name: "CategoriaId", table: "Produtos", type: "int", nullable: false, oldClrType: typeof(int), oldType: "int", oldNullable: true);
        migrationBuilder.CreateIndex(name: "IX_Produtos_CategoriaId", table: "Produtos", column: "CategoriaId");
        migrationBuilder.AddForeignKey(name: "FK_Produtos_CategoriasProduto_CategoriaId", table: "Produtos", column: "CategoriaId", principalTable: "CategoriasProduto", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        migrationBuilder.DropColumn(name: "Categoria", table: "Produtos");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "Categoria", table: "Produtos", type: "nvarchar(100)", maxLength: 100, nullable: true);
        migrationBuilder.Sql("UPDATE p SET Categoria = c.Nome FROM Produtos p JOIN CategoriasProduto c ON p.CategoriaId = c.Id");
        migrationBuilder.AlterColumn<string>(name: "Categoria", table: "Produtos", type: "nvarchar(100)", maxLength: 100, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(100)", oldMaxLength: 100, oldNullable: true);
        migrationBuilder.DropForeignKey(name: "FK_Produtos_CategoriasProduto_CategoriaId", table: "Produtos");
        migrationBuilder.DropIndex(name: "IX_Produtos_CategoriaId", table: "Produtos");
        migrationBuilder.DropColumn(name: "CategoriaId", table: "Produtos");
        migrationBuilder.DropTable(name: "CategoriasProduto");
    }
}
