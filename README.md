# Gestão de Estoque Full Stack

API ASP.NET Core 8, EF Core, SQL Server Express e interface Angular para produtos e movimentações de estoque.

## Executar no Windows

1. Instale o SDK .NET 8, SQL Server Express e Node.js compatível com Angular 21.
2. Ajuste `ConnectionStrings:DefaultConnection` em `backend/GestaoEstoque.Api/appsettings.Development.json` para sua instância. Para executar a migração, defina também `ConnectionStrings__DefaultConnection` no terminal. Configure a chave JWT e os dados do primeiro administrador **fora do repositório**:

```powershell
$env:ConnectionStrings__DefaultConnection = 'Server=DESKTOP-F05RB99\SQLEXPRESS;Database=GestaoEstoqueDb;Trusted_Connection=True;TrustServerCertificate=True'
$env:Auth__Key = '<uma-chave-aleatoria-de-pelo-menos-32-bytes>'
$env:Bootstrap__AdminEmail = 'admin@exemplo.com'
$env:Bootstrap__AdminPassword = '<senha-forte-de-12-a-128-caracteres>'
```

Substitua os valores entre `<...>` por valores seus. A senha de bootstrap só é usada quando o banco ainda não possui usuários. Depois da primeira execução, retire `Bootstrap__AdminEmail` e `Bootstrap__AdminPassword` do ambiente. Mantenha a mesma `Auth__Key` entre reinícios para preservar sessões existentes. Não inclua a chave nem senhas em `appsettings`, commits ou capturas de tela.

3. Na pasta `backend`, execute:

```powershell
dotnet restore GestaoEstoque.sln
dotnet ef database update --project GestaoEstoque.Infrastructure --startup-project GestaoEstoque.Api
dotnet run --project GestaoEstoque.Api --launch-profile http
dotnet test GestaoEstoque.Domain.Tests/GestaoEstoque.Domain.Tests.csproj
```

Use a URL do Swagger exibida no terminal, acrescentando `/swagger`.

Em outro terminal, na pasta `frontend`:

```powershell
npm ci
npm start
```

Acesse `http://localhost:4200` e entre com o administrador criado na primeira execução. O proxy do Angular encaminha `/api` para `http://localhost:5029`. Se usar outra porta, ajuste `frontend/proxy.conf.json`.

Cadastre uma categoria em **Categorias** antes de criar o primeiro produto. O cadastro começa com saldo zero. Registre uma **entrada** para formar o estoque e depois teste uma **saída**. O menu alterna entre Visão geral, Produtos e Categorias.

## Fluxo da API

| Método | Rota | Uso |
| --- | --- | --- |
| GET | `/api/produtos` | Lista produtos e saldo |
| POST | `/api/produtos` | Cria produto com `nome`, `categoriaId`, `preco`, `estoqueMinimo` |
| GET | `/api/produtos/{id}` | Consulta produto |
| PUT | `/api/produtos/{id}` | Altera dados cadastrais |
| DELETE | `/api/produtos/{id}` | Desativa sem apagar histórico |
| GET | `/api/produtos/{id}/movimentos` | Consulta entradas e saídas |
| POST | `/api/produtos/{id}/movimentos` | Registra entrada (`tipo: 1`) ou saída (`tipo: 2`) |
| GET | `/api/categorias` | Lista categorias |
| POST | `/api/categorias` | Cadastra categoria (Administrador) |
| PUT | `/api/categorias/{id}` | Renomeia categoria (Administrador) |
| PATCH | `/api/categorias/{id}/ativo` | Ativa/desativa categoria (Administrador) |

Exemplo de movimento: `{ "tipo": 1, "quantidade": 10, "observacao": "Compra inicial" }`.

O saldo só é alterado por movimentações. A API recusa saída acima do saldo e usa `rowversion` para detectar alterações simultâneas. A migração cria um movimento de abertura para produtos existentes com saldo positivo.

A migração de categorias cria registros a partir dos nomes já usados nos produtos, preserva seus vínculos e impede excluir categorias em uso por produtos ativos. Execute `dotnet ef database update` após atualizar o projeto antes de iniciar a API.

## Login e permissões

O administrador acessa a tela **Usuários** e clica em **Novo usuário** para cadastrar uma conta; a tela **Perfis** permite criar perfis personalizados. Os perfis básicos **Administrador** e **Operador** são preservados pela migração. O Operador pode cadastrar produtos e categorias e movimentar estoque, mas não editar/desativar produtos ou categorias nem gerenciar usuários e perfis. Perfis personalizados permitem definir essas ações separadamente. Somente o administrador cadastra usuários e perfis. Ao alterar permissões, seus usuários precisam entrar novamente. Não há cadastro público de usuários.

Após atualizar o código, execute `dotnet ef database update` antes de iniciar a API. A migração vincula os usuários existentes aos perfis básicos, sem apagar as contas.

As senhas são armazenadas com hash pelo `PasswordHasher` do ASP.NET Core. O token JWT dura duas horas e fica apenas na memória do navegador; atualizar a página exige novo login. Desativar um usuário ou alterar a senha revoga seus tokens anteriores. O login tem limite de tentativas por IP. Em produção, configure HTTPS, armazenamento seguro da chave e backup do banco antes de atender clientes.
# Rastreabilidade das movimentações

Cada nova entrada ou saída exige um motivo e registra data efetiva, data de registro, usuário autenticado, saldo após a operação, documento de origem e custo unitário informado (os dois últimos opcionais). O histórico do produto apresenta esses dados em ordem de registro e não oferece edição ou exclusão de movimentações. Para corrigir um lançamento, registre uma operação compensatória com motivo e referência ao documento original.

Após atualizar o código, execute `dotnet ef database update` na pasta `backend` usando a configuração local de conexão. A migração preserva os movimentos antigos e usa a observação anterior como motivo quando existir; responsável, custo, documento e saldo por lançamento anteriores permanecem desconhecidos. O custo informado não calcula custo médio, CMV ou conformidade SPED.

### Quantidades fracionadas

Quantidades e saldos aceitam até três casas decimais (ex.: 0,001 M ou 1,250 L), mantendo os registros inteiros existentes. Valores negativos, acima de 2147483647,999 ou com mais de três casas são rejeitados, sem arredondamento silencioso. A unidade cadastrada continua sendo a referência de todo o fluxo; não há conversão automática entre unidades.

Após atualizar o código, aplique as migrations na pasta `backend`:

```powershell
dotnet ef database update --project GestaoEstoque.Infrastructure --startup-project GestaoEstoque.API
```

A migration `20261002100000_FractionalQuantities` altera as colunas de quantidade para `decimal(13,3)`. A reversão é bloqueada se houver frações ou valores fora do intervalo inteiro, para impedir perda de dados.

### Transferências e saldos por local

A tela **Transferências** consulta os saldos de um produto em cada posição e permite transferir quantidades disponíveis entre locais, com motivo, responsável e histórico paginado. Operadores com permissão de movimentar estoque e administradores podem transferir; usuários autenticados podem consultar. Não há edição ou exclusão de transferências; correções exigem outra transferência.

- O saldo total e as reservas totais são preservados em transferências. Materiais reservados ficam protegidos em cada origem.
- A migration `20261002140000_StockTransfers` coloca os saldos e reservas existentes na posição cadastrada ou em **Sem localização**, sem inventar movimentações. É possível transferir de Sem localização para uma posição ativa.
- A posição cadastrada no produto passa a ser a **posição padrão**. Alterá-la define o destino de futuras entradas, sem mover o saldo existente. Entradas de compras, devoluções e ajustes positivos usam essa posição; posições padrão inativas impedem novas entradas até sua correção.
- Reservas e saídas usam o padrão primeiro, depois os demais locais em ordem de posição. Entregas liberam somente a quantidade reservada entregue; cancelamentos liberam o restante. Saídas não consomem as reservas restantes.
- Inventário continua sendo uma contagem **total por produto**, abrangendo todos os locais. Um ajuste negativo consome apenas saldo disponível; inventário individual por posição fica para uma evolução futura.
- Os saldos locais, totais e históricos são gravados na mesma transação, com controle de concorrência. A transferência exige a versão dos saldos consultados; uma alteração concorrente exige recarregar.
- Destino precisa estar ativo. Origem inativa pode ser esvaziada; posições/almoxarifados com saldo ou reservas não podem ser desativados. O histórico guarda os nomes dos locais no momento da transferência.
- Reversão da migration é bloqueada se houver transferências, para preservar o histórico e a distribuição por local.

Após merge, execute na pasta `backend`: `dotnet ef database update --project .\GestaoEstoque.Infrastructure --startup-project .\GestaoEstoque.Api`, e reinicie a API e o frontend. Testes manuais com SQL Server Express continuam pendentes.
