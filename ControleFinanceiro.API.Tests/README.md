# Validação da Fase 1

Requisitos locais: SDK .NET 8, SQL Server Express em `localhost\SQLEXPRESS`, autenticação do Windows com permissão para criar bancos de teste e Microsoft Edge instalado.

Na raiz do workspace:

```powershell
dotnet restore ControleFinanceiro.API.Tests/ControleFinanceiro.API.Tests.csproj
dotnet build ControleFinanceiro.API.Tests/ControleFinanceiro.API.Tests.csproj --no-restore
dotnet test ControleFinanceiro.API.Tests/ControleFinanceiro.API.Tests.csproj --no-build
```

Cada fixture cria um banco `ControleFinanceiro_Fase1_Tests_<guid>`. A conexão não é obtida do banco pessoal. A fábrica da API substitui o DbContext antes da inicialização. A limpeza confere o nome exato gerado e o prefixo antes de excluir exclusivamente o banco de teste.

O teste de migration aplica as duas migrations antigas, insere histórico sintético (incluindo salários e parcelas), captura todos os campos antigos, aplica `AdicionarContas` e compara o histórico. Nenhuma conta é criada automaticamente.

Os testes HTTP cobrem contas, validação de lançamentos, associação de histórico, contas inativas, preservação de vínculos, restrição de exclusão, crédito legado, contratos dos resumos e inicialização. Os testes de configuração executam a aplicação com argumento explícito vazio/inválido, impedindo conexão com qualquer banco.

O teste de interface usa Playwright com o Edge instalado em modo headless. Requisições são encaminhadas à API de teste; não é iniciado um servidor pessoal e requisições externas, inclusive fontes, são bloqueadas. O teste cobre cadastro, edição, inativação, reativação, associação de histórico a conta inativa e criação de lançamentos comuns e de crédito.

Resultado da validação da Fase 1: build com zero avisos/erros; 23 testes aprovados, nenhum ignorado. Os bancos temporários são removidos ao final.

## Decisões transitórias implementadas

- `PUT /api/contas/{id}` edita nome/tipo/status; não existe DELETE de contas.
- `POST /api/transacoes` exige conta ativa fora do crédito e rejeita conta informada no crédito legado.
- `PUT /api/transacoes/{id}` edita campos financeiros preservando a conta. A conta é alterada exclusivamente por `PATCH /api/transacoes/{id}/conta`.
- A associação permite conta inativa, mas rejeita crédito legado. Converter lançamento já associado para crédito também é rejeitado para não apagar vínculos silenciosamente.
- Todos os lançamentos desta fase retornam `situacaoFinanceira: "Não reconciliado"`, independentemente da conta. Não foi introduzido estado persistido de realização.
- Nomes antigos dos campos numéricos do dashboard/balanço foram preservados por compatibilidade; as respostas incluem aviso e `saldoBancarioConfirmado: false`. A interface usa rótulos de lançamentos cadastrados.
- O parcelamento antigo foi mantido, envolvendo suas gravações em uma transação de banco para evitar parcelas parcialmente salvas.
- Não há saldo por conta, abertura, previsões, recorrências ou faturas nesta entrega.

## Migration e banco pessoal

A migration foi gerada e validada exclusivamente nos bancos isolados. As migrations antigas não foram editadas.

`AppDbContextFactory` permite gerar migrations sem executar o `Program.cs`, usando uma conexão de design separada que não é aberta na geração. Para aplicar migrations por CLI a um banco específico, é necessário informar a conexão de destino explicitamente com `--connection`.

O comportamento existente de `Database.Migrate()` na inicialização normal foi preservado. Portanto, a próxima execução normal da aplicação aplicará as migrations pendentes ao banco configurado em `appsettings.json`. Faça backup restaurável antes dessa execução. Não execute reversão (`Down`) sobre dados pessoais como substituto de backup: ela removeria contas e associações.
