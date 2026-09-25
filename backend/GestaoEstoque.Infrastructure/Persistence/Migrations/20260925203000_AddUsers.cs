using GestaoEstoque.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestaoEstoque.Infrastructure.Persistence.Migrations;

[DbContext(typeof(GestaoEstoqueDbContext))]
[Migration("20260925203000_AddUsers")]
public partial class AddUsers : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Usuarios",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                Nome = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                SenhaHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                Perfil = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                VersaoSeguranca = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                Ativo = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Usuarios", x => x.Id));
        migrationBuilder.CreateIndex(name: "IX_Usuarios_Email", table: "Usuarios", column: "Email", unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("Usuarios");
}
