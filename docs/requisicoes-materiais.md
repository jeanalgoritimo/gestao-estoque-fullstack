# Requisições de materiais e reservas

Acesse **Requisições → Nova requisição**, informe a finalidade, adicione produtos e salve o rascunho. O solicitante ou administrador envia para aprovação. O administrador aprova; somente nesse momento as quantidades são reservadas. A aprovação inteira falha se qualquer produto não tiver saldo disponível.

## Permissões

| Ação | Acesso |
|---|---|
| Consultar e criar requisição | Usuário autenticado |
| Enviar rascunho | Solicitante ou administrador |
| Aprovar e reservar | Administrador |
| Registrar entrega | Perfil com permissão de movimentar estoque |
| Cancelar rascunho ou pendente | Solicitante ou administrador |
| Cancelar aprovada ou parcialmente atendida | Administrador |

## Saldos e rastreabilidade

- Saldo disponível = estoque físico − estoque reservado.
- A aprovação não cria saída nem altera estoque físico.
- Entregas parciais reduzem estoque físico e reserva na mesma quantidade, registrando uma movimentação por produto, usuário responsável e identificador comum de entrega.
- Cancelamento exige motivo e libera somente o saldo pendente. Entregas realizadas permanecem no histórico. Requisições atendidas não podem ser canceladas.
- Saídas avulsas e ajustes negativos de inventário respeitam o saldo disponível. Produtos com reserva não podem ser desativados.
- Reservas e entregas usam rowversion de produto e requisição. Conflitos devolvem HTTP 409; recarregue antes de repetir. Cada operação é gravada em uma transação.
- Nesta versão, itens são montados antes de salvar; um rascunho salvo não pode ser editado. Para corrigir, cancele com motivo e crie outra requisição.
- A lista mostra as últimas 100 requisições, com pesquisa e filtro de situação.

## Atualização local após integrar o PR

```powershell
cd C:\gestao-estoque-fullstack
git checkout main
git pull origin main
cd backend
dotnet ef database update --project GestaoEstoque.Infrastructure --startup-project GestaoEstoque.API
```

Reinicie a API e o frontend após atualizar. A migration adiciona tabelas de requisição e itens, a reserva nos produtos (inicia em zero) e vínculos opcionais nas movimentações existentes.

## Roteiro para a futura bateria de testes

1. Com saldo físico 20, solicitar 8, enviar e aprovar: físico 20, reservado 8, disponível 12.
2. Tentar saída avulsa de 13: deve falhar sem movimento novo.
3. Entregar 3: físico 17, reservado 5, disponível 12; situação parcialmente atendida e movimento no histórico.
4. Cancelar o restante com motivo: físico 17, reservado 0, disponível 17; entrega anterior preservada.
5. Criar outra requisição, entregar tudo e verificar situação atendida e bloqueio de nova entrega.
6. Aprovar duas requisições simultâneas que excederiam o saldo: uma deve falhar sem reserva parcial.
7. Tentar aprovar como operador: HTTP 403. Operador pode entregar se tiver permissão de movimentação.
8. Ajuste de inventário abaixo da reserva ou desativação de produto reservado deve falhar.

A execução desse roteiro no SQL Server Express do usuário não foi realizada pelo ambiente de desenvolvimento remoto.
