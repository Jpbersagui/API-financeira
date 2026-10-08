# Validação das Fases 1, 2 e 3

Resultado atual (02/10/2026): **76 testes aprovados**, nenhum ignorado ou com falha. Build Release com zero avisos/erros; migration expansiva validada exclusivamente em bancos isolados; EF sem alterações pendentes. A seção Fase 3 ao final registra cobertura, decisões e inventário. As seções anteriores preservam os resultados históricos.

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

## Fase 3 — resultado e execução

Validada em 02/10/2026: **76 testes aprovados** (50 anteriores + 26 novos), nenhum ignorado. Build Release com zero avisos/erros. Cinco casos de interface no Edge headless, incluindo os dois novos com viewport desktop de 1280 pixels e móvel de 390 pixels. Nenhum teste acessa o banco pessoal.

```powershell
dotnet build ControleFinanceiro.API.Tests/ControleFinanceiro.API.Tests.csproj --configuration Release --no-restore
dotnet test ControleFinanceiro.API.Tests/ControleFinanceiro.API.Tests.csproj --configuration Release --no-build --logger "console;verbosity=minimal"
dotnet ef migrations has-pending-model-changes --project ControleFinanceiro.API --configuration Release --no-build
```

A última verificação EF retornou que o modelo não possui alterações desde a última migration. A única migration criada nesta fase foi `20261002174011_AdicionarPrevisoesERealizacoes`. Migrations anteriores preservadas. A fixture parte de AddSalarios, aplica AdicionarContas e AdicionarAberturaEConfirmacao antes da nova migration. O cenário SeedFase2 insere conta inativa com abertura, movimento confirmado e revisão; compara campos e rowversions antes/depois. Nenhum vínculo ou previsão é inferido.

Testes adicionados:

* PrevisaoTests: 11 casos de cadastro, valores, original imutável, final zero, competência, estados, contas, dashboard e calendário local.
* RealizacaoPrevisaoTests: 6 casos de realização parcial/múltipla/integral/excedente, contas diferentes, correção/desconsideração, concorrência e rollback.
* VinculoPrevisaoTests: 6 casos de vínculo único/idempotente, desvínculo sem alterar saldo, histórico anterior à abertura/inativa, bloqueios e concorrência entre previsões.
* Fase3MigrationTests: preservação dos dados das migrations anteriores, tabelas novas vazias, vínculo nulo, modelo sincronizado.
* Fase3InterfaceTests: dois casos (desktop/celular) de previsão, realização parcial/total, final, vínculo/desvínculo, encerramento/reabertura, cancelamento e histórico, sem erros JavaScript ou overflow horizontal.

O teste de atomicidade injeta falha ao gravar RevisaoPrevisao, após o INSERT da movimentação dentro da transação. Confere que movimentação, vínculo, versão e revisão não persistem. Concorrência verifica HTTP 409 e somente um movimento efetivo; vínculos concorrentes não compartilham uma transação.

Decisões de implementação: Transacao.PrevisaoId opcional, relação integral sem rateio; totais derivados sem persistência; Serializable + rowversion; atualização de participante invalida a versão da previsão; repetição do vínculo existente é idempotente. Criação vinculada reutiliza o preparo de MovimentacaoService sem transações aninhadas. O tipo/original não entram na edição; propriedades não previstas no DTO de cadastro/edição são rejeitadas. SemValor é situação calculada para final zero sem pagamentos. Edição e novo valor final exigem estado Ativa. Encerramento/cancelamento/reabertura e desvinculação exigem motivo.

As fórmulas de SaldoService não foram alteradas. O bloco dashboard.previsoes é separado de financeiro e das métricas antigas. Seu realizado é o total atual das previsões com DataPrevista no mês, mesmo que o pagamento tenha ocorrido depois. Canceladas não entram nos totais; encerradas preservam comparação e restante zero.

Limitações: consultas sem paginação; resumo não reconstrói uma fotografia passada dos estados; correções financeiras são consultadas pelo histórico do movimento. Nenhuma recorrência, categoria cadastrável, transferência, cartão, fatura ou saldo projetado foi antecipado. A inicialização normal continua aplicando migrations pendentes: backup restaurável antes de executar contra o banco pessoal.

## Inventário da Fase 3

Caminhos relativos à raiz do workspace. As alterações de preparação já existentes nos três documentos de domínio foram preservadas; DOMAIN_MODEL.md não precisou de nova alteração nesta implementação.

Arquivos criados:

```text
ControleFinanceiro.API/Models/Previsao.cs
ControleFinanceiro.API/Models/RevisaoPrevisao.cs
ControleFinanceiro.API/Enums/EstadoPrevisao.cs
ControleFinanceiro.API/Enums/SituacaoPrevisao.cs
ControleFinanceiro.API/Services/PrevisaoService.cs
ControleFinanceiro.API/Controllers/PrevisoesController.cs
ControleFinanceiro.API/DTOs/PrevisaoCreateDto.cs
ControleFinanceiro.API/DTOs/PrevisaoUpdateDto.cs
ControleFinanceiro.API/DTOs/PrevisaoReadDto.cs
ControleFinanceiro.API/DTOs/PrevisaoFiltroDto.cs
ControleFinanceiro.API/DTOs/DefinirValorFinalDto.cs
ControleFinanceiro.API/DTOs/RealizarPrevisaoDto.cs
ControleFinanceiro.API/DTOs/VincularTransacaoDto.cs
ControleFinanceiro.API/DTOs/DesvincularTransacaoDto.cs
ControleFinanceiro.API/DTOs/AlterarEstadoPrevisaoDto.cs
ControleFinanceiro.API/DTOs/RevisaoPrevisaoDto.cs
ControleFinanceiro.API/DTOs/ResumoPrevisoesDto.cs
ControleFinanceiro.API/Migrations/20261002174011_AdicionarPrevisoesERealizacoes.cs
ControleFinanceiro.API/Migrations/20261002174011_AdicionarPrevisoesERealizacoes.Designer.cs
ControleFinanceiro.API/wwwroot/js/previsoes.js
ControleFinanceiro.API.Tests/PrevisaoScenario.cs
ControleFinanceiro.API.Tests/PrevisaoTests.cs
ControleFinanceiro.API.Tests/RealizacaoPrevisaoTests.cs
ControleFinanceiro.API.Tests/VinculoPrevisaoTests.cs
ControleFinanceiro.API.Tests/Fase3MigrationTests.cs
ControleFinanceiro.API.Tests/Fase3InterfaceTests.cs
```

Arquivos alterados nesta implementação:

```text
ControleFinanceiro.API/Models/Transacao.cs
ControleFinanceiro.API/Data/AppDbContext.cs
ControleFinanceiro.API/Services/MovimentacaoService.cs
ControleFinanceiro.API/Controllers/TransacoesController.cs
ControleFinanceiro.API/Controllers/DashboardController.cs
ControleFinanceiro.API/DTOs/TransacaoReadDto.cs
ControleFinanceiro.API/DTOs/DashboardDto.cs
ControleFinanceiro.API/Program.cs
ControleFinanceiro.API/Migrations/AppDbContextModelSnapshot.cs
ControleFinanceiro.API/wwwroot/index.html
ControleFinanceiro.API/wwwroot/css/style.css
ControleFinanceiro.API/wwwroot/js/app.js
ControleFinanceiro.API/wwwroot/js/contas.js
ControleFinanceiro.API/wwwroot/js/movimentacoes.js
ControleFinanceiro.API/docs/REQUIREMENTS.md
ControleFinanceiro.API/docs/ROADMAP.md
ControleFinanceiro.API.Tests/AberturaTests.cs
ControleFinanceiro.API.Tests/SqlFixture.cs
ControleFinanceiro.API.Tests/README.md
```

AberturaTests teve apenas a construção do serviço adaptada à dependência PrevisaoService, preservando as verificações anteriores. SqlFixture ganhou o cenário sintético da Fase 2 e a comparação adicional da migration.
