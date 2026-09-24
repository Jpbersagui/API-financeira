# Controle Financeiro — Roadmap

## 1. Objetivo

Este documento define a ordem de evolução do sistema.

A implementação deve ser incremental.

O fato de uma funcionalidade estar documentada em REQUIREMENTS.md ou DOMAIN_MODEL.md não significa que ela deva ser implementada imediatamente.

Somente a fase ativa deve ser implementada.

---

# 2. Documentos obrigatórios

Antes de trabalhar em qualquer fase, ler:

```text
/docs/REQUIREMENTS.md
/docs/DOMAIN_MODEL.md
/docs/ROADMAP.md
```

Esses documentos representam a referência atual de produto e arquitetura.

Caso o código atual entre em conflito com a documentação, o conflito deve ser informado antes de qualquer decisão irreversível.

---

# 3. Regras gerais de implementação

Antes de iniciar uma fase:

1. Ler os documentos.
2. Revisar o estado atual do código.
3. Identificar arquivos afetados.
4. Identificar migrations necessárias.
5. Identificar riscos ao banco existente.
6. Informar mudanças planejadas.

Durante a fase:

* não implementar funcionalidades de fases futuras;
* não realizar refatorações não relacionadas;
* preservar funcionalidades úteis existentes;
* preservar migrations antigas;
* não inventar fatos financeiros para registros legados;
* adicionar estrutura de forma expansiva antes de remover estrutura antiga;
* utilizar transações de banco quando necessário;
* manter regras financeiras no backend;
* criar interface mínima utilizável para a funcionalidade quando aplicável.

Ao finalizar:

1. Compilar o projeto.
2. Executar testes.
3. Criar testes relevantes.
4. Informar arquivos modificados.
5. Informar migrations geradas.
6. Informar alterações no comportamento.
7. Atualizar o status deste roadmap.
8. Não iniciar automaticamente a próxima fase.

---

# 4. Estados

```text
[ ] Não iniciada
[~] Em andamento
[x] Concluída
[!] Bloqueada
```

---

# Fase 0 — Análise da base atual

Status:

```text
[x]
```

Objetivo:

Compreender o projeto existente.

Já foram analisados:

* Models
* Controllers
* DTOs
* Enums
* AppDbContext
* Migrations
* Program.cs
* frontend
* lógica atual de salário
* parcelamento
* dashboard
* persistência

Principais conclusões:

* o projeto atual pode evoluir sem ser recriado;
* `Transacao` deve evoluir para movimentação efetiva;
* previsão deve ser conceito separado;
* salário automático atual precisa ser suspenso;
* cartão precisa possuir domínio próprio;
* histórico legado exige migração segura;
* migrations antigas devem ser preservadas;
* testes automatizados precisam ser introduzidos.

Nenhuma implementação foi realizada nesta fase.

---

# Fase 1 — Contas e preparação do legado

Status:

```text
[x]
```

## Objetivo

Introduzir contas financeiras e preparar o banco atual para evolução sem destruir ou interpretar incorretamente dados existentes.

---

## Escopo

Implementar:

* entidade `Conta`;
* `TipoConta`;
* criar, listar, consultar e editar contas;
* inativar e reativar contas;
* não oferecer exclusão física de contas nesta fase;
* adicionar associação opcional entre registros antigos e conta;
* exigir conta ativa para novos lançamentos comuns;
* manter temporariamente compras no fluxo legado de cartão de crédito sem `ContaId`;
* permitir associação manual de histórico a contas ativas ou inativas;
* preservar registros legados ainda sem conta;
* deixar explícito que associação de conta não confirma realização;
* separar validações de criação e atualização de transações quando necessário;
* criar operação específica para associação de conta ao histórico;
* adaptar temporariamente o dashboard para identificar seus números como resumo do modelo legado;
* suspender a criação automática de salário realizado;
* remover o fallback silencioso para banco InMemory no funcionamento normal;
* adicionar testes automatizados necessários para a fase.

---

## Conta

Começar com estrutura suficiente para identificação e organização.

Campos iniciais podem incluir:

```text
Id
Nome
Tipo
Ativa
```

A abertura financeira completa entra na Fase 2.

Evitar introduzir um simples `SaldoAtual` mutável nesta fase.

---

## Histórico legado

`ContaId` deve permanecer opcional temporariamente para registros antigos.

Não associar automaticamente todos os registros a uma conta inventada.

A associação manual de uma conta representa somente organização do histórico.

```text
Conta associada
≠
Movimentação confirmada
```

Todos os registros pertencentes ao modelo antigo continuam não reconciliados até a implementação da Fase 2.

A Fase 2 não poderá utilizar:

```text
ContaId != null
```

como critério de realização.

Registros históricos podem ser associados inclusive a contas inativas, pois elas podem representar contas antigas encerradas pelo usuário.

---

## Regra transitória para cartão de crédito

Durante a Fase 1, o domínio próprio de cartões ainda não existe.

Portanto:

### Novo lançamento comum

Deve possuir conta ativa.

### Nova compra no fluxo legado de crédito

Pode continuar sem `ContaId`.

Não presumir que uma conta bancária informada representa a conta futura de pagamento da fatura.

### Crédito antigo

Não associar automaticamente conta bancária.

A migração completa será tratada nas fases de cartões.

---

## Regras de contas

A Fase 1 deve permitir:

* criar;
* listar;
* consultar;
* editar;
* inativar;
* reativar.

Não implementar exclusão física.

Uma conta inativa:

* não pode ser usada em novos lançamentos comuns;
* permanece disponível no histórico;
* pode receber associação de registros históricos;
* não deve impedir edição de outros campos de um registro já vinculado a ela.

---

## Criação e atualização de transações

Novos lançamentos comuns devem exigir conta ativa.

Registros antigos sem conta devem continuar podendo ser editados.

Caso o contrato atual prejudique esse comportamento, criar contrato separado, como:

```text
TransacaoUpdateDto
```

A associação de conta deve possuir operação específica, alterando somente o vínculo.

---

## Dashboard provisório

A Fase 1 não entrega saldo bancário confiável.

Os cálculos existentes podem permanecer temporariamente, porém devem ser identificados como resumo dos lançamentos cadastrados.

Utilizar rótulos semelhantes a:

```text
Resultado dos lançamentos
Receitas cadastradas
Despesas cadastradas
```

Exibir aviso equivalente a:

> Resumo dos lançamentos cadastrados. Estes valores ainda não representam saldo bancário confirmado.

Associar contas não deve transformar os totais antigos em saldo por conta.

---

## Salário automático

Suspender a lógica atual que cria receita realizada automaticamente na inicialização.

Não remover o cadastro ou histórico salarial ainda.

A automação correta retornará futuramente como previsão recorrente.

---

## Persistência obrigatória

Remover o fallback silencioso utilizado atualmente quando a connection string não está configurada.

No funcionamento normal, ausência de configuração válida para o banco persistente deve impedir a inicialização e apresentar mensagem clara.

`UseInMemoryDatabase` pode continuar sendo utilizado explicitamente em testes, mas não como fallback transparente para o usuário.

---

## Arquivos existentes esperados

```text
Models/Transacao.cs
Data/AppDbContext.cs
DTOs/TransacaoCreateDto.cs
DTOs/TransacaoReadDto.cs
DTOs/DashboardDto.cs
DTOs/BalancoMensalDto.cs
Controllers/TransacoesController.cs
Controllers/DashboardController.cs
Program.cs
wwwroot/index.html
wwwroot/js/app.js
wwwroot/css/style.css
Migrations/AppDbContextModelSnapshot.cs
```

---

## Arquivos novos possíveis

```text
Models/Conta.cs
Enums/TipoConta.cs
DTOs/ContaCreateDto.cs
DTOs/ContaUpdateDto.cs
DTOs/ContaReadDto.cs
DTOs/AssociarContaDto.cs
DTOs/TransacaoUpdateDto.cs
Controllers/ContasController.cs
Migrations/<timestamp>_AdicionarContas.cs
Migrations/<timestamp>_AdicionarContas.Designer.cs
```

---

## Testes iniciais

Adicionar projeto de testes separado.

Cobrir:

1. Migration preserva registros existentes.
2. Migration preserva IDs, valores, datas, salários e vínculos de parcelas.
3. Associar conta a registro legado não altera seu valor.
4. Associar conta não confirma realização.
5. Novo lançamento comum exige conta existente e ativa.
6. Nova compra pelo fluxo legado de crédito continua funcionando sem `ContaId`.
7. Crédito antigo não recebe conta bancária automaticamente.
8. Novo lançamento comum não aceita conta inativa.
9. Histórico pode ser associado a uma conta inativa.
10. Registro já ligado a uma conta posteriormente inativada pode ter outros campos editados.
11. Registro antigo sem conta pode ser editado sem associação obrigatória.
12. Conta com histórico não pode ser destruída de forma insegura.
13. Inicialização não cria receita salarial realizada.
14. Ausência de connection string persistente impede inicialização normal.
15. Dashboard não apresenta seus totais legados como saldo bancário confirmado.

Testes relacionais devem utilizar banco de teste isolado.

Nunca utilizar o banco pessoal real.


---

## Critérios de conclusão

* usuário consegue criar conta;
* usuário consegue listar e consultar contas;
* usuário consegue editar conta;
* usuário consegue inativar e reativar conta;
* não existe exclusão física de contas nessa fase;
* novos lançamentos comuns exigem conta ativa;
* compras no crédito legado continuam funcionando sem conta bancária presumida;
* registros antigos sem conta continuam preservados;
* histórico pode ser associado manualmente a conta;
* associação de conta não é apresentada como confirmação financeira;
* histórico pode utilizar conta inativa;
* salário automático realizado foi suspenso;
* fallback silencioso para InMemory foi removido;
* dashboard deixa clara sua semântica provisória;
* nova migration não destrói nem reclassifica automaticamente o histórico;
* testes previstos estão passando.


---

## Resultado da implementação

Validada em 23/09/2026. Build com zero avisos e zero erros. 23 testes aprovados, nenhum ignorado, usando bancos SQL Server isolados e teste de interface no Edge headless.

Migration `AdicionarContas` criada: tabela `Contas`, coluna nullable `Transacoes.ContaId`, índice e chave estrangeira com exclusão restrita. Migrations anteriores preservadas. Nenhuma migration aplicada ao banco pessoal durante a implementação.

Incluídos `Data/AppDbContextFactory.cs`, `wwwroot/js/contas.js` e o projeto `ControleFinanceiro.API.Tests`, além dos arquivos previstos. A fábrica de design evita executar a inicialização ao gerar migrations.

Contratos numéricos antigos dos resumos foram mantidos por compatibilidade, com aviso explícito e `SaldoBancarioConfirmado = false`. Todos os registros continuam não reconciliados. Associação manual rejeita crédito legado; edição não modifica conta. O parcelamento existente foi preservado e suas gravações tornadas atômicas.

Comandos, cobertura e decisões transitórias estão em `../../ControleFinanceiro.API.Tests/README.md`.

A inicialização normal continua aplicando migrations pendentes ao banco configurado. Fazer backup antes da primeira execução com esta versão.

Fase 2 não iniciada.

---

# Fase 2 — Movimentações efetivas, abertura e saldo por conta

Status:

```text
[ ]
```

## Objetivo

Transformar `Transacao` em representação clara de dinheiro efetivamente movimentado.

---

## Implementar

* abertura financeira da conta;
* data de início do acompanhamento;
* entradas realizadas;
* saídas realizadas;
* saldo por conta;
* saldo consolidado;
* extrato básico;
* revisão progressiva do legado;
* indicação de histórico ainda não confirmado.

---

## Regra principal

Somente movimentações efetivas alteram saldo.

---

## Abertura

Permitir:

```text
Começar a acompanhar esta conta em DD/MM/AAAA com R$ X.
```

Esse valor não deve ser receita.

---

## Critérios de conclusão

* saldo por conta funciona;
* saldo consolidado funciona;
* abertura não conta como receita;
* registros não revisados não são apresentados silenciosamente como saldo confirmado;
* extrato básico funciona;
* saldo usa somente fatos considerados realizados.

---

# Fase 3 — Previsões e realizações parciais

Status:

```text
[ ]
```

## Objetivo

Introduzir a separação formal entre planejamento financeiro e dinheiro realizado.

---

## Implementar

* `Previsao`;
* vínculo entre previsão e transações;
* previsão avulsa;
* realização parcial;
* valor previsto original;
* valor final;
* encerramento;
* cancelamento;
* cálculo de valor em aberto.

---

## Casos obrigatórios

### Salário parcial

```text
Previsto: 3000
Recebido: 1000
Restante: 2000
```

Depois:

```text
Recebido adicional: 2000
Restante: 0
```

### Conta variável

```text
Previsto: 250
Valor final: 273,18
Pago: 273,18
```

### Encerramento divergente

```text
Previsto: 250
Pago: 230
Encerrado: sim
Restante financeiro: 0
```

---

## Critérios de conclusão

* previsão não altera saldo;
* uma previsão suporta múltiplas realizações;
* uma transação pode existir sem previsão;
* previsão pode existir sem recorrência;
* cancelamento e encerramento funcionam.

---

# Fase 4 — Categorias cadastráveis

Status:

```text
[ ]
```

## Objetivo

Remover dependência permanente de categorias hardcoded.

---

## Implementar

* entidade Categoria;
* CRUD;
* inativação;
* seleção no frontend;
* vínculo entre transações/previsões e categoria;
* estratégia de migração das strings existentes.

---

## Migração

Não fundir automaticamente categorias semelhantes sem regra explícita.

Exemplo:

```text
Mercado
mercado
Supermercado
```

Podem exigir revisão.

---

## Critérios de conclusão

* frontend carrega categorias da API;
* novas categorias podem ser criadas;
* categorias antigas permanecem preservadas;
* categoria inativa não destrói histórico.

---

# Fase 5 — Transferências

Status:

```text
[ ]
```

## Objetivo

Representar corretamente movimentação entre contas próprias.

---

## Implementar

* origem;
* destino;
* valor;
* data;
* gravação atômica;
* integração ao saldo;
* integração ao extrato;
* exclusão correta dos totais de receita/despesa consolidada.

---

## Regra

```text
Origem: -500
Destino: +500
Consolidado: 0
```

---

## Critérios de conclusão

* transferência altera duas contas;
* saldo consolidado não muda;
* receita não aumenta;
* despesa não aumenta;
* falha intermediária não deixa somente metade da operação salva.

---

# Fase 6 — Recorrências e salário

Status:

```text
[ ]
```

## Objetivo

Criar regras recorrentes que geram previsões sem depender de execução contínua.

---

## Implementar

* Recorrencia;
* vigência;
* periodicidade;
* geração de previsões;
* competência;
* unicidade;
* sincronização;
* integração do salário atual;
* interface específica para cadastrar salário.

---

## Sincronização

Deve:

* funcionar após dias ou meses sem execução;
* criar competências ausentes;
* criar somente previsões;
* não criar movimentações realizadas;
* ser idempotente;
* preservar cancelamentos;
* preservar parcialidades;
* considerar recorrências encerradas quando houver competências históricas faltantes.

---

## Unicidade

Considerar:

```text
RecorrenciaId
AnoCompetencia
MesCompetencia
```

com proteção também no banco.

---

## Mudança de regra

Alterar uma recorrência não pode reescrever meses anteriores.

Implementar vigência usando versão ou estratégia equivalente.

---

## Salário

Migrar gradualmente a entidade atual.

Não remover dados antigos antes de garantir equivalência.

---

## Critérios de conclusão

* recorrência mensal funciona;
* aplicação pode ficar fechada;
* competências ausentes são criadas corretamente;
* sincronização repetida não duplica;
* salário gera previsão;
* salário não gera receita realizada automaticamente;
* adiantamento e complemento continuam funcionando através da relação previsão/transação.

---

# Fase 7 — Cartões, compras e ciclos

Status:

```text
[ ]
```

## Objetivo

Transformar cartão de crédito em domínio próprio.

---

## Implementar

* CartaoCredito;
* CRUD;
* limite;
* fechamento;
* vencimento;
* conta padrão;
* CompraCartao;
* compra à vista;
* categoria;
* ciclo de fatura;
* primeira alocação em fatura.

---

## Regras

Compra no cartão:

```text
não reduz saldo bancário
```

Compra participa de:

```text
consumo
```

Fatura participa de:

```text
compromisso futuro
```

Pagamento participa de:

```text
fluxo de caixa
```

---

## Datas

Criar regra centralizada para identificar fatura correta.

Cobrir:

* antes do fechamento;
* depois do fechamento;
* dia do fechamento;
* virada do ano;
* meses menores.

---

## Critérios de conclusão

* múltiplos cartões podem ser cadastrados;
* compra identifica cartão;
* compra entra na fatura correta;
* compra não reduz conta bancária;
* consumo por categoria pode ser identificado.

---

# Fase 8 — Parcelamento, faturas e pagamento

Status:

```text
[ ]
```

## Objetivo

Completar fluxo de crédito.

---

## Implementar

* geração de parcelas;
* arredondamento;
* associação a faturas;
* faturas futuras;
* total de fatura;
* PagamentoFatura ou modelo equivalente;
* saída da conta;
* prevenção de dupla contabilização.

---

## Parcelamento

Não utilizar somente:

```text
Data.AddMonths()
```

Usar ciclo real do cartão.

---

## Arredondamento

A soma das parcelas deve ser igual ao valor original.

---

## Pagamento

Pagamento reduz saldo da conta.

Não criar segunda despesa econômica na análise de consumo.

---

## Decisões da fase

Antes de implementar, definir:

* pagamento parcial;
* pagamento antecipado;
* atraso;
* juros;
* antecipação de parcelas;
* liberação de limite.

Funcionalidades que não entrarem devem permanecer documentadas como backlog.

---

## Critérios de conclusão

* parcelas corretas;
* faturas corretas;
* pagamentos corretos;
* saldo bancário correto;
* consumo correto;
* projeção não duplica compra + fatura.

---

# Fase 9 — Visão mensal, projeções e planejamento

Status:

```text
[ ]
```

## Objetivo

Substituir a experiência mensal da planilha através de consultas próprias do backend.

---

## Implementar

* saldo inicial do mês;
* entradas realizadas;
* saídas realizadas;
* saldo ao fim do período;
* saldo atual;
* receitas previstas;
* despesas previstas;
* pendências anteriores;
* saldo projetado;
* compromissos futuros;
* resumo por conta;
* resumo por cartão;
* consumo por categoria.

---

## Visões

O backend deve distinguir:

```text
Fluxo de caixa
```

e:

```text
Consumo
```

---

## Extrato

Fornecer:

```text
Data
Descrição
Entrada
Saída
Saldo após linha
```

Com ordenação determinística.

---

## Meses históricos

Ao consultar mês antigo, distinguir:

* saldo naquele mês;
* saldo final daquele mês;
* saldo atual de hoje.

---

## Critérios de conclusão

O frontend consegue montar a visão mensal sem reconstruir regras financeiras importantes.

---

# Fase 10 — Dashboard e refinamento da interface

Status:

```text
[ ]
```

## Objetivo

Consolidar a experiência do usuário.

---

## Dashboard

Apresentar:

* saldo por conta;
* saldo consolidado;
* saldo projetado;
* receitas realizadas;
* despesas realizadas;
* valores previstos;
* pendências;
* próximas faturas;
* compromissos futuros.

---

## Navegação

Possível estrutura:

```text
Dashboard
Movimentações
Meses
Contas
Cartões
Recorrências
Planejamento
```

---

## Princípio

```text
Resumo primeiro.
Detalhe sob demanda.
```

---

## Refinamentos

Revisar:

* formulários;
* mensagens;
* feedback visual;
* datas;
* formatação monetária;
* navegação;
* acessibilidade básica;
* responsividade;
* segurança de conteúdo no HTML.

Corrigir problemas como uso inseguro de `innerHTML` quando aplicável.

---

# Fase 11 — Testes integrados e estabilização

Status:

```text
[ ]
```

## Objetivo

Garantir consistência financeira do sistema completo.

---

## Cobrir

* abertura de conta;
* saldo;
* previsão;
* realizações parciais;
* recorrências;
* sincronização;
* concorrência;
* transferências;
* fechamento de cartão;
* faturas;
* parcelas;
* arredondamento;
* pagamentos;
* fluxo de caixa;
* consumo;
* projeções;
* migrations;
* datas;
* legado.

---

## Testes temporais

A data atual deve ser controlável durante os testes.

Evitar dependência direta de `DateTime.Now` em regras difíceis de testar.

Utilizar abstração ou estratégia equivalente quando necessário.

---

# 5. Backlog

Não implementar sem fase específica:

* antecipação de parcelas;
* pagamento parcial avançado de fatura;
* juros automáticos;
* renegociação;
* importação de OFX;
* importação CSV;
* exportação Excel;
* orçamento mensal por categoria;
* metas financeiras;
* investimentos;
* contas compartilhadas;
* múltiplos usuários;
* autenticação;
* backup automático;
* sincronização em nuvem;
* notificações externas.

---

# 6. Regra final

Quando uma regra financeira importante estiver ambígua:

```text
não assumir silenciosamente.
```

Primeiro:

```text
documentar
→ decidir
→ testar
→ implementar
```

Preservar consistência financeira tem prioridade sobre implementar rapidamente.
