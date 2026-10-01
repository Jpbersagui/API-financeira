# Controle Financeiro — Modelo de Domínio

## 1. Objetivo

Este documento descreve os principais conceitos financeiros do sistema e suas regras.

Ele não representa obrigatoriamente a estrutura exata final do banco de dados.

Os nomes, relacionamentos e detalhes técnicos podem evoluir durante a implementação desde que os requisitos definidos em REQUIREMENTS.md sejam preservados.

---

# 2. Conceitos fundamentais

O sistema deve separar claramente:

```text
Previsão financeira
≠
Movimentação financeira realizada
```

A previsão representa expectativa ou obrigação.

A transação representa dinheiro efetivamente movimentado.

---

# 3. Modelo conceitual principal

O domínio provavelmente caminhará para algo semelhante:

```text
Conta

Categoria

Previsao
 └── pode estar relacionada a Recorrencia
      └── pode possuir regra de vigência/versionamento

Transacao
 └── movimentação REAL de uma Conta
      └── pode realizar total ou parcialmente uma Previsao

Transferencia

CartaoCredito
 ├── CompraCartao
 │    └── Parcela
 ├── Fatura
 └── PagamentoFatura
```

Não implementar todas essas entidades simultaneamente.

A implementação deve seguir ROADMAP.md.

---

# 4. Conta

Representa onde o dinheiro do usuário está armazenado.

Campos conceituais possíveis:

```text
Id
Nome
Tipo
Ativa
DataAbertura
ValorAbertura
```

Tipos iniciais:

```text
ContaCorrente
Poupanca
Carteira
```

Outros tipos podem ser adicionados futuramente.

---

## 4.1 Abertura financeira

Uma conta deve possuir um marco de início do acompanhamento.

Exemplo:

```text
Conta: Nubank
Data de início: 01/10/2026
Valor existente: R$ 850,00
```

Esse valor representa abertura financeira.

Não é uma receita.

A decisão da Fase 2 é utilizar campos na própria Conta: `DataAbertura` (`DateOnly?`) e `ValorAbertura` (`decimal?`). Ambos são informados juntos. Ausência não equivale a abertura zero.

A posição vale no início do dia, antes dos movimentos daquele dia. Aceitar valor positivo, zero ou negativo; rejeitar data futura. Abertura não é receita nem transação especial.

Corrigir abertura exige ação específica. Uma alteração de data que retire movimentos confirmados do período acompanhado deve ser bloqueada até revisão explícita. Consultas anteriores à abertura não possuem saldo conhecido.

---

# 5. Categoria

Representa classificação financeira.

Campos conceituais:

```text
Id
Nome
TipoOpcional
Ativa
```

Exemplos:

```text
Alimentação
Moradia
Transporte
Lazer
Salário
Financiamento
```

Categorias utilizadas no histórico devem poder ser inativadas sem destruir vínculos antigos.

---

# 6. Transação

`Transacao` deve evoluir para representar movimentação financeira efetivamente realizada.

Uma transação altera dinheiro real de uma conta.

Campos conceituais possíveis:

```text
Id
Descricao
Valor
Data
ContaId
CategoriaId
Tipo
MetodoPagamento
Observacoes
PrevisaoId opcional
Origem
```

---

## 6.1 Naturezas de movimentação

A implementação deve distinguir corretamente:

```text
Receita
Despesa
Transferencia
PagamentoFatura
Abertura
```

A forma exata de representar movimentos especiais pode utilizar enum, entidades específicas ou vínculos.

Evitar classificar transferência entre contas próprias como receita ou despesa.

---

# 7. Previsão

Representa compromisso ou expectativa financeira.

Campos conceituais possíveis:

```text
Id
Descricao
Tipo
ValorPrevistoOriginal
ValorFinalOpcional
DataPrevista
ContaIdOpcional
CategoriaId
RecorrenciaIdOpcional
AnoCompetenciaOpcional
MesCompetenciaOpcional
Encerrada
Cancelada
Observacoes
```

---

## 7.1 Valor original

A previsão original deve poder ser preservada.

Exemplo:

```text
Previsto: R$ 250
Valor final conhecido: R$ 273,18
```

Não sobrescrever silenciosamente o valor originalmente planejado caso essa informação seja relevante para comparação.

---

# 8. Relação entre Previsão e Transação

A relação conceitual é:

```text
Previsao 1
    ↓
Transacao 0..N
```

Uma previsão pode não possuir nenhuma transação.

Uma previsão pode possuir várias transações.

Uma transação pode existir sem previsão.

---

## 8.1 Realização parcial

Exemplo:

```text
Previsao:
Salário Setembro
Valor previsto: 3000
```

Movimentações:

```text
Adiantamento: 1000
Pagamento: 2000
```

Valor realizado:

```text
3000
```

Valor restante:

```text
0
```

---

## 8.2 Encerramento com valor divergente

Exemplo:

```text
Previsto: 250
Pago: 230
```

Caso o usuário informe que a obrigação foi encerrada:

```text
Restante financeiro: 0
```

O sistema não deve manter automaticamente uma dívida fictícia de R$ 20.

Isso exige conceito de encerramento além de simples comparação matemática.

---

# 9. Status derivados e persistidos

Algumas situações podem ser calculadas.

Exemplos:

```text
Parcialmente realizado
Vencido
Pendente
```

Outras devem ser persistidas quando representam decisão do usuário.

Exemplos:

```text
Cancelado
Encerrado
```

Evitar persistir estados derivados quando eles podem ser calculados com segurança.

---

# 10. Transferência

Representa uma única operação financeira entre duas contas próprias.

Campos conceituais possíveis:

```text
Id
ContaOrigemId
ContaDestinoId
Valor
Data
Observacoes
```

A operação deve resultar em:

```text
Origem: -Valor
Destino: +Valor
```

Saldo consolidado:

```text
sem alteração
```

---

## 10.1 Atomicidade

A transferência deve ser salva como uma única operação lógica.

Não pode ocorrer situação permanente onde a saída foi registrada e a entrada não.

---

# 11. Recorrência

Representa regra que gera previsões repetidas.

Campos conceituais possíveis:

```text
Id
Descricao
Tipo
ValorEsperado
ContaIdOpcional
CategoriaId
Periodicidade
DiaEsperado
DataInicio
DataFim
Ativa
FinalidadeOpcional
```

Exemplos:

```text
Salário
Spotify
Condomínio
Financiamento
Seguro
```

---

# 12. Ocorrência recorrente

A recorrência representa regra.

A ocorrência representa uma competência específica.

Exemplo:

```text
Recorrência:
Spotify mensal
```

```text
Ocorrência:
Spotify Setembro/2026
```

Essa ocorrência pode ser representada diretamente através de uma `Previsao` gerada pela recorrência ou através de estrutura intermediária equivalente.

Evitar duplicar entidades sem necessidade.

---

# 13. Identidade por competência

Uma ocorrência mensal deve possuir identidade única lógica.

Exemplo:

```text
RecorrenciaId
+
AnoCompetencia
+
MesCompetencia
```

A implementação deve considerar constraint única no banco.

A unicidade não deve depender apenas de:

```text
AnyAsync()
```

antes do insert.

O banco deve participar da proteção contra duplicidade.

---

# 14. Sincronização

Deve existir serviço ou componente capaz de sincronizar previsões recorrentes.

Possível nome:

```text
FinancialSynchronizationService
```

ou equivalente.

Esse serviço não é worker permanente.

Ele é executado quando necessário.

---

## 14.1 Fluxo conceitual

```text
1. Determinar intervalo necessário.
2. Buscar recorrências válidas naquele intervalo.
3. Enumerar competências esperadas.
4. Verificar ocorrências existentes.
5. Criar somente as ausentes.
6. Persistir como previsões.
7. Nunca marcar como realizadas automaticamente.
```

---

## 14.2 Idempotência

Executar a sincronização várias vezes deve produzir o mesmo estado.

Regras:

* não duplicar ocorrência;
* não recriar ocorrência cancelada;
* não sobrescrever ocorrência parcialmente realizada;
* permitir recuperação após falha parcial;
* utilizar constraint única;
* utilizar transação quando necessário.

---

# 15. Vigência de recorrência

Alterações de recorrência não podem reescrever histórico.

Exemplo:

```text
Salário:
3000 até outubro
3500 a partir de novembro
```

A modelagem pode utilizar:

```text
RecorrenciaVersao
```

ou mecanismo equivalente.

`RecorrenciaVersao` é uma possibilidade arquitetural, não uma obrigação antecipada.

Requisito obrigatório:

mudanças possuem vigência.

---

# 16. Salário

Salário deve preferencialmente utilizar o mecanismo de recorrência.

A interface pode continuar oferecendo:

```text
Cadastrar salário
```

Internamente:

```text
Recorrencia
Tipo: Receita
Finalidade: Salario
```

ou representação equivalente.

---

# 17. Cartão de crédito

Representa um cartão individual do usuário.

Campos conceituais possíveis:

```text
Id
Nome
Limite
DiaFechamento
DiaVencimento
ContaPagamentoPadraoId
Ativo
```

---

# 18. Compra no cartão

Representa a compra original.

Campos conceituais possíveis:

```text
Id
CartaoCreditoId
Descricao
CategoriaId
DataCompra
ValorTotal
QuantidadeParcelas
```

Uma compra não reduz imediatamente o saldo bancário.

---

# 19. Parcela

Representa parte da compra.

Campos conceituais possíveis:

```text
Id
CompraCartaoId
Numero
QuantidadeTotal
Valor
FaturaId
```

Invariantes:

```text
Parcela pertence a uma compra.
Parcela pertence a uma fatura.
Uma parcela não pertence a duas faturas.
```

---

# 20. Arredondamento

A soma das parcelas deve corresponder exatamente ao valor total da compra.

Exemplo:

```text
Valor total: 100
Parcelas: 3
```

Não gerar:

```text
33,33
33,33
33,33
= 99,99
```

A diferença deve ser tratada explicitamente.

---

# 21. Fatura

Representa ciclo de cobrança do cartão.

Campos conceituais possíveis:

```text
Id
CartaoCreditoId
AnoCompetencia
MesCompetencia
DataFechamento
DataVencimento
```

Status pode ser:

```text
Aberta
Fechada
Paga
```

Mas parte desses estados pode ser derivada.

Definir na fase correspondente o que realmente precisa ser persistido.

---

# 22. Ciclo da fatura

A fatura de uma compra deve ser determinada por:

* data da compra;
* fechamento;
* vencimento.

Não utilizar somente:

```text
DataCompra.AddMonths()
```

A lógica deve ser centralizada.

Possível componente:

```text
CreditCardInvoiceService
```

ou equivalente.

---

## 22.1 Casos especiais

A regra deve tratar:

* compra antes do fechamento;
* compra depois do fechamento;
* compra no próprio dia de fechamento;
* meses com 28, 29, 30 e 31 dias;
* virada de ano;
* mudanças futuras no fechamento;
* histórico que não pode ser reorganizado silenciosamente.

---

# 23. Configuração histórica do cartão

Alterações futuras em:

* fechamento;
* vencimento;
* conta padrão;

não devem reorganizar silenciosamente faturas históricas existentes.

Faturas já existentes devem preservar suas datas de ciclo.

---

# 24. Pagamento de fatura

Pode existir entidade explícita:

```text
PagamentoFatura
```

ou relacionamento equivalente.

Conceitualmente o pagamento deve registrar:

```text
Fatura
Conta
Valor
Data
```

Deve suportar futuramente:

* pagamento total;
* pagamento parcial;
* pagamento antecipado;
* pagamento atrasado.

A primeira implementação pode limitar escopo desde que isso esteja documentado.

---

# 25. Compra versus pagamento

A compra representa:

```text
consumo
```

O pagamento da fatura representa:

```text
fluxo de caixa
```

Não contabilizar ambos como duas despesas econômicas iguais.

---

# 26. Relatórios

Relatórios devem saber qual visão está sendo calculada.

## Fluxo de caixa

Baseado em movimentações realizadas nas contas.

## Consumo por categoria

Baseado em compras/despesas econômicas.

Essas visões podem apresentar números diferentes no mesmo mês sem que isso represente erro.

---

# 27. Limite do cartão

Limite não faz parte do saldo bancário.

O limite disponível será calculado a partir dos compromissos do cartão e pagamentos conforme política futura.

Não misturar:

```text
saldo da conta
```

com:

```text
limite disponível
```

---

# 28. Saldo de conta

Conceitualmente:

```text
Saldo =
Abertura
+ Entradas efetivas
- Saídas efetivas
```

Previsões não entram no saldo real.

Compras no cartão não reduzem saldo bancário imediatamente.

Pagamento de fatura reduz saldo bancário.

---

# 29. Saldo projetado

Conceitualmente:

```text
SaldoProjetado =
SaldoRealizadoNaReferencia
+ ValoresPendentesDeReceber
- ValoresPendentesDePagar
```

Ao calcular valores pendentes:

```text
ValorPendente =
ValorFinanceiroDaPrevisao
- RealizaçõesRelacionadas
```

considerando encerramento e cancelamento.

---

# 30. Pendências vencidas

Previsões vencidas e ainda abertas devem permanecer identificáveis.

Não duplicar pendências antigas em todos os meses futuros.

Elas devem aparecer uma única vez na projeção adequada.

---

# 31. Resumo mensal

Não precisa ser entidade persistida.

Pode ser um DTO/read model.

Campos possíveis:

```text
Ano
Mes
SaldoInicial
EntradasRealizadas
SaidasRealizadas
SaldoFimPeriodo
SaldoAtual
ReceitasPrevistas
DespesasPrevistas
PendenciasAnteriores
SaldoProjetado
ResumoContas
ResumoCartoes
```

---

# 32. Extrato

Extrato é consulta.

Pode retornar:

```text
Data
Descricao
Entrada
Saida
SaldoAposMovimentacao
Origem
```

O backend deve calcular saldo de abertura da consulta.

---

## 32.1 Paginação

Caso o extrato seja paginado, o saldo acumulado não pode ignorar movimentações anteriores à página atual.

---

# 33. Datas

Datas financeiras sem horário devem utilizar semântica de calendário.

Evitar UTC quando ele não fizer sentido.

Tipos como:

```text
DateOnly
```

devem ser considerados para:

* vencimento;
* competência;
* compra;
* pagamento;
* recebimento;
* fechamento.

---

# 34. Histórico legado

Transações antigas podem inicialmente possuir:

```text
ContaId = null
```

ou mecanismo equivalente.

Isso permite migração expansiva.

Estratégia:

```text
1. Adicionar nova estrutura.
2. Preservar registros existentes.
3. Permitir associação/revisão.
4. Migrar progressivamente.
5. Só depois tornar vínculos obrigatórios quando seguro.
```

---

# 35. Estado legado não revisado

Pode existir indicação explícita de que determinado registro antigo ainda não foi reconciliado.

O sistema não deve apresentar esses registros como saldo financeiro confirmado sem informar sua situação.

A estratégia técnica será definida durante as fases iniciais.

---

# 35.1 Associação de conta no legado

A associação entre uma `Transacao` antiga e uma `Conta` representa somente organização e classificação do histórico.

A seguinte regra deve ser preservada:

```text
ContaId != null
```

não significa:

```text
Movimentação confirmada
```

e não pode ser utilizado como critério para determinar que dinheiro efetivamente entrou ou saiu da conta.

Na Fase 1, todos os registros permanecem não reconciliados. Na Fase 2, continuam assim até confirmação manual explícita; a associação de conta não substitui essa confirmação.

---

# 35.2 Crédito legado

Durante as fases anteriores ao domínio próprio de cartões, uma transação do fluxo legado com método de pagamento de cartão de crédito pode permanecer sem `ContaId`.

Isso é intencional.

Uma compra no cartão:

```text
não movimenta imediatamente uma conta bancária
```

e, portanto, não deve receber artificialmente a conta que futuramente poderá pagar sua fatura.

Registros antigos de crédito devem permanecer preservados para migração e classificação na fase de cartões.

---

# 35.3 Regras de conta ativa e inativa durante a transição

Uma conta ativa pode ser utilizada em novos lançamentos comuns.

Uma conta inativa:

* não pode ser selecionada para novos lançamentos comuns;
* deve continuar disponível no histórico;
* deve preservar todos os vínculos existentes;
* pode receber associação manual de registros históricos, pois pode representar uma conta que já foi encerrada na vida real.

Caso um lançamento esteja vinculado a uma conta que posteriormente foi inativada, a edição de outros campos desse lançamento não deve exigir a troca da conta.

---

# 35.4 Criação, atualização e associação de transações

As validações de criação de uma nova transação e atualização de um registro legado podem ser diferentes.

Novos lançamentos comuns devem exigir uma conta ativa.

Entretanto, registros antigos ainda sem conta devem poder ser editados sem que o sistema obrigue imediatamente sua associação.

A implementação pode utilizar contratos separados, por exemplo:

```text
TransacaoCreateDto
TransacaoUpdateDto
AssociarContaDto
```

ou solução equivalente.

A operação utilizada para associar uma conta ao histórico deve alterar somente o vínculo necessário e não deve alterar valor, data ou situação financeira do registro.

---

# 35.5 Semântica transitória do dashboard

Até a implementação do modelo confiável de movimentações realizadas e saldo por conta, os indicadores antigos devem ser tratados como informações do modelo legado.

Eles não representam saldo bancário confirmado.

O backend e o frontend devem deixar essa limitação explícita.

Nenhuma consulta pode considerar um registro realizado apenas porque uma conta foi associada a ele.

---

# 36. Exclusão

Evitar exclusão destrutiva de entidades referenciadas.

Preferir inativação para:

* contas;
* categorias;
* cartões;
* recorrências.

Uma conta inativa continua existindo no histórico.

Uma categoria inativa continua existindo em transações antigas.

---

# 37. Banco de dados

Entity Framework Core continuará sendo utilizado.

Regras:

* preservar migrations existentes;
* adicionar migrations novas;
* não editar migrations antigas para reescrever histórico;
* testar migrations;
* realizar backup antes de aplicar mudanças importantes no banco pessoal.

---

# 38. Atomicidade

Operações compostas devem usar transação de banco quando necessário.

Exemplos:

* transferência;
* geração de parcelas;
* sincronização de competências;
* pagamento de fatura quando gerar movimentações relacionadas.

---

# 39. Serviços possíveis

Conforme a complexidade surgir, podem existir serviços como:

```text
FinancialSynchronizationService
FinancialQueryService
BalanceService
ProjectionService
CreditCardInvoiceService
RecurringTransactionService
```

Não criar serviços por antecipação sem regra suficiente.

---

# 40. Invariantes

As seguintes regras devem sempre ser preservadas:

1. Previsão não altera saldo real.
2. Transação representa dinheiro efetivamente movimentado.
3. Uma previsão pode possuir várias realizações.
4. Uma realização pode existir sem previsão.
5. Sincronização repetida não gera duplicatas.
6. Cancelamento não é recriado pela sincronização.
7. Transferência não altera patrimônio consolidado.
8. Transferência deve ser atômica.
9. Soma das parcelas corresponde ao valor total da compra.
10. Parcela pertence a uma única fatura.
11. Fatura pertence a um único cartão.
12. Compra no cartão não reduz saldo bancário imediatamente.
13. Pagamento da fatura reduz saldo da conta.
14. Compra e pagamento da fatura não podem ser contados duas vezes como consumo.
15. Alterações de recorrência não reescrevem histórico.
16. Alterações de cartão não reorganizam silenciosamente faturas antigas.
17. Conta inativa preserva histórico.
18. Categoria inativa preserva histórico.
19. Nenhuma migration deve inventar fatos financeiros.
20. Datas financeiras devem preservar corretamente o dia de calendário.
21. Associar uma conta a um registro legado não confirma sua realização.
22. `ContaId != null` não é evidência de movimentação efetiva.
23. Compras de crédito do modelo legado podem permanecer sem conta bancária durante a transição.
24. Conta inativa não pode ser utilizada em novos lançamentos comuns.
25. Conta inativa deve continuar preservando e podendo representar histórico antigo.
26. A ausência de configuração do banco persistente não pode iniciar silenciosamente uma sessão volátil.
27. Indicadores do modelo legado não podem ser apresentados como saldo bancário confirmado.

---

# 41. Decisões abertas

Devem ser resolvidas somente na fase correspondente:

* entidade explícita ou não para ocorrência recorrente;
* estratégia de versionamento de recorrências;
* representação técnica de transferências;
* pagamento parcial de previsões;
* fechamento manual de obrigação com valor divergente;
* compra no dia exato do fechamento;
* antecipação de parcelas;
* pagamento parcial de fatura;
* juros;
* encargos;
* alteração de limite;
* liberação do limite após pagamento;
* política de arredondamento;
* importação de cartões legados;
* conversão das parcelas antigas;
* conversão ou aposentadoria futura da coluna antiga `Transacao.Data`, preservada na Fase 2.

Não assumir silenciosamente uma dessas regras.

---

# 42. Modelo da Fase 2 — Decisões fechadas e desenho de implementação

## 42.1 Transição da entidade Transacao

A tabela existente será preservada para compatibilidade. Enquanto contiver registros legados, nem toda linha é uma movimentação efetiva. Apenas uma confirmação explícita e elegível estabelece a realização. Isso é uma exceção transitória ao modelo conceitual final, não uma modelagem de previsão dentro de Transacao.

Desenho mínimo recomendado:

```text
EstadoTransacao: NaoReconciliada | Confirmada | Desconsiderada
DataEfetivacao: DateOnly?
ConfirmadaEm: DateTimeOffset?
DesconsideradaEm: DateTimeOffset?
MotivoDesconsideracao: string?
OrigemRegistro: identificação preservada de legado comum, crédito legado ou novo realizado
ClassificacaoPendente: indicação de transferência própria aguardando fase futura, quando identificada pelo usuário
Versao: controle de concorrência
```

Os nomes técnicos podem ser ajustados sem mudar as regras. A origem de crédito não pode ser removida pela simples edição de método. Ao migrar, considerar também os vínculos existentes de parcelamento; inconsistências não autorizam confirmação automática.

Confirmação exige conta e data efetiva explícitas. Crédito legado e transferência própria identificada não são elegíveis. Valor é positivo, com direção dada pelo tipo; data efetiva não pode ser futura. A presença de `ContaId` não define o estado.

Novo realizado exige conta ativa e abertura configurada, com data dentro do acompanhamento. Histórico pode ser confirmado em conta inativa e pode ser anterior à abertura, permanecendo fora do saldo acompanhado. Confirmação repetida não insere outro movimento; conflito de dados deve ser informado.

`Transacao.Data` continua `DateTime` e preserva a data antiga. `DataEfetivacao` é outro campo e não recebe preenchimento automático na migration. Instantes de confirmação/revisão podem usar UTC; datas financeiras usam calendário local. Utilizar referência temporal testável, como `TimeProvider`.

## 42.2 Revisão sem infraestrutura de auditoria complexa

Utilizar `RevisaoTransacao` para histórico simples das correções, guardando referência ao registro, instante, motivo e valores anteriores/posteriores relevantes. Ela não será fonte de reconstrução de saldo e não exige event sourcing.

Os endpoints comuns não alteram campos financeiros, associação ou exclusão de movimentos confirmados. A correção explícita valida as mesmas invariantes e grava mudança/revisão atomicamente. Desconsideração preserva o registro e seu motivo, retirando seu efeito do saldo; não cria um estorno bancário fictício.

## 42.3 Cálculos

Para uma conta com abertura `A` e referência `D >= A`, somar somente movimentos elegíveis com estado Confirmada e `A <= DataEfetivacao <= D`:

```text
Saldo(D) = ValorAbertura + ReceitasConfirmadas(A..D) - DespesasConfirmadas(A..D)
```

Histórico confirmado anterior a A não entra no saldo acompanhado. Conta sem abertura ou referência anterior a A tem saldo indisponível, nunca zero presumido.

Consolidado usa a mesma referência para todas as contas e inclui contas inativas. Deve retornar total calculável, indicação de cobertura parcial e contas excluídas com motivo. O nome apresentado é Saldo calculado.

Extrato é DTO/consulta, com saldo inicial, linhas e saldo final. Ordenar por DataEfetivacao e Id crescentes. Não reconciliados ficam em consulta de revisão separada. Preservar o saldo anterior ao intervalo e, se houver paginação, à página. A linha de abertura não participa dos totais de receita.

## 42.4 Organização técnica

Manter projeto único, EF Core e frontend existente. Introduzir somente dois serviços com regras suficientes:

* `MovimentacaoService`: abertura, realização explícita, confirmação, correção, desconsideração e validações de transição.
* `SaldoService`: saldo por conta, consolidado, extrato e totais realizados necessários ao dashboard.

Preservar o endpoint legado de criação como não reconciliado. Criar operações explícitas para realização e revisão. Acrescentar bloco financeiro aos DTOs de resumo, sem reinterpretar os campos legados.

## 42.5 Limites da fase

Não criar `Previsao`, `PrevisaoId`, `CartaoCredito`, `Fatura`, recorrência ou operação de transferência. Uma saída manual correspondente a fatura antiga não possui vínculo com compras. A classificação de transferência própria somente impede sua confirmação indevida.

Conta permanece sem saldo persistido. Abertura é um fato informado, não um acumulador. A migration é expansiva; não confirma registros, não presume abertura zero e não converte datas antigas.
