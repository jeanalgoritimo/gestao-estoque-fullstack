# Localização dos materiais

No menu **Localizações**, o administrador cadastra almoxarifados e posições com corredor, estante e prateleira. Nomes de almoxarifado são únicos. A combinação de corredor/estante/prateleira é única dentro de cada almoxarifado, desconsiderando caixa e espaços nas pontas. Diferentes almoxarifados podem usar a mesma combinação. Campos de posição aceitam até 40 caracteres cada; nome de almoxarifado aceita até 100.

Ao cadastrar ou editar um produto, selecione uma posição ativa de um almoxarifado ativo ou **Sem localização**. Usuários com permissão de cadastrar produtos podem escolher uma posição existente; alterar vínculo de produto existente mantém a permissão de gerenciar produtos. Somente o administrador gerencia o cadastro de localizações. Usuários autenticados consultam.

Cada produto tem uma localização opcional. A migration preserva produtos existentes sem inventar endereços e sem alterar saldo, reservas ou histórico. Não há saldo por almoxarifado, múltiplas posições por produto ou transferência nesta fase. Alterar o endereço cadastral não movimenta quantidades.

## Desativação e consulta

Almoxarifados e posições podem ser editados, desativados e reativados; não há exclusão física. Desativar o almoxarifado torna suas posições indisponíveis para novos vínculos, mesmo quando a posição permanece ativa. Reativar o almoxarifado restaura a disponibilidade das posições que continuam ativas. Para reativar uma posição desativada, reative seu almoxarifado primeiro.

Vínculos atuais são preservados e podem ser mantidos ao editar outros dados do produto, inclusive quando a posição ficou inativa. Não é permitido escolher outra posição inativa. É possível remover o vínculo selecionando Sem localização. O almoxarifado de uma posição existente não muda; para corrigir esse vínculo, cadastre posição no almoxarifado correto e reatribua os produtos.

Produtos mostram **Localização atual**, permitem pesquisar pelo endereço e filtrar por posição, inclusive inativa, ou Sem localização. No inventário, o filtro limita os produtos oferecidos para nova contagem e as contagens carregadas na tela (o endpoint existente retorna as últimas 100). A localização exibida é a atual do produto, não uma fotografia do endereço quando a contagem foi aberta. Renomear almoxarifado/posição atualiza o texto nas consultas. Alterar o vínculo do produto durante uma contagem muda seu rowversion e exige reabrir a contagem antes de confirmar, conforme a proteção existente.

## Atualização após merge

```powershell
cd C:\gestao-estoque-fullstack
git checkout main
git pull origin main
cd backend
dotnet ef database update --project GestaoEstoque.Infrastructure --startup-project GestaoEstoque.API
```

Reinicie API e frontend. Cadastre um almoxarifado e uma posição no novo menu; depois edite o produto para vinculá-lo. Migration: `20261001190000_StockLocations`.

## Bateria manual no SQL Server Express

1. Criar Central e posição corredor A, estante 01, prateleira 02. Duplicar com caixa/espaços diferentes: rejeitar. Mesma posição em outro almoxarifado: aceitar.
2. Produto antigo continua Sem localização e com saldos/reservas intactos após migration.
3. Vincular produto, mudar a posição e remover o vínculo: conferir retorno da API, tabela e filtros.
4. Operador sem permissão de gerenciar produto pode escolher posição na criação autorizada, mas não alterar produto existente nem gerenciar locais. Consultor tenta cadastro: 403.
5. Desativar posição/almoxarifado: vínculo atual continua visível e pode ser mantido; novo vínculo deve falhar.
6. Reativar almoxarifado/posição e conferir opções oferecidas.
7. Renomear e editar corredor/estante/prateleira: consultas mostram endereço atualizado sem alterar estoque.
8. Filtrar produtos e inventário por posição e Sem localização; verificar opções de nova contagem.
9. Mudar posição do produto durante contagem aberta: confirmação deve exigir nova contagem.

Testes automatizados verificam domínio, unicidade/configuração de relacionamentos e SQL da migration sem conectar. A aplicação real e a bateria manual ficam para a validação final.
