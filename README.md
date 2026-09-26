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

O administrador cadastra outros usuários pela opção **Usuários** e cria perfis em **Perfis**. Os perfis básicos **Administrador** e **Operador** são preservados pela migração. Perfis personalizados permitem escolher separadamente as permissões de gerenciar produtos, gerenciar categorias e movimentar estoque. Somente o administrador pode cadastrar usuários e perfis. Ao alterar as permissões de um perfil, seus usuários precisam entrar novamente. Ambos podem alterar a própria senha. Não há cadastro público de usuários.

Após atualizar o código, execute `dotnet ef database update` antes de iniciar a API. A migração vincula os usuários existentes aos perfis básicos, sem apagar as contas.

As senhas são armazenadas com hash pelo `PasswordHasher` do ASP.NET Core. O token JWT dura duas horas e fica apenas na memória do navegador; atualizar a página exige novo login. Desativar um usuário ou alterar a senha revoga seus tokens anteriores. O login tem limite de tentativas por IP. Em produção, configure HTTPS, armazenamento seguro da chave e backup do banco antes de atender clientes.
