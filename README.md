# Controle Financeiro Pessoal

Aplicação financeira pessoal desenvolvida em **ASP.NET Core**, com o objetivo de substituir o uso de planilhas por uma solução local, organizada e simples de utilizar.

O projeto está sendo desenvolvido de forma incremental e atualmente encontra-se em desenvolvimento.

## Tecnologias

### Backend
- C#
- .NET 8
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server

### Frontend
- HTML
- CSS
- JavaScript

### Testes
- xUnit
- Testes de integração
- Playwright

## Funcionalidades atuais

A primeira fase do projeto já está concluída e inclui:

- cadastro de contas financeiras;
- edição, ativação e inativação de contas;
- associação de lançamentos existentes a contas;
- validação de contas ativas em novos lançamentos;
- preservação de dados antigos durante migrations;
- testes automatizados da API, banco de dados e interface.

Atualmente o projeto possui **23 testes automatizados**.

## Roadmap

O desenvolvimento está dividido em fases.

- ✅ Contas e preparação do histórico
- ⬜ Movimentações e saldo por conta
- ⬜ Previsões financeiras
- ⬜ Categorias cadastráveis
- ⬜ Transferências
- ⬜ Recorrências e salário
- ⬜ Cartões de crédito
- ⬜ Faturas e parcelamentos
- ⬜ Visão mensal e planejamento
- ⬜ Refinamento do dashboard e interface

A documentação completa do projeto está disponível em:

ControleFinanceiro.API/docs/

---

## Como executar

### Pré-requisitos

É necessário possuir:

- [.NET 8 SDK](https://dotnet.microsoft.com/)
- SQL Server Express ou SQL Server
- Git

O ambiente utilizado durante o desenvolvimento usa:

```text
localhost\SQLEXPRESS
```

---

### 1. Clone o repositório

```bash
git clone https://github.com/Jpbersagui/API-financeira.git
```

Entre na pasta:

```bash
cd API-financeira
```

---

### 2. Configure o banco

A aplicação utiliza SQL Server.

A connection string padrão está em:

```text
ControleFinanceiro.API/appsettings.json
```

Exemplo:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=ControleFinanceiroDb;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

Altere o servidor ou outras configurações caso seu ambiente seja diferente.

> A aplicação não utiliza banco InMemory como fallback durante o uso normal para evitar perda silenciosa de dados.

---

### 3. Entre no projeto da API

```bash
cd ControleFinanceiro.API
```

---

### 4. Execute a aplicação

```bash
dotnet run
```

Na inicialização, o Entity Framework aplica automaticamente as migrations pendentes ao banco configurado.

A URL utilizada pode variar conforme a configuração de desenvolvimento e será exibida no terminal.

Exemplo:

```text
Now listening on: http://localhost:5068
```

Abra essa URL no navegador.

---

## Executando os testes

A partir da raiz do repositório:

```bash
dotnet restore ControleFinanceiro.API.Tests/ControleFinanceiro.API.Tests.csproj
```

```bash
dotnet build ControleFinanceiro.API.Tests/ControleFinanceiro.API.Tests.csproj --no-restore
```

```bash
dotnet test ControleFinanceiro.API.Tests/ControleFinanceiro.API.Tests.csproj --no-build
```

Os testes relacionais utilizam bancos SQL Server temporários e isolados, evitando alterações no banco financeiro pessoal.

---

## Autor

Desenvolvido por **Jpbersagui**.

GitHub: [@Jpbersagui](https://github.com/Jpbersagui)