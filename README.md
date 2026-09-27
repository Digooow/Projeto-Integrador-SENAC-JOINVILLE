# Sistema da Cantina - Cardápio Antidesperdício

Aplicação desktop para apoiar a operação de cantinas e pequenos comércios, com foco na redução do desperdício de alimentos por meio de controle de produtos, estoque, descontos e divulgação de ofertas.

> **Status atual:** protótipo funcional acadêmico em evolução. O build atual é executável, mas o sistema ainda não está pronto para uso multiusuário ou produção.

<p>
  <img src="https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white" alt=".NET 10" />
  <img src="https://img.shields.io/badge/WPF-Windows-0078D4?logo=windows&logoColor=white" alt="WPF" />
  <img src="https://img.shields.io/badge/EF%20Core-9-512BD4" alt="Entity Framework Core 9" />
  <img src="https://img.shields.io/badge/status-protótipo%20funcional-yellow" alt="Protótipo funcional" />
  <img src="https://img.shields.io/badge/testes-a%20implementar-red" alt="Testes a implementar" />
</p>

## Sumário

- [Visão geral](#visão-geral)
- [Estado atual](#estado-atual)
- [Funcionalidades](#funcionalidades)
- [Stack](#stack)
- [Arquitetura atual](#arquitetura-atual)
- [Como executar](#como-executar)
- [Estrutura do repositório](#estrutura-do-repositório)
- [Melhorias necessárias para um projeto profissional](#melhorias-necessárias-para-um-projeto-profissional)
- [Roadmap recomendado](#roadmap-recomendado)
- [Equipe e contexto acadêmico](#equipe-e-contexto-acadêmico)
- [Licença](#licença)

## Visão geral

O problema de negócio é o desperdício de produtos que permanecem sem venda ao final do dia. A proposta é permitir que a equipe da cantina acompanhe o estoque, aplique descontos e divulgue os itens promocionais aos alunos e funcionários.

O público inicial é a cantina ALAMO do SENAC, mas o domínio pode ser reutilizado por cantinas, lanchonetes, padarias e pequenos mercados.

## Estado atual

### O que funciona hoje

- Aplicação desktop Windows compilável em `net10.0-windows`.
- Cadastro, edição, busca e exclusão de produtos.
- Controle de quantidade em estoque.
- Desconto percentual ou em valor fixo.
- Cálculo de preço promocional e total.
- Cadastro e exclusão de alunos por meio de EF Core.
- Migração inicial para MariaDB/MySQL.
- Abertura do WhatsApp Web com uma mensagem promocional pré-preenchida.
- Separação inicial entre Views, ViewModels, Services, Models e Data.

### Evidências da análise

| Item | Resultado |
|---|---|
| Build | `dotnet build --no-restore` concluído com sucesso |
| Erros de compilação | 0 |
| Warnings | 6 |
| Testes automatizados | Não há projeto de testes no repositório |
| Persistência de produtos | Arquivo `produtos.json` no diretório da aplicação |
| Persistência de alunos | MariaDB/MySQL via EF Core |
| Integração WhatsApp | Link `wa.me` com mensagem pré-preenchida; não é envio automático |
| Plataforma | Windows; WPF não é multiplataforma |

### Pontos de atenção atuais

- A persistência está dividida: produtos usam JSON e alunos usam banco de dados. Isso pode gerar dados inconsistentes e impede transações entre estoque, vendas e promoções.
- A string de conexão está hardcoded em `App.xaml.cs` e na fábrica de design-time. Credenciais devem vir de configuração segura.
- Não existe histórico de movimentação, venda, usuário, auditoria ou fechamento diário.
- O botão do WhatsApp abre uma URL; a aplicação ainda não possui integração oficial nem disparo agendado.
- Não há autenticação, autorização, backup, tratamento centralizado de erros ou telemetria.
- O container de DI possui registros duplicados e alguns serviços são instanciados diretamente pelos ViewModels.
- O projeto compila com warnings de nullable e `using` duplicado.
- Não há testes unitários, testes de integração, pipeline de CI/CD ou pacote de distribuição versionado.

## Funcionalidades

### Disponíveis no protótipo

- CRUD de produtos.
- Busca de produtos por nome.
- Ajustes rápidos de estoque (+1, +5, +10 e redução).
- Aplicação de desconto em produto selecionado ou em todos os produtos.
- Remoção de descontos.
- CRUD parcial de alunos (criação, listagem e exclusão).
- Divulgação manual de produtos com desconto pelo WhatsApp Web.

### Ainda não implementadas

- Cardápio público para alunos.
- Notificação automática no horário de encerramento.
- Envio por WhatsApp Business API.
- Registro de vendas e histórico de movimentações.
- Relatórios e indicadores.
- Login, perfis e permissões.
- Checkout e pagamento online.

## Stack

### Stack existente

| Tecnologia | Uso |
|---|---|
| C# / .NET 10 | Linguagem e runtime |
| WPF / XAML | Interface desktop Windows |
| MVVM | Separação entre interface e estado/apresentação |
| Microsoft.Extensions.DependencyInjection | Injeção de dependências inicial |
| Entity Framework Core 9 | ORM usado no módulo de alunos e migrações |
| Pomelo.EntityFrameworkCore.MySql 9 | Provedor MariaDB/MySQL |
| System.Text.Json | Persistência atual de produtos |
| Git/GitHub | Versionamento e colaboração |

### Stack recomendada para evolução profissional

| Área | Recomendação |
|---|---|
| Cliente | Manter WPF para operação interna Windows; considerar Blazor ou React para o cardápio público |
| API | ASP.NET Core Web API, com contratos versionados e OpenAPI |
| Domínio | Camada de aplicação/domínio separada da UI e da infraestrutura |
| Banco | MariaDB ou PostgreSQL como fonte única de verdade |
| Persistência | EF Core com migrations, transações e concorrência otimista |
| Mensageria | WhatsApp Business Cloud API ou provedor oficial, com webhook e fila |
| Jobs | Worker Service/Hangfire para fechamento diário e notificações |
| Validação | FluentValidation e validação de domínio |
| Observabilidade | Serilog, métricas, health checks e rastreamento de erros |
| Segurança | ASP.NET Core Identity/JWT, secrets fora do Git, HTTPS e princípio do menor privilégio |
| Testes | xUnit, FluentAssertions, testes de integração e testes de UI críticos |
| Qualidade | `dotnet format`, analyzers, nullable habilitado e cobertura no CI |
| Entrega | GitHub Actions, publicação self-contained/MSIX para desktop e deploy automatizado da API |
| Infraestrutura | Docker para API/banco em ambientes de desenvolvimento e homologação |

## Arquitetura atual

O projeto usa uma organização inspirada em MVVM:

```text
Views/         Janelas WPF e XAML
ViewModels/    Estado da tela e comandos
Services/      Regras de estoque, descontos, produtos e alunos
Models/        Entidades Produto e Aluno
Data/          DbContext e fábrica para migrations
Migrations/    Migrações do EF Core
Converters/    Conversores para a UI
```

O desenho atual é adequado para um protótipo, mas ainda não é uma arquitetura profissional completa. A evolução recomendada é:

```text
Client.Wpf       Interface operacional
Client.Web       Cardápio público (futuro)
Api              HTTP, autenticação e contratos
Application      Casos de uso e DTOs
Domain           Entidades, regras e eventos
Infrastructure   EF Core, integrações e persistência
Worker           Tarefas agendadas e notificações
Tests             Unitários, integração e arquitetura
```

## Como executar

### Pré-requisitos

- Windows 10 ou superior.
- .NET SDK 10.
- Visual Studio 2022 com a carga **Desenvolvimento para desktop .NET**, ou VS Code com extensão C#.
- MariaDB/MySQL local para os recursos de alunos.

### Build

```powershell
dotnet restore
dotnet build
```

### Banco de dados

1. Crie um banco chamado `ProjetoIntegradorSENAC`.
2. Configure a conexão fora do código-fonte. Atualmente, o protótipo ainda possui uma conexão padrão em `App.xaml.cs` e `Data/DesignTimeDbContextFactory.cs`; substitua esse mecanismo antes de publicar.
3. Instale a ferramenta do EF Core, se necessário:

```powershell
dotnet tool install --global dotnet-ef
```

4. Aplique as migrations:

```powershell
dotnet ef database update
```

> Os produtos são carregados e salvos em `produtos.json` no diretório de execução. Essa solução é provisória e deve ser substituída por persistência transacional no banco.

### Execução

```powershell
dotnet run
```

## Estrutura do repositório

```text
Projeto-Integrador-SENAC/
├── Converters/
├── Data/
├── Migrations/
├── Models/
├── Services/
├── ViewModels/
├── Views/
├── App.xaml
├── App.xaml.cs
├── Projeto-Integrador-SENAC.csproj
└── README.md
```

Arquivos de build (`bin/`, `obj/`), cache do Visual Studio (`.vs/`) e configurações específicas do usuário são artefatos locais e não fazem parte da entrega da aplicação.

## Melhorias necessárias para um projeto profissional

### Prioridade 1 - consistência e segurança

1. Migrar produtos para o mesmo banco dos alunos e remover o `Storage` baseado em JSON.
2. Mover a conexão para `appsettings`, variável de ambiente ou secret manager; nunca versionar credenciais.
3. Corrigir os registros duplicados de DI e injetar serviços por construtor.
4. Centralizar tratamento de exceções, mensagens ao usuário e logging.
5. Corrigir todos os warnings do compilador e habilitar analyzers.

### Prioridade 2 - domínio operacional

1. Criar entidades de `MovimentacaoEstoque`, `Venda`, `Oferta`, `Usuario` e `FechamentoDiario`.
2. Registrar cada entrada, saída, ajuste e venda com data, responsável e motivo.
3. Usar transações e controle de concorrência para evitar estoque negativo ou perda de atualização.
4. Implementar regras de fechamento, validade, preço promocional e disponibilidade.
5. Criar uma API para desacoplar a operação da divulgação pública.

### Prioridade 3 - produto e operação

1. Substituir a abertura manual do WhatsApp por integração oficial.
2. Implementar agendamento, fila, retry e idempotência para notificações.
3. Adicionar autenticação, perfis (administrador, operador e consulta) e auditoria.
4. Criar relatórios de desperdício, vendas, estoque baixo e conversão das ofertas.
5. Criar backup, restauração e política de retenção de dados.

### Prioridade 4 - engenharia de software

1. Criar projetos separados para produção, testes unitários e integração.
2. Adicionar CI com restore, build, testes, análise estática e publicação de artefatos.
3. Cobrir regras de desconto, estoque, persistência e notificações com testes automatizados.
4. Adicionar testes de fumaça para os fluxos críticos da interface.
5. Definir versionamento semântico, changelog e processo de release.

## Roadmap recomendado

- [ ] **Fase 1 - fundação:** corrigir warnings, DI, configuração e logging.
- [ ] **Fase 2 - persistência:** consolidar produtos e alunos no banco, com migrations e transações.
- [ ] **Fase 3 - domínio:** implementar vendas, movimentações, ofertas e fechamento diário.
- [ ] **Fase 4 - qualidade:** adicionar testes, CI/CD e empacotamento da aplicação.
- [ ] **Fase 5 - integração:** API, autenticação, WhatsApp oficial e jobs agendados.
- [ ] **Fase 6 - escala:** cardápio web/mobile, observabilidade, backups e relatórios.

## Equipe e contexto acadêmico

Projeto Integrador do curso **Programa Jovem Programador - Desenvolvedor de Sistemas**, da **Faculdade de Tecnologia SENAC Joinville**.

- Alan Benitez
- Matheus Souza
- Maurício Sant'anna
- Rodrigo da Cruz Godinho
- Orientador: Professor Marcelo
- Data registrada no material do projeto: maio de 2026

## Licença

Este projeto está sob a licença MIT.

---

<p><sub>© 2026 - Projeto Integrador SENAC</sub></p>
