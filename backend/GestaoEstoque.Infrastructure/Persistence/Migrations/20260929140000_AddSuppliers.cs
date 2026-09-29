using GestaoEstoque.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestaoEstoque.Infrastructure.Persistence.Migrations;

[DbContext(typeof(GestaoEstoqueDbContext))]
[Migration("20260929140000_AddSuppliers")]
public partial class AddSuppliers : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Fornecedores",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                Nome = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                NomeNormalizado = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                Contato = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: true),
                Telefone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                Ativo = table.Column<bool>(type: "bit", nullable: false)
            }, constraints: table => table.PrimaryKey("PK_Fornecedores", x => x.Id));
        migrationBuilder.CreateIndex("IX_Fornecedores_NomeNormalizado", "Fornecedores", "NomeNormalizado", unique: true);
        migrationBuilder.AddColumn<int>("FornecedorId", "Produtos", type: "int", nullable: true);
        migrationBuilder.CreateIndex("IX_Produtos_FornecedorId", "Produtos", "FornecedorId");
        migrationBuilder.AddForeignKey("FK_Produtos_Fornecedores_FornecedorId", "Produtos", "FornecedorId", "Fornecedores", "Id", onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey("FK_Produtos_Fornecedores_FornecedorId", "Produtos");
        migrationBuilder.DropIndex("IX_Produtos_FornecedorId", "Produtos");
        migrationBuilder.DropColumn("FornecedorId", "Produtos");
        migrationBuilder.DropTable("Fornecedores");
    }
}
