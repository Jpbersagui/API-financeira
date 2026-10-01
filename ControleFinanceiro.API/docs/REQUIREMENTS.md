# Controle Financeiro — Requisitos do Sistema

## 1. Objetivo

O Controle Financeiro é uma aplicação financeira pessoal local-first desenvolvida em ASP.NET Core.

Seu objetivo é substituir uma planilha financeira pessoal, mantendo a capacidade de visualizar detalhadamente receitas, despesas, contas, cartões, parcelas, faturas, recorrências, previsões e projeções mensais, porém através de uma interface mais organizada, segura e fácil de utilizar.

A aplicação deve ser adequada para usuários sem conhecimento técnico.

O usuário não deve precisar compreender conceitos internos de programação, banco de dados, chaves estrangeiras, IDs ou arquitetura de software para controlar sua vida financeira.

A aplicação deve priorizar clareza, previsibilidade e facilidade de uso.

---

# 2. Modelo de utilização

A aplicação será executada localmente na máquina do usuário.

Cada instalação deverá utilizar seu próprio banco de dados.

Não assumir que a aplicação ficará executando continuamente.

O usuário pode:

* abrir a aplicação;
* utilizá-la;
* fechá-la;
* permanecer dias ou meses sem executá-la;
* retornar posteriormente.

Nenhuma funcionalidade financeira essencial pode depender de a aplicação estar aberta em determinada data ou horário.

---

# 3. Princípios funcionais fundamentais

## 3.1 Previsão e movimentação realizada são conceitos diferentes

Uma previsão representa algo que se espera que aconteça.

Uma movimentação realizada representa dinheiro que efetivamente entrou ou saiu de uma conta.

Esses dois conceitos não devem ser tratados como o mesmo registro apenas com um booleano do tipo `Pago = true/false`.

Exemplo:

Salário previsto:

R$ 3.000,00

Recebimentos realizados:

* R$ 1.000,00 de adiantamento;
* R$ 2.000,00 de pagamento posterior.

A previsão continua representando o compromisso original de R$ 3.000,00.

As movimentações realizadas representam o dinheiro efetivamente recebido.

---

## 3.2 Uma previsão pode ter várias realizações

Uma previsão pode ser realizada:

* integralmente de uma vez;
* parcialmente;
* através de múltiplos pagamentos ou recebimentos;
* por valor diferente do inicialmente previsto;
* encerrada mesmo com valor final diferente do previsto.

Exemplo:

Energia prevista:

R$ 250,00

Conta final:

R$ 273,18

Pagamento realizado:

R$ 273,18

O sistema deve preservar a previsão original e também o valor efetivamente realizado.

---

## 3.3 Uma movimentação pode existir sem previsão

Nem toda movimentação precisa ter sido planejada.

Exemplo:

Almoço pago no momento da compra.

O usuário pode registrar diretamente uma despesa realizada sem que exista previamente uma previsão.

---

## 3.4 Uma previsão pode existir sem recorrência

Nem toda previsão precisa vir de uma regra recorrente.

Exemplo:

Conserto do carro previsto para o próximo mês.

O usuário deve poder criar uma previsão avulsa.

---

## 3.5 Previsto não altera saldo real

Uma previsão não deve alterar o saldo real de uma conta.

Somente movimentações realizadas alteram saldo real.

---

# 4. Saldo atual e saldo projetado

## 4.1 Saldo atual

Representa a posição financeira efetiva da conta até determinada data.

Deve considerar:

* saldo de abertura;
* receitas realizadas;
* despesas realizadas;
* transferências recebidas;
* transferências enviadas;
* demais entradas e saídas efetivas.

Não deve considerar previsões ainda não realizadas.

---

## 4.2 Saldo projetado

Representa uma estimativa futura.

Deve considerar:

* saldo atual;
* receitas previstas ainda em aberto;
* despesas previstas ainda em aberto;
* faturas relevantes;
* compromissos financeiros futuros.

O saldo projetado deve evitar dupla contabilização.

Exemplo:

Se uma previsão de salário é R$ 3.000,00 e R$ 1.000,00 já foram recebidos, a projeção deve considerar apenas os R$ 2.000,00 restantes.

---

# 5. Automação não deve inventar fatos financeiros

O sistema pode criar automaticamente previsões.

O sistema não deve automaticamente afirmar que determinada receita foi recebida ou determinada despesa foi paga apenas porque chegou sua data prevista.

Exemplo:

Salário esperado no dia 5.

O sistema pode apresentar:

"Salário previsto para recebimento."

O sistema não deve automaticamente adicionar esse valor ao saldo realizado.

---

# 6. Contas financeiras

O usuário deve poder cadastrar contas financeiras.

Exemplos:

* conta corrente;
* poupança;
* carteira;
* outras contas futuramente suportadas.

Cada conta deve possuir inicialmente informações como:

* nome;
* tipo;
* status ativo/inativo;
* marco de início do acompanhamento financeiro.

---

## 6.1 Abertura de conta no sistema

O usuário deve conseguir informar algo como:

"Começar a acompanhar esta conta em 1º de outubro de 2026 com R$ 850,00."

Esse valor representa uma posição financeira de abertura.

Não deve ser tratado como receita.

Movimentações anteriores ao marco de abertura não devem ser contabilizadas novamente no saldo da conta sem tratamento explícito.

---

## 6.2 Conta não é método de pagamento

Conta e método de pagamento são conceitos diferentes.

Exemplo:

Conta:

Nubank

Método:

Pix

Valor:

R$ 80,00

A conta indica onde o dinheiro estava.

O método indica como o pagamento foi realizado.

---

## 6.3 Saldo por conta

O sistema deve permitir visualizar:

* saldo de cada conta;
* saldo consolidado;
* histórico da conta;
* movimentações realizadas;
* previsões relacionadas;
* extrato.

---

# 7. Histórico legado

O projeto já possui dados e estrutura anteriores à nova modelagem.

O sistema não deve inferir automaticamente fatos financeiros que não possam ser comprovados pelos dados atuais.

Não assumir automaticamente que:

* toda transação antiga com data passada foi realizada;
* toda categoria "Salário" representa um recebimento confirmado;
* todo método "Transferência" representa transferência entre contas próprias;
* toda compra no crédito pertence ao mesmo cartão;
* todas as transações pertencem a uma conta inventada.

Registros antigos podem precisar permanecer temporariamente como:

* não revisados;
* sem conta associada;
* pendentes de classificação;
* pertencentes ao modelo legado.

O sistema deve permitir revisão e associação progressiva desses registros.

Nenhuma migration deve inventar informações financeiras silenciosamente.

---

## 7.1 Associação de conta não confirma realização

Durante a transição do modelo antigo para o novo, associar uma conta a um lançamento legado representa apenas organização do histórico.

```text
Associar uma conta
≠
Confirmar que a movimentação aconteceu
```

Um registro antigo pode possuir uma conta associada e ainda assim continuar não reconciliado.

A presença de `ContaId` não pode ser utilizada como evidência de:

* pagamento confirmado;
* recebimento confirmado;
* movimentação realizada;
* impacto real no saldo.

Na Fase 2, a confirmação será uma ação explícita, independente da associação de conta, conforme a seção 38.

---

## 7.2 Regra transitória para cartão de crédito

Enquanto o domínio próprio de cartões ainda não estiver implementado, deve existir uma regra temporária para preservar o funcionamento atual.

### Novos lançamentos fora do crédito

Devem exigir uma conta existente e ativa.

Exemplos:

* Pix;
* débito;
* receita recebida;
* despesa comum;
* dinheiro associado a uma carteira.

### Compras no fluxo legado de cartão de crédito

Podem continuar temporariamente sem `ContaId`.

Uma compra no cartão não representa saída imediata de uma conta bancária.

Portanto, durante essa fase, não deve ser exigida nem presumida uma conta bancária de pagamento para compras no crédito.

O vínculo entre cartão, fatura e conta utilizada para pagamento será implementado posteriormente no domínio próprio de cartões.

### Crédito antigo

Registros antigos de cartão não devem receber automaticamente:

* conta bancária;
* cartão específico;
* conta futura de pagamento de fatura.

Esses dados devem permanecer preservados para classificação futura.

---

# 8. Categorias

O usuário deve possuir categorias financeiras cadastráveis.

Exemplos:

* Alimentação
* Moradia
* Transporte
* Lazer
* Assinaturas
* Saúde
* Financiamentos
* Salário

As categorias não devem depender exclusivamente de valores hardcoded no frontend.

O usuário deve poder:

* cadastrar categoria;
* editar categoria;
* inativar categoria.

Categorias já utilizadas não devem ser apagadas de forma que destrua histórico.

---

# 9. Movimentações realizadas

Uma movimentação realizada representa dinheiro que efetivamente entrou ou saiu de uma conta.

Pode representar:

* receita;
* despesa;
* transferência;
* pagamento de fatura;
* recebimento ligado a uma previsão;
* outras movimentações financeiras efetivas.

Uma movimentação pode possuir informações como:

* descrição;
* valor;
* data;
* conta;
* categoria;
* método de pagamento;
* observações;
* vínculo opcional com previsão;
* vínculo opcional com transferência;
* origem da movimentação.

---

# 10. Previsões

Uma previsão representa uma obrigação ou expectativa financeira.

Pode representar:

* receita futura;
* despesa futura;
* salário;
* conta de energia;
* condomínio;
* financiamento;
* pagamento esperado;
* previsão avulsa.

Uma previsão pode possuir informações como:

* descrição;
* valor previsto;
* valor final conhecido;
* data prevista;
* categoria;
* conta relacionada;
* origem;
* recorrência relacionada;
* status;
* encerramento;
* cancelamento.

---

# 11. Realizações parciais

Uma previsão deve poder possuir zero, uma ou várias movimentações realizadas relacionadas.

Exemplo:

Salário previsto:

R$ 3.000,00

Recebimentos:

* R$ 1.000,00
* R$ 2.000,00

Valor restante:

R$ 0,00

Outro exemplo:

Despesa prevista:

R$ 250,00

Pagamento realizado:

R$ 230,00

Se o usuário informar que esse pagamento encerrou a obrigação, o sistema não deve manter automaticamente uma dívida fictícia de R$ 20,00.

O sistema deve permitir distinguir:

* valor previsto;
* valor realizado;
* valor em aberto;
* obrigação encerrada;
* obrigação cancelada.

---

# 12. Transferências

O usuário deve conseguir transferir dinheiro entre contas próprias.

Exemplo:

Origem:

Conta Corrente

Destino:

Poupança

Valor:

R$ 500,00

Efeito:

* Conta Corrente: -R$ 500,00
* Poupança: +R$ 500,00
* Saldo consolidado: sem alteração

Uma transferência não deve inflar receitas nem despesas consolidadas.

Caso exista tarifa de transferência, ela deve ser registrada como uma despesa separada.

---

# 13. Recorrências

O sistema deve possuir mecanismo para regras financeiras recorrentes.

Exemplos:

* salário;
* condomínio;
* Spotify;
* financiamento;
* seguro;
* aluguel.

Uma recorrência pode possuir informações como:

* descrição;
* natureza;
* valor esperado;
* categoria;
* conta;
* periodicidade;
* dia esperado;
* data inicial;
* data final opcional;
* status ativo/inativo.

A recorrência representa a regra.

A ocorrência de uma recorrência representa determinado período.

Exemplo:

Recorrência:

Spotify

Ocorrência:

Spotify — Setembro/2026

---

# 14. Alterações em recorrências

Alterar uma recorrência não deve reescrever silenciosamente o passado.

Exemplo:

Salário:

R$ 3.000,00 até outubro

Novo salário:

R$ 3.500,00 a partir de novembro

As ocorrências de setembro e outubro devem continuar com os valores históricos correspondentes.

Mudanças devem possuir vigência temporal.

A estratégia técnica pode utilizar versionamento de recorrências ou mecanismo equivalente.

---

# 15. Salário

A interface deve oferecer uma funcionalidade específica chamada "Cadastrar salário".

Internamente, salário pode utilizar o mecanismo de recorrências.

O usuário deve poder informar:

* valor esperado;
* dia esperado de pagamento;
* conta de destino;
* data de início;
* data final opcional;
* status ativo/inativo.

---

## 15.1 Recebimentos parciais do salário

O sistema deve suportar:

* adiantamento;
* pagamento principal;
* complemento;
* ajuste;
* múltiplos recebimentos.

Exemplo:

Salário esperado:

R$ 3.000,00

Adiantamento:

R$ 1.000,00

Pagamento posterior:

R$ 2.000,00

Resultado:

Previsto:

R$ 3.000,00

Recebido:

R$ 3.000,00

Restante:

R$ 0,00

---

# 16. Sincronização temporal

Como a aplicação não ficará executando continuamente, recorrências não podem depender de tarefas executadas em uma data exata.

Quando necessário, o backend deve verificar competências ainda não processadas.

Exemplo:

Última utilização:

Setembro/2026

Nova utilização:

Dezembro/2026

Recorrência mensal existente desde setembro.

O sistema deve conseguir identificar:

* Outubro/2026
* Novembro/2026
* Dezembro/2026

e criar somente as ocorrências ausentes.

Essas ocorrências devem ser criadas como previsões.

Nenhuma ocorrência deve ser automaticamente considerada realizada apenas porque sua data passou.

---

## 16.1 Idempotência

Executar a sincronização várias vezes não pode gerar registros duplicados.

Uma ocorrência já existente para determinada recorrência e competência não deve ser criada novamente.

Cancelamentos também devem ser preservados.

Uma ocorrência cancelada não deve reaparecer após nova sincronização.

---

## 16.2 Recorrências encerradas

A sincronização não deve considerar apenas recorrências atualmente ativas.

Exemplo:

Uma recorrência terminou em novembro, mas a aplicação ficou fechada durante outubro e novembro.

Ao reabrir posteriormente, o sistema deve ainda conseguir identificar ocorrências históricas faltantes dentro da vigência daquela recorrência.

---

# 17. Cartões de crédito

Cartão de crédito deve possuir representação própria.

O usuário deve poder cadastrar múltiplos cartões.

Cada cartão deve possuir pelo menos:

* nome;
* limite;
* dia de fechamento;
* dia de vencimento;
* conta padrão de pagamento;
* status ativo/inativo.

---

# 18. Compra no cartão

Uma compra no cartão representa consumo e compromisso financeiro.

Ela não reduz imediatamente o saldo da conta bancária.

Ao registrar uma compra, o usuário deve selecionar:

* cartão;
* descrição;
* valor;
* data;
* categoria;
* quantidade de parcelas.

O usuário não deve precisar cadastrar manualmente cada parcela futura.

---

# 19. Parcelas

Uma compra parcelada deve gerar parcelas corretamente.

Cada parcela deve possuir pelo menos:

* compra original;
* número da parcela;
* quantidade total;
* valor;
* fatura correspondente.

A soma das parcelas deve corresponder ao valor original da compra, respeitando regras de arredondamento.

---

# 20. Faturas

Cada cartão deve possuir faturas organizadas por ciclos.

Uma fatura deve permitir visualizar:

* cartão;
* competência;
* fechamento;
* vencimento;
* compras;
* parcelas;
* total;
* pagamentos;
* situação.

---

## 20.1 Fechamento e vencimento

A alocação de compras em faturas deve considerar:

* data da compra;
* dia de fechamento;
* dia de vencimento;
* virada de mês;
* virada de ano;
* meses com menos de 31 dias.

Não utilizar somente:

`DataCompra.AddMonths(...)`

como regra definitiva.

Compras exatamente no dia do fechamento podem precisar de política explícita e possibilidade de correção manual.

---

# 21. Pagamento de fatura

A compra no cartão e o pagamento da fatura representam eventos diferentes.

Compra:

representa consumo.

Pagamento da fatura:

representa saída de dinheiro da conta.

O pagamento da fatura deve afetar o saldo da conta utilizada.

---

## 21.1 Não duplicar despesas

O sistema não pode contabilizar compra e pagamento de fatura como duas despesas econômicas independentes.

Exemplo incorreto:

Mercado:

-R$ 500,00

Pagamento da fatura:

-R$ 500,00

Total de despesas:

-R$ 1.000,00

Isso estaria errado para análise de consumo.

---

# 22. Fluxo de caixa e consumo são visões diferentes

O sistema deve distinguir pelo menos duas perspectivas.

## 22.1 Fluxo de caixa

Responde:

"Quando o dinheiro efetivamente entrou ou saiu das minhas contas?"

Exemplo:

Uma compra no cartão feita em setembro pode gerar saída da conta somente em outubro, quando a fatura for paga.

---

## 22.2 Consumo por categoria

Responde:

"Quando e com o que eu gastei?"

Exemplo:

Compra de mercado feita em setembro deve aparecer como consumo de alimentação em setembro, mesmo que a fatura seja paga em outubro.

---

# 23. Limite do cartão

Limite disponível não deve ser confundido com saldo bancário.

O cálculo do limite deve considerar os compromissos do cartão e as regras de liberação após pagamentos.

A política definitiva será definida na fase correspondente.

---

# 24. Datas financeiras

Datas como:

* vencimento;
* competência;
* data de compra;
* data de pagamento;
* data de recebimento;
* fechamento;

devem ser tratadas como datas de calendário local quando não houver necessidade real de horário.

Evitar conversões desnecessárias para UTC que possam deslocar o dia visualizado.

Quando apropriado no .NET, considerar tipos como `DateOnly`.

---

# 25. Visualização mensal

O sistema deve preservar a capacidade da planilha de permitir leitura financeira detalhada por mês.

O usuário deve poder selecionar um mês.

Exemplo:

* Setembro 2026
* Outubro 2026
* Novembro 2026

---

## 25.1 Estrutura conceitual

Uma possível visão mensal:

* Resumo
* Contas
* Cartões
* Previstos
* Realizados
* Poupança

A estrutura visual pode evoluir durante o desenvolvimento.

---

# 26. Extrato

Extrato é uma consulta, não uma entidade obrigatoriamente persistida.

Uma visão pode mostrar:

| Data  | Descrição  |  Entrada |  Saída |    Saldo |
| ----- | ---------- | -------: | -----: | -------: |
| 05/09 | Salário    | R$ 3.000 |        | R$ 7.200 |
| 07/09 | Mercado    |          | R$ 320 | R$ 6.880 |
| 10/09 | Condomínio |          | R$ 800 | R$ 6.080 |

O backend deve fornecer saldo de abertura e saldo acumulado corretamente.

---

# 27. Resumo mensal

Para determinado período, o backend deve conseguir fornecer:

* saldo inicial;
* entradas realizadas;
* saídas realizadas;
* saldo atual;
* saldo ao fim do período;
* receitas previstas;
* despesas previstas;
* pendências vencidas;
* compromissos futuros;
* saldo projetado;
* faturas;
* consumo por categoria.

O frontend não deve reconstruir sozinho regras financeiras complexas.

---

# 28. Meses antigos

Ao consultar um mês passado, o sistema deve distinguir corretamente:

* saldo naquela época;
* saldo ao final daquele mês;
* saldo atual de hoje.

Essas informações não devem ser apresentadas como se fossem equivalentes.

---

# 29. Dashboard

O dashboard deve priorizar informação resumida.

Exemplos:

* saldo atual por conta;
* saldo consolidado;
* saldo projetado;
* receitas realizadas;
* despesas realizadas;
* receitas previstas;
* despesas previstas;
* pendências vencidas;
* próxima fatura;
* compromissos futuros.

Princípio de interface:

**Resumo primeiro, detalhe sob demanda.**

---

## 29.1 Dashboard durante a transição

Enquanto o novo modelo de movimentações realizadas ainda não estiver implementado, os cálculos antigos do dashboard podem permanecer temporariamente para evitar quebra desnecessária da aplicação.

Entretanto, eles não devem ser apresentados como saldo bancário confirmado.

Durante essa transição, utilizar conceitos como:

* Resultado dos lançamentos;
* Receitas cadastradas;
* Despesas cadastradas.

Evitar temporariamente rótulos como:

* Saldo da Conta;
* Recebido no Mês;
* Gasto no Mês;

quando esses valores incluírem registros cuja realização ainda não foi confirmada.

A interface deve informar claramente algo equivalente a:

> Resumo dos lançamentos cadastrados. Estes valores ainda não representam saldo bancário confirmado.

Associar uma conta a registros antigos não transforma automaticamente os indicadores antigos em saldo financeiro por conta.

---

# 30. Navegação conceitual

Uma possível estrutura:

* Dashboard
* Movimentações
* Meses
* Contas
* Cartões
* Recorrências
* Planejamento

A navegação poderá evoluir.

---

# 31. Persistência versus cálculo

Preferir persistir fatos e compromissos.

Exemplos:

* contas;
* abertura de contas;
* transações realizadas;
* previsões;
* recorrências;
* ocorrências;
* compras;
* parcelas;
* faturas;
* pagamentos.

Preferir calcular sob demanda:

* saldo atual;
* saldo projetado;
* totais mensais;
* valores em aberto;
* resumo mensal;
* indicadores.

---

# 32. Interface simples

A interface deve utilizar termos compreensíveis.

Não mostrar diretamente:

* IDs;
* chaves estrangeiras;
* nomes internos de enums;
* nomes de tabelas.

Exemplo:

Não mostrar:

`ContaId = 3`

Mostrar:

`Conta: Nubank`

---

# 33. Interface mínima por fase

A interface não deve ser deixada completamente para o final do projeto.

Cada nova funcionalidade implementada deve possuir uma interface mínima utilizável quando isso fizer sentido.

Exemplos:

Ao implementar contas:

* permitir cadastrar conta;
* listar contas;
* selecionar conta.

Ao implementar cartões:

* permitir cadastrar cartão;
* visualizar cartões;
* selecionar cartão.

A fase final de frontend servirá para consolidar e refinar a experiência, e não para criar toda a usabilidade de uma vez.

---

# 34. Segurança e consistência de dados

Operações financeiras relacionadas devem ser atômicas quando necessário.

Exemplo:

Transferência entre contas não pode salvar somente a saída e falhar antes de salvar a entrada.

Parcelamentos também não devem permanecer parcialmente criados em caso de falha intermediária.

---

# 35. Preservação do projeto atual

O sistema já possui código funcional.

A evolução deve:

* reaproveitar funcionalidades existentes sempre que possível;
* evitar reescrita completa;
* preservar migrations antigas;
* evitar alterações fora do escopo atual;
* migrar dados de forma progressiva;
* evitar inferências financeiras silenciosas;
* manter histórico legado acessível.

---

# 36. Banco local persistente

A aplicação deve utilizar banco persistente em seu funcionamento normal.

Não utilizar fallback silencioso para banco em memória quando a configuração do banco persistente estiver ausente.

Se a conexão necessária não estiver configurada corretamente, a aplicação deve interromper a inicialização normal e fornecer uma mensagem clara de orientação.

Não é aceitável iniciar uma sessão aparentemente funcional que perca todos os dados quando a aplicação for fechada.

Banco InMemory pode continuar sendo utilizado explicitamente em testes quando apropriado.

---

# 37. Fora do escopo inicial

Não são prioridades neste momento:

* microserviços;
* mensageria;
* execução em nuvem;
* workers independentes;
* Hangfire;
* Quartz;
* RabbitMQ;
* autenticação complexa;
* múltiplos usuários;
* sincronização em nuvem.

Esses recursos somente devem ser adicionados futuramente se houver necessidade concreta.

---

# 38. Fase 2 — Decisões fechadas

Estas regras definem o escopo de movimentações efetivas, abertura e saldo por conta. As descrições do domínio completo nas demais seções não antecipam funcionalidades de fases futuras.

## 38.1 Abertura

Representar a abertura por `DataAbertura` e `ValorAbertura` na própria Conta. Os campos devem estar preenchidos juntos ou ambos ausentes.

A abertura é o saldo no início do dia informado, antes das movimentações daquele dia. Participa do saldo, mas não é receita. O valor pode ser positivo, zero ou negativo. Não permitir abertura futura.

Conta sem abertura não possui saldo calculável. Exibir orientação para definir a abertura; não presumir zero. Para uma referência anterior à abertura, informar que o período não era acompanhado, sem inventar saldo.

A correção da abertura deve ser uma ação específica, separada da edição de nome, tipo ou status. Alterar a data não pode excluir silenciosamente movimentos confirmados do período acompanhado: bloquear a operação com informação dos registros afetados até revisão explícita. Corrigir a abertura recalcula os saldos sob demanda, sem criar receita ou despesa compensatória automática.

## 38.2 Registro realizado e compatibilidade

Novos movimentos comuns devem utilizar fluxo explícito de pagamento ou recebimento realizado. Exigir conta ativa com abertura, valor positivo e data efetiva não futura dentro do período acompanhado.

O endpoint legado de criação continua temporariamente como cadastro não reconciliado. Não mudar silenciosamente seu significado financeiro. Associação de conta continua sendo apenas organização.

Registros antigos podem ser confirmados manualmente, com conta e data de efetivação explícitas. A confirmação repetida não pode duplicar o registro ou seu efeito. Histórico confirmado anterior à abertura é preservado, mas não participa do saldo acompanhado.

Não utilizar `ContaId` como evidência de realização. Não utilizar o estado não reconciliado como substituto de previsão ou obrigação pendente.

## 38.3 Revisão e correção

Registro antigo incorreto deve ser desconsiderado com motivo, preservando seus dados em vez de ser excluído fisicamente.

Movimento confirmado não pode ser alterado silenciosamente por edição, associação ou exclusão comuns. Deve existir ação específica de correção. Pode ser utilizado um histórico simples de revisões, sem infraestrutura complexa de auditoria ou event sourcing.

Desconsiderar ou corrigir um registro ajusta a representação no sistema; não significa que ocorreu devolução de dinheiro. Uma devolução efetiva é outra movimentação.

## 38.4 Contas inativas

Não aceitam novos movimentos comuns. Continuam permitindo saldo e extrato, participam do consolidado e permitem associação, confirmação e correção de histórico antigo. Inativar não zera saldo nem apaga vínculos.

## 38.5 Crédito e transferências próprias

Crédito legado permanece fora do saldo e do extrato bancário realizado. Preservar sua origem para impedir que mudar o método de pagamento permita confirmar uma compra antiga como movimentação bancária.

Se uma fatura antiga foi paga após a abertura, o usuário pode registrar manualmente a saída como movimento comum. Não criar vínculo com compras, parcelas ou faturas nesta fase. Não promover os totais legados de consumo a indicadores financeiros confirmados.

Registros antigos identificados pelo usuário como transferência entre contas próprias devem permanecer para classificação futura, sem confirmação como receita ou despesa. O método `Transferencia` sozinho não identifica transferência própria. A indicação para classificação futura não cria a entidade ou a operação financeira de transferência.

## 38.6 Saldo e consolidado

Para referência igual ou posterior à abertura:

```text
Saldo calculado = ValorAbertura
               + receitas confirmadas desde a abertura até a referência
               - despesas confirmadas desde a abertura até a referência
```

Incluir movimentos do próprio dia de abertura. Excluir não reconciliados, desconsiderados, crédito legado, transferências próprias aguardando classificação e histórico anterior à abertura.

O consolidado soma as contas calculáveis na mesma referência, inclusive inativas. Quando uma conta não tiver abertura ou a referência anteceder sua abertura, listar a conta e o motivo de exclusão e indicar que o total é parcial. Informar também pendências de revisão conhecidas; confirmação manual não garante ausência de movimentos esquecidos.

Usar o termo **Saldo calculado** na interface. Não persistir o saldo atual como campo mutável.

## 38.7 Extrato e dashboard

Extrato por conta deve conter saldo inicial, movimentos confirmados e saldo final. Ordenação: `DataEfetivacao` crescente, depois `Id` crescente. O desempate não representa horário bancário conhecido.

Filtros de início e fim são inclusivos. O saldo inicial considera abertura e movimentos anteriores ao intervalo dentro do acompanhamento. A abertura pode ser exibida como linha informativa, sem receita. Se o intervalo começar antes da abertura, indicar o trecho não acompanhado; se estiver inteiramente antes, o saldo é indisponível. Não projetar saldos futuros nesta fase. Se houver paginação, considerar movimentos anteriores à página.

Não reconciliados aparecem separadamente para revisão. No dashboard, distinguir saldo atual, posição no período selecionado e movimentos confirmados do período. Indicadores antigos permanecem explicitamente legados, separados dos novos indicadores calculados; não somar ambos nem mudar silenciosamente a semântica dos campos existentes.

## 38.8 Datas, migration e limites

Utilizar `DateOnly` para `DataAbertura`, `DataEfetivacao`, referência de saldo e filtros do extrato. Preservar `Transacao.Data` como `DateTime`, sem converter a coluna antiga ou inferir automaticamente a efetivação.

A migration deve ser expansiva, preservar dados e migrations antigas, manter abertura não informada nas contas existentes e todos os registros existentes não reconciliados. Validar em banco isolado antes de qualquer aplicação ao banco pessoal.

Não implementar previsões, domínio de cartões, faturas, novas regras de parcelas, recorrências ou transferências entre contas próprias. O salário automático continua suspenso. Nada exige execução contínua da aplicação.
