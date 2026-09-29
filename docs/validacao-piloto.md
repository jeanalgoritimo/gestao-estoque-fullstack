# Validação do fluxo de estoque

Use uma cópia de testes do banco e uma conta com permissões **Cadastrar categorias**, **Cadastrar produtos** e **Movimentar estoque**. O roteiro automatizado cria registros `VALIDACAO-*` e `Parafuso validacao *`; eles permanecem no banco para inspeção posterior. Não o execute no banco de um cliente.

Com a API na porta 5029, no PowerShell, na raiz do repositório:

```powershell
$env:ESTOQUE_TEST_EMAIL = '<e-mail da conta de teste>'
$env:ESTOQUE_TEST_PASSWORD = '<senha da conta de teste>'
./scripts/validar-fluxo-estoque.ps1
```

A senha fica só no ambiente do terminal. O script aceita apenas uma API local, não imprime token ou senha e para na primeira divergência. Ao terminar, remova as variáveis da sessão:

```powershell
Remove-Item Env:ESTOQUE_TEST_EMAIL, Env:ESTOQUE_TEST_PASSWORD
```

O resultado esperado é `APROVADO` com saldo final **6**. O script confere: produto iniciado em zero; entrada de 10; saída de 3; inventário de 7 para 5 com ajuste de 2; histórico com documento `INV-{id}` e responsável; conflito HTTP 409 quando há movimentação durante uma contagem; cancelamento; e confirmação sem diferença, sem novo movimento.

## Conferência manual com um pequeno negócio

1. Cadastre cinco produtos e categorias reais em um banco de teste. Registre entradas, saídas e documentos de origem, sem dados sensíveis de clientes.
2. Peça ao responsável que conte fisicamente dois produtos sem consultar o saldo. Registre a contagem, apresente a diferença e só então confirme com motivo.
3. Abra o histórico de cada produto e confira quantidade, saldo após a operação, responsável e referência do inventário.
4. Registre em uma lista as dificuldades de uso, informações faltantes e relatórios solicitados. Não trate o indicador **Potencial de venda** como custo ou valor contábil.

A execução na máquina local e a observação do usuário real ainda precisam ser feitas; a aprovação no CI não substitui esses passos.
