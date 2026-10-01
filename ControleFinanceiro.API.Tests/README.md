# Validação das Fases 1 e 2

Resultado final da Fase 2 (30/09/2026): **49 testes aprovados**, nenhum ignorado ou com falha; build Release com **zero avisos e zero erros**. Os 23 casos da Fase 1 foram preservados, com 26 casos novos, incluindo o segundo teste de interface. As seções originais da Fase 1 abaixo documentam aquela entrega; os contratos atuais estão na seção Fase 2.

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

## Fase 2: validação reproduzível

```powershell
dotnet build ControleFinanceiro.API.Tests/ControleFinanceiro.API.Tests.csproj --configuration Release --no-restore
dotnet test ControleFinanceiro.API.Tests/ControleFinanceiro.API.Tests.csproj --configuration Release --no-build
dotnet ef migrations has-pending-model-changes --project ControleFinanceiro.API --configuration Release --no-build
```

Release foi utilizado na validação final porque outro processo mantinha arquivos Debug bloqueados. Esse processo não foi encerrado. Não foi iniciado servidor apontando para o banco pessoal.

A fixture agora aplica a sequência antiga, insere dados sintéticos, aplica `AdicionarContas`, captura contas/vínculos e aplica `AdicionarAberturaEConfirmacao`. O teste específico também insere uma conta inativa da Fase 1 com histórico associado, comparando os dados antes/depois. Verifica abertura, efetivação, confirmação, origem e classificação sem preenchimento, estado não reconciliado, ausência de revisões fabricadas e modelo sem alterações pendentes. Todos os bancos continuam com nomes aleatórios e limpeza restrita ao próprio teste.

Cobertura nova: abertura positiva/zero/negativa, datas inválidas/futuras, campos obrigatórios, precisão, período não acompanhado, correção de abertura com impacto, calendário local na virada UTC, elegibilidade do saldo, consolidado parcial com inativas, requisitos do novo realizado, dashboard atual/histórico/futuro, associação sem confirmação, confirmação idempotente e simultânea, revisão de correções (inclusive entre contas), bloqueio dos endpoints comuns, desconsideração, crédito com método alterado, transferência própria identificada, ordem/intervalos do extrato e preservação da migration. Interface: abertura, realização, correção, extrato, confirmação em conta inativa e desconsideração.

## Fase 2: contratos e decisões

| Operação | Endpoint |
|---|---|
| Definir/corrigir abertura | `PUT /api/contas/{id}/abertura` |
| Saldo por conta | `GET /api/contas/{id}/saldo?data=AAAA-MM-DD` |
| Consolidado | `GET /api/contas/saldos?data=AAAA-MM-DD` |
| Extrato inclusivo | `GET /api/contas/{id}/extrato?inicio=AAAA-MM-DD&fim=AAAA-MM-DD` |
| Novo realizado | `POST /api/transacoes/realizadas` |
| Confirmar histórico | `POST /api/transacoes/{id}/confirmacao` |
| Corrigir confirmado | `PUT /api/transacoes/{id}/correcao` |
| Desconsiderar com motivo | `POST /api/transacoes/{id}/desconsideracao` |
| Indicar/remover transferência própria pendente | `PUT /api/transacoes/{id}/classificacao` |
| Consultar revisões | `GET /api/transacoes/{id}/revisoes` |
| Revisão filtrada | `GET /api/transacoes?estado=NaoReconciliada&contaId=1` |

Abertura, confirmação, correção, desconsideração e classificação recebem `versao` retornada nas consultas. Versão desatualizada ou disputa de gravação retorna 409; confirmação idêntica já realizada continua idempotente. Correção e desconsideração exigem motivo. Revisões armazenam instante UTC e snapshots JSON antes/depois; datas financeiras usam calendário local e DateOnly.

O POST legado mantém sua semântica não reconciliada. PUT e associação comuns somente alteram não reconciliados. DELETE de transações existentes retorna 409 e orienta desconsiderar, preservando histórico. Origem de registros antigos permanece nula na migration; método/parcelamento/vínculos identificam crédito para proteção, preservada numa edição explícita. Método Transferencia não classifica automaticamente transferência própria.

Extrato e saldos são calculados sob demanda, sem paginação ou projeção. Conta sem abertura não presume zero. Inativas continuam no consolidado. Não há automação salarial, previsões, operação de transferência, domínio de cartão/fatura ou tarefas agendadas novas. Os campos numéricos legados permanecem; o bloco `financeiro` contém os novos indicadores independentes. Não há ligação automática entre pagamento manual de fatura antiga e compras.

## Inventário da implementação da Fase 2

Caminhos relativos a `ControleFinanceiro.API/`, salvo indicação em contrário.

Arquivos criados:

```text
Enums/EstadoTransacao.cs
Enums/OrigemRegistroTransacao.cs
Enums/ClassificacaoPendenteTransacao.cs
Models/RevisaoTransacao.cs
Services/MovimentacaoService.cs
Services/SaldoService.cs
DTOs/AberturaContaDto.cs
DTOs/TransacaoRealizadaCreateDto.cs
DTOs/ConfirmarTransacaoDto.cs
DTOs/CorrigirTransacaoDto.cs
DTOs/DesconsiderarTransacaoDto.cs
DTOs/ClassificarHistoricoDto.cs
DTOs/SaldoContaDto.cs
DTOs/SaldoConsolidadoDto.cs
DTOs/ExtratoContaDto.cs
DTOs/ResumoFinanceiroDto.cs
wwwroot/js/movimentacoes.js
Migrations/20260930202014_AdicionarAberturaEConfirmacao.cs
Migrations/20260930202014_AdicionarAberturaEConfirmacao.Designer.cs
../ControleFinanceiro.API.Tests/Fase2Scenario.cs
../ControleFinanceiro.API.Tests/AberturaTests.cs
../ControleFinanceiro.API.Tests/SaldoTests.cs
../ControleFinanceiro.API.Tests/ReconciliacaoTests.cs
../ControleFinanceiro.API.Tests/ExtratoTests.cs
../ControleFinanceiro.API.Tests/Fase2MigrationTests.cs
../ControleFinanceiro.API.Tests/Fase2InterfaceTests.cs
```

Arquivos alterados nesta implementação:

```text
Models/Conta.cs
Models/Transacao.cs
Data/AppDbContext.cs
DTOs/ContaReadDto.cs
DTOs/TransacaoReadDto.cs
DTOs/DashboardDto.cs
DTOs/BalancoMensalDto.cs
Controllers/ContasController.cs
Controllers/TransacoesController.cs
Controllers/DashboardController.cs
Program.cs
wwwroot/index.html
wwwroot/js/app.js
wwwroot/js/contas.js
wwwroot/css/style.css
Migrations/AppDbContextModelSnapshot.cs
docs/ROADMAP.md
../ControleFinanceiro.API.Tests/SqlFixture.cs
../ControleFinanceiro.API.Tests/README.md
```

As atualizações prévias em REQUIREMENTS.md e DOMAIN_MODEL.md foram preservadas. Migrations anteriores e arquivos dos testes da Fase 1 não foram alterados. A nova migration substitui o índice simples por índice composto iniciado por ContaId, adiciona colunas/tabela/restrições e não apaga registros; seu Down remove os novos dados, portanto não deve ser usado como alternativa a backup.
