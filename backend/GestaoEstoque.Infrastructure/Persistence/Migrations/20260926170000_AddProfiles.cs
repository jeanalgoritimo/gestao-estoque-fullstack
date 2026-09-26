using GestaoEstoque.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestaoEstoque.Infrastructure.Persistence.Migrations;

[DbContext(typeof(GestaoEstoqueDbContext))]
[Migration("20260926170000_AddProfiles")]
public partial class AddProfiles : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "PerfisAcesso",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                Nome = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                NomeNormalizado = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                Sistema = table.Column<bool>(type: "bit", nullable: false),
                Ativo = table.Column<bool>(type: "bit", nullable: false),
                GerenciarProdutos = table.Column<bool>(type: "bit", nullable: false),
                GerenciarCategorias = table.Column<bool>(type: "bit", nullable: false),
                MovimentarEstoque = table.Column<bool>(type: "bit", nullable: false),
                VersaoSeguranca = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_PerfisAcesso", x => x.Id));
        migrationBuilder.CreateIndex(name: "IX_PerfisAcesso_NomeNormalizado", table: "PerfisAcesso", column: "NomeNormalizado", unique: true);
        migrationBuilder.Sql(@"
SET IDENTITY_INSERT PerfisAcesso ON;
INSERT INTO PerfisAcesso (Id, Nome, NomeNormalizado, Sistema, Ativo, GerenciarProdutos, GerenciarCategorias, MovimentarEstoque, VersaoSeguranca)
VALUES (1, N'Administrador', N'ADMINISTRADOR', 1, 1, 1, 1, 1, '11111111111111111111111111111111'),
       (2, N'Operador', N'OPERADOR', 1, 1, 0, 0, 1, '22222222222222222222222222222222');
SET IDENTITY_INSERT PerfisAcesso OFF;");
        migrationBuilder.AddColumn<int>(name: "PerfilId", table: "Usuarios", type: "int", nullable: true);
        migrationBuilder.Sql("UPDATE Usuarios SET PerfilId = CASE WHEN Perfil = N'Administrador' THEN 1 ELSE 2 END");
        migrationBuilder.AlterColumn<int>(name: "PerfilId", table: "Usuarios", type: "int", nullable: false, oldClrType: typeof(int), oldType: "int", oldNullable: true);
        migrationBuilder.CreateIndex(name: "IX_Usuarios_PerfilId", table: "Usuarios", column: "PerfilId");
        migrationBuilder.AddForeignKey(name: "FK_Usuarios_PerfisAcesso_PerfilId", table: "Usuarios", column: "PerfilId", principalTable: "PerfisAcesso", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        migrationBuilder.DropColumn(name: "Perfil", table: "Usuarios");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "Perfil", table: "Usuarios", type: "nvarchar(20)", maxLength: 20, nullable: true);
        migrationBuilder.Sql("UPDATE u SET Perfil = CASE WHEN p.Id = 1 THEN N'Administrador' ELSE N'Operador' END FROM Usuarios u JOIN PerfisAcesso p ON u.PerfilId = p.Id");
        migrationBuilder.AlterColumn<string>(name: "Perfil", table: "Usuarios", type: "nvarchar(20)", maxLength: 20, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(20)", oldMaxLength: 20, oldNullable: true);
        migrationBuilder.DropForeignKey(name: "FK_Usuarios_PerfisAcesso_PerfilId", table: "Usuarios");
        migrationBuilder.DropIndex(name: "IX_Usuarios_PerfilId", table: "Usuarios");
        migrationBuilder.DropColumn(name: "PerfilId", table: "Usuarios");
        migrationBuilder.DropTable(name: "PerfisAcesso");
    }
}
