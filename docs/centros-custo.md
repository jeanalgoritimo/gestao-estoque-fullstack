# Centros de custo e consumo

O administrador cadastra, renomeia, desativa e reativa centros no menu **Centros de custo**. Exemplos: Manutenção, Produção e Administrativo. Usuários autenticados podem consultá-los. Nomes são únicos, desconsiderando diferença de maiúsculas/minúsculas e espaços nas pontas.

Novas requisições precisam selecionar um centro ativo. O solicitante ou administrador pode trocar o centro enquanto a requisição estiver em rascunho. Depois do envio, o vínculo não muda. A desativação não exclui requisições nem impede entregas de requisições já vinculadas; o centro deixa de ser oferecido em novas requisições. Para salvar alterações em um rascunho cujo centro ficou inativo, selecione outro ativo.

Requisições anteriores à migration recebem vínculo nulo e aparecem como **Sem centro de custo**. Elas podem continuar seu fluxo. Se um rascunho antigo for editado, será necessário selecionar um centro ativo. A migration não inventa uma área para registros antigos e não altera saldos. Não há exclusão física de centros; renomear atualiza o nome mostrado nas consultas e no histórico, mantendo o mesmo ID.

## Relatório

**Consumo por centro** consulta todos os registros no período informado, até 366 dias; consulta todo o período, independentemente da paginação da lista de requisições. Permite filtrar por centro ativo/inativo ou Sem centro de custo, e exportar CSV. Datas consideram dias completos no fuso America/Sao_Paulo, com fim inclusivo.

- **Solicitado**: quantidades dos itens de requisições criadas no período, incluindo rascunhos e canceladas. Rascunhos editados exibem suas quantidades atuais.
- **Entregue**: quantidades das saídas vinculadas a requisições e registradas no período. Inclui entregas de requisições criadas antes do período e de requisições que depois tiveram o restante cancelado.
- As colunas têm critérios de data distintos; sua diferença não é saldo pendente. Consumo efetivo corresponde a Entregue.
- Saídas avulsas não vinculadas a requisições não entram neste relatório.

## Atualização após integrar o PR

```powershell
cd C:\gestao-estoque-fullstack
git checkout main
git pull origin main
cd backend
dotnet ef database update --project GestaoEstoque.Infrastructure --startup-project GestaoEstoque.API
```

Reinicie API e frontend. Primeiro cadastre um centro como administrador; depois crie uma requisição vinculada a ele.

## Casos para a bateria manual

1. Criar Manutenção e tentar repetir o nome com caixa diferente: deve rejeitar duplicidade.
2. Operador tentar cadastrar/editar/desativar centro: HTTP 403.
3. Criar requisição sem centro ou com centro inativo: deve falhar; editar rascunho com centro ativo deve funcionar.
4. Enviar requisição e tentar trocar centro: deve falhar sem afetar reserva ou entregas.
5. Desativar centro com requisição aprovada: histórico e entrega permanecem; centro não pode ser selecionado para nova requisição.
6. Entregar parcialmente, cancelar o restante e consultar período da entrega: quantidade entregue permanece.
7. Entregar hoje uma requisição criada no mês anterior: relatório de hoje deve mostrar Entregue, sem contar sua quantidade em Solicitado de hoje.
8. Verificar registros antigos em Sem centro de custo e o CSV com filtros aplicados.

Os testes automatizados verificam regras de domínio, SQL da migration e tradução das consultas de agregação para SQL sem conexão. A aplicação da migration e os casos manuais no SQL Server Express local permanecem para validação pelo usuário.
