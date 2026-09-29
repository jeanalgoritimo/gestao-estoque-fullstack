param(
    [string]$BaseUrl = 'http://localhost:5029',
    [string]$Email = $env:ESTOQUE_TEST_EMAIL,
    [string]$Senha = $env:ESTOQUE_TEST_PASSWORD
)

$ErrorActionPreference = 'Stop'
$uri = [uri]$BaseUrl
if ($uri.Scheme -notin @('http', 'https') -or $uri.Host -notin @('localhost', '127.0.0.1', '::1')) {
    throw 'Este roteiro só aceita uma API local. Use localhost ou 127.0.0.1.'
}
if ([string]::IsNullOrWhiteSpace($Email) -or [string]::IsNullOrWhiteSpace($Senha)) {
    throw 'Defina ESTOQUE_TEST_EMAIL e ESTOQUE_TEST_PASSWORD para uma conta com permissão de cadastrar e movimentar.'
}
$BaseUrl = $BaseUrl.TrimEnd('/')
$script:token = $null
$abertos = [System.Collections.Generic.List[long]]::new()

function Invoke-Api {
    param([string]$Method, [string]$Path, $Body = $null)
    $params = @{ Uri = "$BaseUrl$Path"; Method = $Method; ErrorAction = 'Stop' }
    if ($script:token) { $params.Headers = @{ Authorization = "Bearer $script:token" } }
    if ($null -ne $Body) {
        $params.ContentType = 'application/json'
        $params.Body = ConvertTo-Json -InputObject $Body -Depth 5 -Compress
    }
    Invoke-RestMethod @params
}

function Assert-Equal {
    param($Expected, $Actual, [string]$Description)
    if ($Expected -ne $Actual) {
        throw "$Description`: esperado $Expected; encontrado $Actual."
    }
}

function Assert-Conflict {
    param([scriptblock]$Action, [string]$Description)
    try { & $Action | Out-Null }
    catch {
        $response = $_.Exception.Response
        if ($null -ne $response -and [int]$response.StatusCode -eq 409) { return }
        throw "$Description`: falha diferente de HTTP 409. $($_.Exception.Message)"
    }
    throw "$Description`: a API aceitou a operação que deveria retornar HTTP 409."
}

try {
    $login = Invoke-Api 'POST' '/api/auth/login' @{ email = $Email; senha = $Senha }
    $script:token = $login.token
    if (-not $script:token) { throw 'Login sem token.' }

    $sufixo = [guid]::NewGuid().ToString('N').Substring(0, 8)
    $categoria = Invoke-Api 'POST' '/api/categorias' @{ nome = "VALIDACAO-$sufixo" }
    $produto = Invoke-Api 'POST' '/api/produtos' @{
        nome = "Parafuso validacao $sufixo"; categoriaId = $categoria.id; preco = 8.50; estoqueMinimo = 2
    }
    $produtoId = [int]$produto.id
    Assert-Equal 0 $produto.estoque 'Produto começa com saldo zero'

    Invoke-Api 'POST' "/api/produtos/$produtoId/movimentos" @{
        tipo = 1; quantidade = 10; motivo = 'Compra de teste'; documentoOrigem = "PED-$sufixo"
    } | Out-Null
    Invoke-Api 'POST' "/api/produtos/$produtoId/movimentos" @{
        tipo = 2; quantidade = 3; motivo = 'Venda de teste'; documentoOrigem = "VEN-$sufixo"
    } | Out-Null
    $produto = Invoke-Api 'GET' "/api/produtos/$produtoId"
    Assert-Equal 7 $produto.estoque 'Saldo após entrada e saída'

    $inventario = Invoke-Api 'POST' '/api/inventarios' @{ produtoId = $produtoId }
    $abertos.Add([long]$inventario.id)
    Assert-Equal 7 $inventario.saldoInicial 'Saldo congelado na abertura'
    $contagem = Invoke-Api 'PUT' "/api/inventarios/$($inventario.id)/contagem" @{ quantidade = 5 }
    Assert-Equal -2 $contagem.diferenca 'Diferença da contagem'
    $confirmado = Invoke-Api 'POST' "/api/inventarios/$($inventario.id)/confirmar" @{
        motivo = 'Divergência verificada no teste'; quantidadeEsperada = 5
    }
    $abertos.Remove([long]$inventario.id) | Out-Null
    Assert-Equal 2 $confirmado.situacao 'Inventário confirmado'
    Assert-Equal 5 (Invoke-Api 'GET' "/api/produtos/$produtoId").estoque 'Saldo após ajuste'

    $movimentos = @(Invoke-Api 'GET' "/api/produtos/$produtoId/movimentos")
    $ajuste = @($movimentos | Where-Object { $_.documentoOrigem -eq "INV-$($inventario.id)" })
    Assert-Equal 1 $ajuste.Count 'Um ajuste vinculado ao inventário'
    Assert-Equal 2 $ajuste[0].tipo 'Ajuste de saída'
    Assert-Equal 2 $ajuste[0].quantidade 'Quantidade ajustada'
    Assert-Equal 5 $ajuste[0].saldoApos 'Saldo registrado no histórico'
    if ([string]::IsNullOrWhiteSpace($ajuste[0].usuarioNome)) { throw 'Ajuste sem usuário responsável.' }

    $desatualizado = Invoke-Api 'POST' '/api/inventarios' @{ produtoId = $produtoId }
    $abertos.Add([long]$desatualizado.id)
    Invoke-Api 'PUT' "/api/inventarios/$($desatualizado.id)/contagem" @{ quantidade = 5 } | Out-Null
    Invoke-Api 'POST' "/api/produtos/$produtoId/movimentos" @{
        tipo = 1; quantidade = 1; motivo = 'Entrada durante contagem'; documentoOrigem = "MOV-$sufixo"
    } | Out-Null
    Assert-Conflict {
        Invoke-Api 'POST' "/api/inventarios/$($desatualizado.id)/confirmar" @{
            motivo = 'Contagem desatualizada'; quantidadeEsperada = 5
        }
    } 'Proteção contra saldo alterado durante a contagem'
    Invoke-Api 'POST' "/api/inventarios/$($desatualizado.id)/cancelar" @{} | Out-Null
    $abertos.Remove([long]$desatualizado.id) | Out-Null
    $produto = Invoke-Api 'GET' "/api/produtos/$produtoId"
    Assert-Equal 6 $produto.estoque 'Saldo preservado após conflito'

    $semDiferenca = Invoke-Api 'POST' '/api/inventarios' @{ produtoId = $produtoId }
    $abertos.Add([long]$semDiferenca.id)
    Invoke-Api 'PUT' "/api/inventarios/$($semDiferenca.id)/contagem" @{ quantidade = 6 } | Out-Null
    $movimentosAntes = @(Invoke-Api 'GET' "/api/produtos/$produtoId/movimentos")
    Invoke-Api 'POST' "/api/inventarios/$($semDiferenca.id)/confirmar" @{
        motivo = 'Contagem sem diferença'; quantidadeEsperada = 6
    } | Out-Null
    $abertos.Remove([long]$semDiferenca.id) | Out-Null
    $movimentosDepois = @(Invoke-Api 'GET' "/api/produtos/$produtoId/movimentos")
    Assert-Equal $movimentosAntes.Count $movimentosDepois.Count 'Sem movimento para diferença zero'

    [pscustomobject]@{
        Resultado = 'APROVADO'; CategoriaId = $categoria.id; ProdutoId = $produtoId
        InventarioAjustadoId = $inventario.id; SaldoFinal = 6
        Cenario = 'Cadastro, entrada, saída, ajuste, conflito e contagem sem diferença'
    }
}
finally {
    foreach ($id in $abertos) {
        try { Invoke-Api 'POST' "/api/inventarios/$id/cancelar" @{} | Out-Null }
        catch { Write-Warning "Contagem $id ficou aberta; cancele-a pela tela de inventário." }
    }
}
