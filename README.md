# Gestão de Estoque Full Stack

Primeira entrega: API ASP.NET Core 8, EF Core e SQL Server Express para produtos e movimentações de estoque. A interface Angular ainda será construída.

## Executar no Windows

1. Instale o SDK .NET 8 e SQL Server Express.
2. Ajuste `ConnectionStrings:DefaultConnection` em `backend/GestaoEstoque.Api/appsettings.Development.json` para sua instância, ou configure via variável de ambiente `ConnectionStrings__DefaultConnection`.
3. Na pasta `backend`, execute:

```powershell
dotnet restore GestaoEstoque.sln
dotnet ef database update --project GestaoEstoque.Infrastructure --startup-project GestaoEstoque.Api
dotnet run --project GestaoEstoque.Api
dotnet test GestaoEstoque.Domain.Tests/GestaoEstoque.Domain.Tests.csproj
```

Use a URL do Swagger exibida no terminal, acrescentando `/swagger`.

## Fluxo da API

| Método | Rota | Uso |
| --- | --- | --- |
| GET | `/api/produtos` | Lista produtos e saldo |
| POST | `/api/produtos` | Cria produto com `nome`, `categoria`, `preco`, `estoqueMinimo` |
| GET | `/api/produtos/{id}` | Consulta produto |
| PUT | `/api/produtos/{id}` | Altera dados cadastrais |
| DELETE | `/api/produtos/{id}` | Desativa sem apagar histórico |
| GET | `/api/produtos/{id}/movimentos` | Consulta entradas e saídas |
| POST | `/api/produtos/{id}/movimentos` | Registra entrada (`tipo: 1`) ou saída (`tipo: 2`) |

Exemplo de movimento: `{ "tipo": 1, "quantidade": 10, "observacao": "Compra inicial" }`.

O saldo só é alterado por movimentações. A API recusa saída acima do saldo e usa `rowversion` para detectar alterações simultâneas. A migração cria um movimento de abertura para produtos existentes com saldo positivo. Ainda não há login: **não publique esta API na Internet antes de implementar autenticação e autorização**.
