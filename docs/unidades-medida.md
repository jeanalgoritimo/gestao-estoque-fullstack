# Unidades de medida — identificação

O administrador cadastra unidades no menu **Unidades de medida**, informando sigla e nome. Exemplos: UN (unidade), CX (caixa), PCT (pacote), M (metro), L (litro) e KG (quilograma). Sigla é única, normalizada para maiúsculas, com até 10 letras/números iniciando por letra. Nome aceita até 80 caracteres. Usuários autenticados consultam; só administrador cadastra, edita, desativa e reativa.

Cada produto tem uma unidade obrigatória. O cadastro oferece unidades ativas; UN é a opção inicial. A migration cria UN como padrão protegido e vincula os produtos existentes a ela, mantendo todas as quantidades, reservas, custos e saldos anteriores. Clientes de API que omitirem o campo na criação recebem UN; na edição a omissão preserva a unidade atual. Esse vínculo serve para preservar o comportamento legado; não inventa conversões de produtos antigos.

## Regras

- As quantidades continuam **inteiras** em todos os fluxos. 3 M ou 2 L são aceitos; 0,5 M ou 1,25 L ficam para uma evolução futura. Os formulários e a API continuam rejeitando frações.
- A unidade se aplica ao estoque, mínimo, reservas, compras, requisições, recebimentos, devoluções, inventário e movimentações do produto.
- Preço cadastrado e custo informado são por uma quantidade dessa unidade. Um produto em CX representa caixas: 3 CX não equivale automaticamente a 3 UN. Não há fator de conversão ou unidade diferente para compra/venda.
- Unidade de produto pode ser alterada somente enquanto não houver saldo, reservas ou registros de movimentos, itens de compra/requisição ou inventário. Registros cancelados e rascunhos também bloqueiam, pois já utilizaram a identificação do produto.
- Manter a mesma unidade é permitido, inclusive inativa. Desativação retira a opção de novos vínculos, sem bloquear operações de produtos já vinculados.
- Sigla de unidade vinculada a qualquer produto não muda; cadastre outra unidade. Nome pode ser corrigido. UN não pode ser desativada ou trocar sigla. Não há exclusão física.
- Troca de unidade e consulta dos registros existentes ocorrem em transação serializável; o rowversion do produto continua protegendo concorrência. Não existe migração/conversão de histórico pela tela.

Compras, requisições, inventário, movimentos, reposição e consumo mostram a sigla. CSV de posição de estoque, reposição e consumo inclui a unidade. Painel e resumo de posição atual agrupam quantidades por sigla; não somam caixa, litro e unidade em um único número. Potencial de venda continua somando valores monetários.

## Atualização após merge

```powershell
cd C:\gestao-estoque-fullstack
git checkout main
git pull origin main
cd backend
dotnet ef database update --project GestaoEstoque.Infrastructure --startup-project GestaoEstoque.API
```

Reinicie API e frontend. Migration `20261001194000_MeasurementUnits`. Cadastre CX/PCT/M/L conforme a operação; selecione a unidade ao criar novos produtos antes de movimentá-los.

## Bateria manual no SQL Server Express

1. Produtos antigos ficam em UN, mantendo números e histórico.
2. Criar CX e repetir cx com espaços: conflito. Sigla inválida ou nome vazio: rejeitar. Operador/consultor cadastrar: 403.
3. Criar produto em CX, receber 3 CX e conferir saldo, reserva, requisição, entrega, devolução, inventário, relatórios e CSV com CX.
4. Conferir preço/custo por CX; nenhum fator de conversão é aplicado.
5. Produto novo sem operações pode mudar unidade. Após entrada/saída até saldo zero, troca deve falhar sem alterar saldo ou unidade.
6. Requisição/compra em rascunho ou inventário sem diferença também bloqueiam troca do produto.
7. Troca simultânea a operação nova: verificar consistência e conflito/recarga em caso de concorrência.
8. Unidade inativa some dos novos vínculos; produto já vinculado pode manter a unidade e operar.
9. Tentar mudar sigla em uso e desativar UN: bloqueado. Correção do nome: permitida.
10. Saldo de 10 UN e 3 CX aparece separado no painel e relatório; CSV preserva coluna Unidade.
11. Tentar quantidades fracionadas: rejeitadas sem gravação parcial.

Testes automatizados cobrem domínio, SQL de migration, modelo e tradução dos relatórios sem conexão. A aplicação real e os casos manuais ficam para a bateria final.
