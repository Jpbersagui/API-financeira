# AGENTS.md

## Objetivo deste arquivo

Este arquivo orienta agentes de IA que trabalham neste repositório.

Use-o como um mapa do projeto e como conjunto de regras permanentes de trabalho.

Ele NÃO substitui a análise necessária para mudanças de arquitetura ou regras de negócio. Em tarefas amplas, leia também a documentação principal e os arquivos necessários.

Evite ler o repositório inteiro por padrão em tarefas de implementação. Comece pela área afetada, siga as dependências relevantes e amplie a leitura somente quando necessário.

---

## Projeto

Aplicação pessoal de controle financeiro construída com:

- .NET 8
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- HTML, CSS e JavaScript no frontend
- testes automatizados no projeto `ControleFinanceiro.API.Tests`

O sistema é desenvolvido incrementalmente por fases.

A fonte de verdade sobre requisitos, domínio e andamento é a documentação em `ControleFinanceiro.API/docs/`.

---

## Documentação principal

Antes de alterações relevantes de domínio, arquitetura ou comportamento financeiro, consulte:

- `ControleFinanceiro.API/docs/REQUIREMENTS.md`
- `ControleFinanceiro.API/docs/DOMAIN_MODEL.md`
- `ControleFinanceiro.API/docs/ROADMAP.md`

Responsabilidades:

- `REQUIREMENTS.md`: o que o sistema deve fazer.
- `DOMAIN_MODEL.md`: regras, conceitos, entidades e relações do domínio.
- `ROADMAP.md`: fases concluídas, fase atual e funcionalidades futuras.

Não duplique regras extensas deste domínio neste arquivo. Se houver divergência, a documentação acima prevalece.

---

## Estado atual

As Fases 1, 2 e 3 foram implementadas e validadas.

A próxima etapa planejada é a Fase 4, conforme `ROADMAP.md`.

Antes de implementar uma nova fase:

1. fazer análise separada;
2. fechar decisões de negócio;
3. atualizar a documentação;
4. somente depois iniciar a implementação.

Não antecipar funcionalidades de fases posteriores.

---

## Mapa do projeto

### Contas

Principais arquivos:

- `ControleFinanceiro.API/Models/Conta.cs`
- `ControleFinanceiro.API/Controllers/ContasController.cs`
- `ControleFinanceiro.API/Services/SaldoService.cs`
- `ControleFinanceiro.API/DTOs/`
- `ControleFinanceiro.API/wwwroot/js/contas.js`

Responsabilidades principais:

- cadastro e manutenção de contas;
- ativação/inativação;
- saldo inicial;
- saldo calculado;
- extrato por conta;
- saldo consolidado.

---

### Movimentações financeiras

Principais arquivos:

- `ControleFinanceiro.API/Models/Transacao.cs`
- `ControleFinanceiro.API/Models/RevisaoTransacao.cs`
- `ControleFinanceiro.API/Controllers/TransacoesController.cs`
- `ControleFinanceiro.API/Services/MovimentacaoService.cs`
- `ControleFinanceiro.API/Services/SaldoService.cs`
- `ControleFinanceiro.API/DTOs/`
- `ControleFinanceiro.API/wwwroot/js/movimentacoes.js`

Responsabilidades principais:

- pagamentos e recebimentos realizados;
- confirmação de registros antigos;
- desconsideração;
- correção de movimentações confirmadas;
- histórico simples de revisão;
- vínculo opcional com previsões.

Regra fundamental:

Somente movimentações confirmadas e financeiramente elegíveis alteram o saldo.

---

### Previsões

Principais arquivos:

- `ControleFinanceiro.API/Models/Previsao.cs`
- `ControleFinanceiro.API/Models/RevisaoPrevisao.cs`
- `ControleFinanceiro.API/Controllers/PrevisoesController.cs`
- `ControleFinanceiro.API/Services/PrevisaoService.cs`
- `ControleFinanceiro.API/DTOs/`
- `ControleFinanceiro.API/wwwroot/js/previsoes.js`

Responsabilidades principais:

- previsões avulsas de receita e despesa;
- valor previsto original;
- valor final opcional;
- realizações parciais, integrais e excedentes;
- vínculo e desvinculação com movimentações;
- encerramento;
- cancelamento;
- reabertura;
- histórico simples;
- vencimento e situação calculados.

Regras fundamentais:

- `Previsao` e `Transacao` são conceitos separados.
- Previsões não alteram saldo.
- Uma transação pode estar vinculada a no máximo uma previsão na estrutura atual.
- Valores realizados e restantes devem ser derivados das movimentações, não usados como fonte de verdade persistida.

---

### Dashboard

Principais arquivos:

- `ControleFinanceiro.API/Controllers/DashboardController.cs`
- `ControleFinanceiro.API/DTOs/DashboardDto.cs`
- `ControleFinanceiro.API/wwwroot/js/app.js`
- `ControleFinanceiro.API/wwwroot/index.html`

O dashboard deve manter separados:

- dinheiro realizado / saldo calculado;
- previsões;
- métricas do cadastro antigo.

Não misturar previsão com saldo disponível.

---

### Persistência e banco

Principais arquivos:

- `ControleFinanceiro.API/Data/AppDbContext.cs`
- `ControleFinanceiro.API/Data/AppDbContextFactory.cs`
- `ControleFinanceiro.API/Migrations/`

Regras:

- Nunca editar migrations antigas.
- Mudanças de modelo devem gerar uma nova migration.
- Migrations devem preservar os dados existentes sempre que possível.
- Não interpretar ou confirmar registros antigos automaticamente sem regra explícita.
- Não aplicar migrations ao banco pessoal durante implementação ou testes.
- Validar migrations em banco isolado antes de usar no banco pessoal.

Migrations importantes já entregues incluem:

- Fase 2: `20260930202014_AdicionarAberturaEConfirmacao`
- Fase 3: `20261002174011_AdicionarPrevisoesERealizacoes`

Consulte a pasta `Migrations/` para a sequência completa.

---

### Frontend

Principais arquivos:

- `ControleFinanceiro.API/wwwroot/index.html`
- `ControleFinanceiro.API/wwwroot/css/style.css`
- `ControleFinanceiro.API/wwwroot/js/app.js`
- `ControleFinanceiro.API/wwwroot/js/contas.js`
- `ControleFinanceiro.API/wwwroot/js/movimentacoes.js`
- `ControleFinanceiro.API/wwwroot/js/previsoes.js`

A interface atual é funcional e serve também para validação incremental do projeto.

Ela NÃO deve ser tratada como o design final da aplicação.

Direção futura esperada:

- navegação por áreas/telas;
- menos informações simultâneas;
- ações contextuais;
- formulários dedicados, painéis ou modais quando apropriado;
- dashboard principal mais enxuto.

Durante as fases de domínio, priorize clareza, acesso às funcionalidades e testabilidade.

Não faça um redesign completo sem solicitação específica ou sem chegar à etapa planejada de refinamento de UI/UX.

---

### Testes

Projeto:

- `ControleFinanceiro.API.Tests/`

Consulte também:

- `ControleFinanceiro.API.Tests/README.md`

Regras:

- testes de integração devem usar bancos isolados;
- nunca utilizar o banco pessoal nos testes;
- preservar regressões das fases anteriores;
- alterações financeiras devem testar sucesso e falha;
- mudanças com concorrência devem validar conflitos;
- mudanças de modelo devem validar migration;
- mudanças de interface devem atualizar/adicionar testes de interface quando necessário.

---

## Regras permanentes de domínio

Estas regras não devem ser alteradas silenciosamente:

1. Abertura de conta não é receita.
2. Conta sem abertura não deve assumir saldo zero.
3. Saldo é calculado; não persistir `SaldoAtual` como fonte de verdade.
4. Apenas movimentações confirmadas elegíveis alteram saldo.
5. Registros antigos não são confirmados apenas porque possuem `ContaId`.
6. Crédito legado não deve ser transformado automaticamente em saída bancária.
7. Previsões não alteram saldo.
8. Realizações de previsões são movimentações financeiras reais.
9. Desconsideração preserva histórico; não representa devolução de dinheiro.
10. Correções de fatos financeiros confirmados devem usar fluxos protegidos.
11. Contas inativas preservam histórico e saldo.
12. Não antecipar domínio de fases futuras sem aprovação.

Para detalhes e exceções, consulte `DOMAIN_MODEL.md`.

---

## Estratégia de leitura para tarefas de implementação

Para uma tarefa de implementação específica, siga esta ordem:

1. Leia este `AGENTS.md`.
2. Leia a seção relevante de `ROADMAP.md`.
3. Consulte `REQUIREMENTS.md` e `DOMAIN_MODEL.md` somente nas partes relacionadas à tarefa.
4. Abra os arquivos indicados no mapa da área afetada.
5. Siga dependências diretas quando necessário.
6. Consulte testes existentes da mesma funcionalidade.
7. Amplie para outras áreas somente se houver dependência real ou risco de regressão.

Não faça uma varredura completa do repositório automaticamente para uma alteração localizada.

Para mudanças de arquitetura, nova fase ou análise de conflitos, a leitura pode e deve ser mais ampla.

---

## Antes de modificar código

Antes da implementação:

- identifique claramente o escopo;
- liste os arquivos provavelmente afetados;
- verifique se existe regra relevante nos documentos;
- verifique se a funcionalidade pertence à fase atual;
- reutilize serviços e regras existentes quando possível;
- evite duplicar validações financeiras;
- evite abstrações sem necessidade demonstrada.

Não criar por iniciativa própria:

- novos frameworks;
- repositórios genéricos;
- event sourcing;
- workers;
- schedulers;
- filas;
- microservices;
- grandes refatorações arquiteturais.

Se algo desse tipo for realmente necessário, explique primeiro o motivo e o impacto.

---

## Durante a implementação

Prefira alterações pequenas e localizadas.

Preserve contratos existentes sempre que possível.

Ao encontrar comportamento inesperado:

1. investigue a dependência relevante;
2. confirme se o comportamento é regra existente ou bug;
3. não altere regra de negócio apenas para fazer um teste passar.

Se a tarefa exigir mudança fora do escopo originalmente previsto, pare e informe antes de expandir significativamente a implementação.

---

## Validação

Após mudanças relevantes:

### Código

- executar build em Release;
- verificar zero erros;
- tratar avisos novos relevantes.

### Testes

- executar testes diretamente relacionados;
- executar toda a suíte quando houver risco de regressão entre áreas;
- preservar testes das fases anteriores.

### Entity Framework

Quando houver mudança de modelo:

- criar nova migration;
- validar a sequência desde migrations anteriores em banco isolado;
- verificar se existe alteração pendente no modelo;
- não aplicar ao banco pessoal durante a implementação.

### Interface

Quando houver mudança visual ou de fluxo:

- validar pelo navegador;
- testar os caminhos principais;
- atualizar testes de interface quando necessário;
- verificar pelo menos desktop e uma largura pequena quando o layout for afetado.

---

## Implementação de novas fases

A análise de uma nova fase pode investigar o projeto amplamente.

Depois que a análise estiver aprovada, prefira registrar um plano de implementação específico da fase em um arquivo separado, por exemplo:

`ControleFinanceiro.API/docs/PHASE_4_IMPLEMENTATION.md`

Esse arquivo deve conter somente o que for necessário para executar a fase:

- escopo aprovado;
- regras fechadas;
- arquivos que provavelmente serão alterados;
- arquivos novos;
- dependências relevantes;
- migration prevista;
- testes previstos;
- itens explicitamente fora de escopo.

Durante a implementação, use:

1. `AGENTS.md`;
2. o plano específico da fase;
3. somente os arquivos necessários.

O plano específico deve ser removido ou arquivado quando deixar de ser útil, para não virar uma segunda fonte de verdade concorrente com `ROADMAP.md`, `REQUIREMENTS.md` e `DOMAIN_MODEL.md`.

---

## Princípio geral

Use a menor quantidade de contexto necessária para fazer a alteração corretamente.

Economizar leitura é desejável.

Perder uma regra de negócio, quebrar compatibilidade ou esconder um risco para economizar contexto não é aceitável.
