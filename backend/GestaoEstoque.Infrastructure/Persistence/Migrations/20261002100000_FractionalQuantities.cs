using GestaoEstoque.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace GestaoEstoque.Infrastructure.Persistence.Migrations;
[DbContext(typeof(GestaoEstoqueDbContext))]
[Migration("20261002100000_FractionalQuantities")]
public partial class FractionalQuantities : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.AlterColumn<decimal>(name: "Estoque", table: "Produtos", type: "decimal(13,3)", precision: 13, scale: 3, nullable: false, oldClrType: typeof(int), oldType: "int", oldNullable: false);
        m.AlterColumn<decimal>(name: "EstoqueReservado", table: "Produtos", type: "decimal(13,3)", precision: 13, scale: 3, nullable: false, oldClrType: typeof(int), oldType: "int", oldNullable: false);
        m.AlterColumn<decimal>(name: "EstoqueMinimo", table: "Produtos", type: "decimal(13,3)", precision: 13, scale: 3, nullable: false, oldClrType: typeof(int), oldType: "int", oldNullable: false);
        m.AlterColumn<decimal>(name: "Quantidade", table: "MovimentosEstoque", type: "decimal(13,3)", precision: 13, scale: 3, nullable: false, oldClrType: typeof(int), oldType: "int", oldNullable: false);
        m.AlterColumn<decimal>(name: "SaldoApos", table: "MovimentosEstoque", type: "decimal(13,3)", precision: 13, scale: 3, nullable: true, oldClrType: typeof(int), oldType: "int", oldNullable: true);
        m.AlterColumn<decimal>(name: "SaldoInicial", table: "InventariosFisicos", type: "decimal(13,3)", precision: 13, scale: 3, nullable: false, oldClrType: typeof(int), oldType: "int", oldNullable: false);
        m.AlterColumn<decimal>(name: "QuantidadeContada", table: "InventariosFisicos", type: "decimal(13,3)", precision: 13, scale: 3, nullable: true, oldClrType: typeof(int), oldType: "int", oldNullable: true);
        m.AlterColumn<decimal>(name: "Quantidade", table: "ItensPedidoCompra", type: "decimal(13,3)", precision: 13, scale: 3, nullable: false, oldClrType: typeof(int), oldType: "int", oldNullable: false);
        m.AlterColumn<decimal>(name: "QuantidadeRecebida", table: "ItensPedidoCompra", type: "decimal(13,3)", precision: 13, scale: 3, nullable: false, oldClrType: typeof(int), oldType: "int", oldNullable: false);
        m.AlterColumn<decimal>(name: "Quantidade", table: "ItensRequisicaoMaterial", type: "decimal(13,3)", precision: 13, scale: 3, nullable: false, oldClrType: typeof(int), oldType: "int", oldNullable: false);
        m.AlterColumn<decimal>(name: "QuantidadeEntregue", table: "ItensRequisicaoMaterial", type: "decimal(13,3)", precision: 13, scale: 3, nullable: false, oldClrType: typeof(int), oldType: "int", oldNullable: false);
        m.AlterColumn<decimal>(name: "QuantidadeDevolvida", table: "ItensRequisicaoMaterial", type: "decimal(13,3)", precision: 13, scale: 3, nullable: false, oldClrType: typeof(int), oldType: "int", oldNullable: false);
    }
    protected override void Down(MigrationBuilder m)
    {
        m.Sql("IF EXISTS (SELECT 1 FROM [Produtos] WHERE ([Estoque] <> ROUND([Estoque], 0) OR [Estoque] > 2147483647 OR [Estoque] < -2147483648) OR ([EstoqueReservado] <> ROUND([EstoqueReservado], 0) OR [EstoqueReservado] > 2147483647 OR [EstoqueReservado] < -2147483648) OR ([EstoqueMinimo] <> ROUND([EstoqueMinimo], 0) OR [EstoqueMinimo] > 2147483647 OR [EstoqueMinimo] < -2147483648)) THROW 51000, 'Existem quantidades fracionadas ou fora do limite inteiro; reversão bloqueada para preservar dados.', 1;");
        m.Sql("IF EXISTS (SELECT 1 FROM [MovimentosEstoque] WHERE ([Quantidade] <> ROUND([Quantidade], 0) OR [Quantidade] > 2147483647 OR [Quantidade] < -2147483648) OR ([SaldoApos] <> ROUND([SaldoApos], 0) OR [SaldoApos] > 2147483647 OR [SaldoApos] < -2147483648)) THROW 51000, 'Existem quantidades fracionadas ou fora do limite inteiro; reversão bloqueada para preservar dados.', 1;");
        m.Sql("IF EXISTS (SELECT 1 FROM [InventariosFisicos] WHERE ([SaldoInicial] <> ROUND([SaldoInicial], 0) OR [SaldoInicial] > 2147483647 OR [SaldoInicial] < -2147483648) OR ([QuantidadeContada] <> ROUND([QuantidadeContada], 0) OR [QuantidadeContada] > 2147483647 OR [QuantidadeContada] < -2147483648)) THROW 51000, 'Existem quantidades fracionadas ou fora do limite inteiro; reversão bloqueada para preservar dados.', 1;");
        m.Sql("IF EXISTS (SELECT 1 FROM [ItensPedidoCompra] WHERE ([Quantidade] <> ROUND([Quantidade], 0) OR [Quantidade] > 2147483647 OR [Quantidade] < -2147483648) OR ([QuantidadeRecebida] <> ROUND([QuantidadeRecebida], 0) OR [QuantidadeRecebida] > 2147483647 OR [QuantidadeRecebida] < -2147483648)) THROW 51000, 'Existem quantidades fracionadas ou fora do limite inteiro; reversão bloqueada para preservar dados.', 1;");
        m.Sql("IF EXISTS (SELECT 1 FROM [ItensRequisicaoMaterial] WHERE ([Quantidade] <> ROUND([Quantidade], 0) OR [Quantidade] > 2147483647 OR [Quantidade] < -2147483648) OR ([QuantidadeEntregue] <> ROUND([QuantidadeEntregue], 0) OR [QuantidadeEntregue] > 2147483647 OR [QuantidadeEntregue] < -2147483648) OR ([QuantidadeDevolvida] <> ROUND([QuantidadeDevolvida], 0) OR [QuantidadeDevolvida] > 2147483647 OR [QuantidadeDevolvida] < -2147483648)) THROW 51000, 'Existem quantidades fracionadas ou fora do limite inteiro; reversão bloqueada para preservar dados.', 1;");
        m.AlterColumn<int>(name: "Estoque", table: "Produtos", type: "int", nullable: false, oldClrType: typeof(decimal), oldType: "decimal(13,3)", oldPrecision: 13, oldScale: 3, oldNullable: false);
        m.AlterColumn<int>(name: "EstoqueReservado", table: "Produtos", type: "int", nullable: false, oldClrType: typeof(decimal), oldType: "decimal(13,3)", oldPrecision: 13, oldScale: 3, oldNullable: false);
        m.AlterColumn<int>(name: "EstoqueMinimo", table: "Produtos", type: "int", nullable: false, oldClrType: typeof(decimal), oldType: "decimal(13,3)", oldPrecision: 13, oldScale: 3, oldNullable: false);
        m.AlterColumn<int>(name: "Quantidade", table: "MovimentosEstoque", type: "int", nullable: false, oldClrType: typeof(decimal), oldType: "decimal(13,3)", oldPrecision: 13, oldScale: 3, oldNullable: false);
        m.AlterColumn<int>(name: "SaldoApos", table: "MovimentosEstoque", type: "int", nullable: true, oldClrType: typeof(decimal), oldType: "decimal(13,3)", oldPrecision: 13, oldScale: 3, oldNullable: true);
        m.AlterColumn<int>(name: "SaldoInicial", table: "InventariosFisicos", type: "int", nullable: false, oldClrType: typeof(decimal), oldType: "decimal(13,3)", oldPrecision: 13, oldScale: 3, oldNullable: false);
        m.AlterColumn<int>(name: "QuantidadeContada", table: "InventariosFisicos", type: "int", nullable: true, oldClrType: typeof(decimal), oldType: "decimal(13,3)", oldPrecision: 13, oldScale: 3, oldNullable: true);
        m.AlterColumn<int>(name: "Quantidade", table: "ItensPedidoCompra", type: "int", nullable: false, oldClrType: typeof(decimal), oldType: "decimal(13,3)", oldPrecision: 13, oldScale: 3, oldNullable: false);
        m.AlterColumn<int>(name: "QuantidadeRecebida", table: "ItensPedidoCompra", type: "int", nullable: false, oldClrType: typeof(decimal), oldType: "decimal(13,3)", oldPrecision: 13, oldScale: 3, oldNullable: false);
        m.AlterColumn<int>(name: "Quantidade", table: "ItensRequisicaoMaterial", type: "int", nullable: false, oldClrType: typeof(decimal), oldType: "decimal(13,3)", oldPrecision: 13, oldScale: 3, oldNullable: false);
        m.AlterColumn<int>(name: "QuantidadeEntregue", table: "ItensRequisicaoMaterial", type: "int", nullable: false, oldClrType: typeof(decimal), oldType: "decimal(13,3)", oldPrecision: 13, oldScale: 3, oldNullable: false);
        m.AlterColumn<int>(name: "QuantidadeDevolvida", table: "ItensRequisicaoMaterial", type: "int", nullable: false, oldClrType: typeof(decimal), oldType: "decimal(13,3)", oldPrecision: 13, oldScale: 3, oldNullable: false);
    }
}
