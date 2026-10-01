# Devoluções de materiais

Em **Requisições**, administrador e operador com permissão de movimentar estoque podem usar **Devolver materiais** quando existe quantidade entregue ainda não devolvida. Informe quantidades inteiras positivas e motivo obrigatório de até 150 caracteres. Exemplo: entregou 10 e devolveu 3; entregue permanece 10, devolvido passa a 3, entregue líquido fica 7 e o estoque aumenta 3.

São aceitas devoluções parciais e múltiplas até o limite entregue menos já devolvido. Um produto deve pertencer à requisição e estar ativo; se foi desativado depois da entrega, reative antes de devolver. Requisições atendidas permanecem encerradas. Requisições canceladas após entrega permitem devolver o material entregue. A devolução não reabre atendimento, não recria reserva e não aumenta o pendente para entrega.

Cada operação gera entradas no histórico de estoque, vinculadas à requisição e a um identificador comum de devolução. Data de registro, motivo, responsável autenticado e saldo após entrada ficam registrados. Entregas originais são preservadas. O botão **Histórico de devoluções** permite consultar esses dados; usuários autenticados podem consultar. O histórico de entregas continua exibindo apenas saídas.

A gravação de quantidades, saldos e movimentos usa a mesma transação. Rowversion da requisição e dos produtos protege contra operações concorrentes; um conflito exige recarregar e tentar novamente. Não há exclusão ou edição de devoluções nesta fase. O relatório **Consumo por centro** e o CSV exibem entregue, devolvido e consumo líquido pela data dos movimentos. Uma devolução de entrega antiga pode produzir consumo líquido negativo no período; o número não comprova utilização física.

## Atualização após merge

```powershell
cd C:\gestao-estoque-fullstack
git checkout main
git pull origin main
cd backend
dotnet ef database update --project GestaoEstoque.Infrastructure --startup-project GestaoEstoque.API
```

Reinicie API e frontend. A migration `20261001175000_MaterialReturns` preserva os registros existentes e inicia a quantidade devolvida em zero.

## Casos para a bateria manual no SQL Server Express

1. Entregar 10 e devolver 3: estoque aumenta 3, entregue fica 10, devolvido fica 3, líquido fica 7.
2. Devolver mais 7 e tentar mais 1: a última operação falha sem alterar estoque/histórico.
3. Devolver após entrega parcial: a reserva pendente permanece igual; ainda pode entregar apenas o pendente original.
4. Cancelar após entrega parcial e devolver: situação e motivo do cancelamento permanecem.
5. Enviar lote com um item acima do limite: nenhum item deve ser gravado.
6. Consultor tentar POST de devolução: deve receber 403; consulta do histórico deve funcionar.
7. Duas devoluções simultâneas que juntas excedem o limite: conflito em uma e saldos consistentes.
8. Consultar período somente da devolução de uma entrega antiga: devolvido positivo, entregue zero e líquido negativo, inclusive no CSV.
9. Conferir histórico de entregas, Kardex e histórico de devoluções com IDs, motivo e responsável.

Os testes automatizados cobrem regras de domínio, geração do SQL de migration e tradução das consultas sem conexão. A execução real e os casos manuais no SQL Server Express ficam para a bateria final.
