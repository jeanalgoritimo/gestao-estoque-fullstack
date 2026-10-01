# Reposição pelo saldo disponível

No menu **Reposição**, a API consulta todos os produtos ativos com **disponível menor que o mínimo**. Disponível é saldo físico menos reservas de requisições. Um produto com físico 15, reservado 12 e mínimo 10 precisa de 7 unidades para voltar ao mínimo, embora tenha mais de 10 fisicamente.

A sugestão é `máximo(0, mínimo − disponível − compras pendentes)`. Compras pendentes são quantidades ainda não recebidas de pedidos abertos ou parcialmente recebidos, de qualquer fornecedor. Recebidos, cancelados e saldos cancelados não contam. Se há 4 unidades pendentes no exemplo, a sugestão será 3. Produtos cuja necessidade já está coberta continuam visíveis com sugestão zero. Saldo igual ao mínimo não entra nesta lista; o indicador antigo de estoque baixo continua com seu critério menor ou igual sobre o físico.

Rascunhos existentes aparecem em **Em rascunhos**, sem abater a sugestão: ainda não são compras confirmadas. Revise-os em Pedidos de compra antes de gerar outro para evitar duplicar o planejamento. As sugestões são uma fotografia do momento da consulta; atualize antes de planejar. Atualizar descarta ajustes e recalcula.

## Gerar e revisar

1. Administrador seleciona um fornecedor ativo e revisa os produtos exibidos. Produtos sem fornecedor ativo aparecem para saneamento do cadastro.
2. Ajusta quantidades; zero exclui o item do novo rascunho. O pedido aceita de 1 a 100 produtos distintos do mesmo fornecedor, com quantidades inteiras até 2147483647.
3. **Gerar rascunho de compra** salva o pedido com situação Rascunho e navega para Pedidos de compra.
4. **Revisar rascunho** permite ajustar quantidades e remover itens, mantendo pelo menos um. O fornecedor permanece o mesmo. Para mudar fornecedor ou incluir outro produto pela tela, cancele e gere novo planejamento.
5. Após salvar, **Confirmar abertura** muda o pedido para Aberto. Só então recebimentos são permitidos. A abertura não envia mensagem ao fornecedor e não altera estoque ou reservas.

Rascunhos podem ser cancelados com motivo. Somente administrador cria, edita, confirma e cancela. Usuários autenticados consultam sugestões; o recebimento mantém a permissão de movimentar estoque. Produtos/fornecedor precisam estar ativos e vinculados na criação, edição e confirmação. Edição e confirmação usam rowversion enviada pela tela para rejeitar revisões antigas e operações simultâneas; recarregue em caso de conflito.

O CSV inclui físico, reservado, disponível, mínimo, compras pendentes, rascunhos, sugestão e quantidade ajustada. A exportação usa os filtros aplicados na tela e não grava pedidos.

## Atualização

Depois do merge, execute `git pull origin main` e reinicie API e frontend. Não há nova migration: Rascunho é o valor 6 no campo de situação já existente. Pedidos antigos conservam seus estados e o endpoint de criação sem `rascunho: true` mantém o comportamento Aberto para compatibilidade.

## Bateria manual final

- Reservar 12 de físico 15 com mínimo 10: disponível 3 e sugestão 7.
- Ter compra de 4 ainda não recebidas: sugestão 3. Receber 2: disponível aumenta 2 e pendente diminui 2, sem dupla contagem.
- Cancelar saldo de compra: saldo cancelado deixa de abater sugestão.
- Criar rascunho: estoque/reservas inalterados; sugestão permanece e coluna de rascunhos aumenta.
- Editar/remover itens, confirmar abertura: rascunho deixa de contar nessa coluna e passa a compras pendentes.
- Tentar receber rascunho: bloqueado sem entrada de estoque.
- Editar/confirmar versão antiga em outra aba: 409, sem sobrescrever alterações.
- Desativar fornecedor/produto ou mudar vínculo antes de confirmar: bloqueado.
- Consultor/operador tentar criar, editar ou confirmar: 403.
- Igual ao mínimo, mínimo zero ou produto inativo: não listado. Necessidade já coberta: sugestão zero.
- Conferir filtros, CSV e pedido gerado com apenas os itens de quantidade positiva.

Testes automatizados cobrem cálculo, limites, transições do rascunho e tradução SQL das consultas sem conexão. A validação real no SQL Server Express fica para a bateria final.
