# 🍽️ Sistema da Cantina - Cardápio Antidesperdício

<p align="left">
  <img src="https://img.shields.io/badge/.NET-10-blue?logo=dotnet&logoColor=white" alt=".NET 10" />
  <img src="https://img.shields.io/badge/WPF-UI-purple?logo=windows&logoColor=white" alt="WPF" />
  <img src="https://img.shields.io/badge/status-em%20desenvolvimento-yellow" alt="Status" />
  <img src="https://img.shields.io/badge/versão-1.0.0-blue" alt="Versão" />
  <img src="https://img.shields.io/badge/testes-0%25-red" alt="Testes" />
  <img src="https://img.shields.io/badge/coverage-0%25-red" alt="Cobertura" />
  <img src="https://img.shields.io/badge/licença-MIT-green" alt="Licença" />
  <img src="https://img.shields.io/badge/build-passing-brightgreen" alt="Build" />
</p>

Sistema desktop para gerenciamento de cantinas e pequenos comércios, desenvolvido em C# com WPF seguindo os padrões **MVVM** e **SOLID**. Oferece controle completo de produtos, estoque e aplicação de descontos, com interface moderna e persistência em banco de dados (MariaDB/EF Core).

---

## 🎯 Sobre o Projeto

O **Sistema da Cantina - Cardápio Antidesperdício** é uma aplicação desktop desenvolvida para simplificar o gerenciamento de produtos, estoque e descontos em estabelecimentos de pequeno e médio porte.

---

### Problema identificado

Identificamos que alguns produtos vendidos na cantina não são vendidos, gerando perdas para a cantina do SENAC.

### Solução proposta

Pensando em evitar que os alimentos que não foram vendidos no dia sejam jogados fora, desenvolvemos um sistema para fazer uma liquidação destes produtos antes de fechar a cantina. A função do sistema será avisar os alunos e frequentadores do SENAC através de mensagens por WhatsApp quais foram os itens que não foram vendidos e o seu valor com desconto.

---

## 📊 Visão Geral do Projeto

Com uma interface intuitiva e responsiva, o sistema permite:

- Cadastro e edição de produtos
- Cadastro e gerenciamento de alunos
- Controle de estoque em tempo real
- Aplicação de descontos percentuais ou em valor absoluto (R$)
- Persistência dos dados em banco de dados (MariaDB com EF Core)
- Estrutura preparada para notificações automáticas via WhatsApp

---

## 📌 Escopo do Projeto

O sistema será utilizado por alunos e funcionários do SENAC, com foco na cantina ALAMO.

### Diferenciais do sistema

- **Ofertas automáticas**: O sistema terá acesso aos itens disponíveis naquele dia, conseguirá dar baixa nos itens sem que uma pessoa tenha de ir manualmente sinalizar que o produto foi vendido, e disponibilizará a oferta sem que algum funcionário tenha de contar os produtos manualmente.
- **Comunicação via WhatsApp**: Será criado um grupo no WhatsApp para a cantina, no qual os alunos interessados participarão e o aplicativo irá disparar mensagens automáticas para os membros do grupo.
- **Cadastro opcional**: O cadastro dos alunos é opcional, permitindo que eles usem o sistema mesmo que tenham decidido não se cadastrar, ou caso tenha decidido se cadastrar, poderem usar antes mesmo de fazer o cadastro.
- **Compra online** (futuro): Caso seja viável e o tempo permita, adicionaremos a funcionalidade de poder comprar e pagar sem ter de sair da sala.

### Finalidade

A finalidade do sistema será enviar aos clientes uma lista dos produtos que não foram comercializados até as 20:45.

---

## 👥 Público-Alvo

- Donos de cantinas, lanchonetes, padarias e pequenos mercados.
- Alunos e funcionários do SENAC (especificamente da cantina ALAMO).
- Estudantes e desenvolvedores que desejam aprender WPF e MVVM.
- Projetos acadêmicos que necessitam de um sistema de gestão simples.

---

## 📋 Requisitos Funcionais

| ID    | Requisito |
|-------|-----------|
| RF001 | Cardápio antidesperdício |
| RF002 | Notificação automática no horário da queima de estoque |
| RF003 | Informar pelo WhatsApp |

### Detalhamento dos requisitos funcionais:

- **RF001 - Cardápio antidesperdício**: O sistema deve permitir cadastrar e remover produtos do cardápio, informar quantidade e preço dos itens.
- **RF002 - Notificação automática**: O sistema deve enviar automaticamente uma lista dos produtos não comercializados até as 20:45.
- **RF003 - Informar pelo WhatsApp**: O sistema deve integrar com WhatsApp para disparar mensagens automáticas para os membros do grupo.

---

## ⚙️ Requisitos Não Funcionais

| ID     | Requisito |
|--------|-----------|
| RNF001 | Lista de produtos com quantidades disponíveis |
| RNF002 | Comunicação com WhatsApp |
| RNF003 | Cardápio diário |

### Detalhamento dos requisitos não funcionais:

- **RNF001 - Lista de produtos**: O sistema deve disponibilizar um cardápio com as quantidades disponíveis para os clientes.
- **RNF002 - Comunicação com WhatsApp**: O sistema deve se comunicar com o WhatsApp para envio de notificações.
- **RNF003 - Cardápio diário**: O sistema deve gerar um cardápio diário com os produtos disponíveis.

---

## 🛠 Tecnologias

| Tecnologia | Versão | Finalidade |
|------------|--------|------------|
| .NET       | 10     | Framework principal |
| WPF        | -      | Interface gráfica (XAML) |
| C#         | 12     | Linguagem de programação |
| MVVM       | -      | Padrão arquitetural ([saiba mais](https://docs.microsoft.com/pt-br/dotnet/architecture/maui/mvvm)) |
| Data Binding| -     | Comunicação View-ViewModel |
| Entity Framework Core | - | ORM para persistência em banco de dados |
| MariaDB | - | Banco de dados local |

---

## ✨ Funcionalidades

- ✅ **CRUD de Produtos** – Cadastro, listagem, edição e exclusão.
- ✅ **Controle de Estoque** – Atualização automática da quantidade.
- ✅ **Descontos Flexíveis**:
  - Percentual (%) – Ex.: 10% de desconto.
  - Valor Fixo (R$) – Ex.: R$ 5,00 de desconto.
- ✅ **Interface Responsiva** – DataGrids com edição inline.
- ✅ **Validação de Dados** – Prevenção de entradas inválidas.
- ✅ **Conversores Personalizados** – Formatação monetária automática.
- ✅ **Persistência em Banco de Dados** – Dados armazenados em MariaDB/EF Core.

---

## 🧱 Arquitetura

O projeto segue o padrão **MVVM (Model-View-ViewModel)** com separação clara de responsabilidades:

| Camada | Componentes | Responsabilidade |
|--------|-------------|------------------|
| **Views (XAML)** | `MainWindow`, `AlunoWindow`, `CadastroWindow`, `EstoqueWindow` | Interface gráfica, interação com o usuário, exibição de dados. |
| **ViewModels** | `MainViewModel`, `AlunoViewModel`, `CadastroViewModel`, `EstoqueViewModel`, `BaseViewModel`, `RelayCommand` | Lógica de apresentação, estado da interface, comandos e bindings. |
| **Services** | `AlunoService`, `ProdutoService`, `OperacaoProdutoService`, `DescontoService`, `EstoqueService`, `MensagemService`, `Storage` | Regras de negócio, persistência (via EF Core), cálculos de desconto, estoque e notificações. |
| **Models** | `Produto`, `Aluno` | Entidades de dados. |
| **Converters** | `MoneyConverter`, `TextMoneyConverter` | Formatação e conversão de valores monetários para exibição e edição. |
| **Data** | `AppDbContext`, `DesignTimeDbContextFactory`, `Migrations` | Configuração do banco de dados, contexto e migrações. |

### Serviços principais

- **AlunoService**: Operações CRUD para alunos (`IAlunoService`).
- **ProdutoService**: Operações CRUD e regras de negócio para produtos (`IProdutoService`).
- **OperacaoProdutoService**: Gerencia vendas, entradas e saídas de produtos (`IOperacaoProdutoService`).
- **EstoqueService**: Gerencia a quantidade em estoque e validações (`IEstoqueService`).
- **DescontoService**: Aplica descontos percentuais ou fixos, calcula o valor final.
- **MensagemService**: Responsável pelo envio de notificações (integração com WhatsApp) - em desenvolvimento.
- **Storage**: Responsável pela persistência em banco de dados via EF Core.

---

## 📦 Estrutura de Dados

Os dados são persistidos em um banco de dados MariaDB, gerenciado pelo Entity Framework Core. As migrações do EF Core são responsáveis por criar e atualizar a estrutura do banco de dados automaticamente.

### Tabela `Produtos`

```Sql
CREATE TABLE Produtos (
    Id INTEGER PRIMARY KEY AUTO_INCREMENT,
    Nome VARCHAR(100) NOT NULL,
    Preco DECIMAL(10,2) NOT NULL,
    QuantidadeEstoque INTEGER NOT NULL,
    Categoria VARCHAR(50)
);
````

### Tabela `Alunos`

```Sql
CREATE TABLE Alunos (
    Id INTEGER PRIMARY KEY AUTO_INCREMENT,
    Nome VARCHAR(100) NOT NULL,
    WhatsApp VARCHAR(20) NOT NULL,
    DataCadastro DATETIME DEFAULT CURRENT_TIMESTAMP
);

```

### Gerenciamento das Migrações

As migrações são gerenciadas pelo Entity Framework Core e podem ser executadas através dos comandos:

````
# Criar uma nova migração
dotnet ef migrations add NomeDaMigracao

# Atualizar o banco de dados com as migrações pendentes
dotnet ef database update
````

### Configuração da Conexão

A string de conexão com o banco de dados é configurada no AppDbContext.cs através da classe DesignTimeDbContextFactory, que permite a execução das migrações em tempo de design.

---

## 📋 Pré-requisitos

- **Sistema Operacional**: Windows 10 ou superior (WPF não é multiplataforma).
- **.NET SDK**: 10.0 ou superior – [Baixar aqui](https://dotnet.microsoft.com/download).
- **Visual Studio**: 2022 (versão 17.8 ou superior) com a carga de trabalho **Desenvolvimento para desktop .NET**.
- **MariaDB** (ou MySQL): Para o banco de dados local.
- **(Opcional)** Git para clonar o repositório.

### Configuração do Banco de Dados

1. Instale o MariaDB e crie um banco de dados.
2. Configure a string de conexão no `AppDbContext.cs` ou no arquivo `appsettings.json`.
3. Execute as migrações:
   
   ```
   dotnet ef database update
   ```

---

## 📁 Estrutura do Projeto

````
Projeto-Integrador-SENAC/
├── bin/
│   └── Debug/
│       └── net10.0-windows/             # Arquivos compilados (gerados automaticamente)
├── obj/                                 # Arquivos temporários de compilação (gerados automaticamente)
├── Converters/
│   ├── MoneyConverter.cs                # Converte decimal para formato monetário (ex.: R$ 10,00)
│   └── TextMoneyConverter.cs            # Converte string para decimal (edição no DataGrid)
├── Data/
│   ├── AppDbContext.cs                  # Contexto do Entity Framework Core para banco de dados
│   └── DesignTimeDbContextFactory.cs    # Fábrica para criação do contexto em tempo de design (migrações)
├── Migrations/                          # Migrações automáticas do banco de dados (EF Core)
├── Models/
│   ├── Aluno.cs                         # Entidade Aluno (Id, Nome, WhatsApp, etc.)
│   └── Produto.cs                       # Entidade Produto (Id, Nome, Preço, Estoque, Categoria)
├── Services/
│   ├── AlunoService.cs                  # CRUD e regras de negócio para alunos
│   ├── DescontoService.cs               # Lógica de aplicação de descontos
│   ├── EstoqueService.cs                # Gerenciamento de quantidades em estoque
│   ├── IAlunoService.cs                 # Interface para injeção de dependência de AlunoService
│   ├── IEstoqueService.cs               # Interface para injeção de dependência de EstoqueService
│   ├── IOperacaoProdutoService.cs       # Interface para injeção de dependência de OperacaoProdutoService
│   ├── IProdutoService.cs               # Interface para injeção de dependência de ProdutoService
│   ├── MensagemService.cs               # Serviço de notificações (WhatsApp)
│   ├── OperacaoProdutoService.cs        # Registro de vendas e movimentações de produtos
│   ├── ProdutoService.cs                # CRUD e regras de negócio para produtos
│   └── Storage.cs                       # Persistência em banco de dados (via EF Core)
├── ViewModels/
│   ├── AlunoViewModel.cs                # Lógica da tela de gerenciamento de alunos
│   ├── BaseViewModel.cs                 # Classe base com INotifyPropertyChanged
│   ├── CadastroViewModel.cs             # Lógica da tela de cadastro/edição de produtos
│   ├── EstoqueViewModel.cs              # Lógica da tela de gerenciamento de estoque
│   ├── MainViewModel.cs                 # ViewModel principal (listagem e ações)
│   └── RelayCommand.cs                  # Implementação de ICommand para ações da UI
├── Views/
│   ├── AlunoWindow.xaml                 # Tela de gerenciamento de alunos (cadastro, listagem, edição)
│   ├── AlunoWindow.xaml.cs              # Code-behind da tela de alunos (eventos, inicialização)
│   ├── CadastroWindow.xaml              # Tela de cadastro/edição de produtos
│   ├── CadastroWindow.xaml.cs           # Code-behind da tela de cadastro de produtos
│   ├── EstoqueWindow.xaml               # Tela de gerenciamento de estoque
│   ├── EstoqueWindow.xaml.cs            # Code-behind da tela de estoque
│   ├── MainWindow.xaml                  # Janela principal do sistema
│   └── MainWindow.xaml.cs               # Code-behind da janela principal (eventos, inicialização)
├── App.xaml                             # Configuração da aplicação (recursos, estilos)
├── App.xaml.cs                          # Code-behind da aplicação (Startup, configurações)
├── AssemblyInfo.cs                      # Informações do assembly (versão, atributos)
├── .gitignore                           # Arquivos ignorados pelo Git
├── Projeto-Integrador-SENAC.csproj      # Arquivo do projeto (dependências, referências)
├── Projeto-Integrador-SENAC.csproj.user # Configurações específicas do usuário (gerado automaticamente)
├── Projeto-Integrador-SENAC.slnx        # Arquivo da solução (organização do projeto)
└── README.md                            # Documentação do projeto
````
---

## 📐 Padrões e Boas Práticas

| Padrão/Prática | Descrição |
|----------------|-----------|
| **MVVM** | Separação completa entre interface (View) e lógica de negócio (ViewModel). |
| **SOLID** | Aplicação dos princípios, com destaque para **SRP** (cada classe tem uma única responsabilidade) e **OCP** (serviços extensíveis). |
| **DRY** | `BaseViewModel` centraliza a implementação de `INotifyPropertyChanged`. |
| **Data Binding** | Sincronização automática entre View e ViewModel (bidirecional). |
| **Injeção de Dependência** | Serviços dependem de interfaces (ex.: `IStorage`) para facilitar testes e manutenção. |
| **RelayCommand** | Implementação reutilizável de `ICommand`, eliminando code-behind. |

---

## 🗺️ Roadmap

### ✅ Já implementado

- [x] CRUD completo de produtos.
- [x] CRUD completo de alunos.
- [x] Sistema de descontos (percentual e valor absoluto).
- [x] Controle de estoque.
- [x] Interface com DataGrid para edição inline.
- [x] Conversores monetários.
- [x] MVVM com `RelayCommand`.
- [x] Persistência em banco de dados (MariaDB/EF Core).
- [x] Estrutura de serviços com injeção de dependência.
- [x] Estrutura base para notificações (`MensagemService`).

### 📋 Futuras melhorias

#### Curto prazo (próximas 2 semanas)
- [ ] Testes unitários com xUnit para os serviços.
- [ ] Configuração de CI/CD com GitHub Actions.
- [ ] Correção de bugs reportados.
- [ ] Finalizar integração com WhatsApp (`MensagemService`).

#### Médio prazo (1–2 meses)
- [ ] Relatórios e gráficos (produtos mais vendidos, estoque baixo).
- [ ] Histórico de movimentações (log de alterações).
- [ ] Exportação de dados (CSV/Excel).

#### Longo prazo (3+ meses)
- [ ] Autenticação de usuários (login com perfis).
- [ ] Backups automáticos.
- [ ] Versão multiplataforma com MAUI (opcional).
- [ ] Pagamento online integrado.

---

## 👨‍🏫 Equipe

##### Alan Benitez

##### Matheus Souza

##### Maurício Sant'anna

##### Rodrigo da Cruz Godinho

##### Orientador: Professor Marcelo

##### Instituição: FACULDADE DE TECNOLOGIA SENAC JOINVILLE

##### Curso: PROGRAMA JOVEM PROGRAMADOR - DESENVOLVEDOR DE SISTEMAS

##### Data: Maio - 2026



---

## 📌 Notas finais

Este projeto foi desenvolvido como parte do Projeto Integrador do curso Jovem Programador do SENAC. Estamos abertos a sugestões, críticas construtivas e contribuições que possam aprimorar ainda mais o sistema.

---

## 📄 Licença

<div align="left">

[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](https://opensource.org/licenses/MIT)
[![Year](https://img.shields.io/badge/©-2026-blue)]()

</div>

Este projeto está sob a licença **MIT**.

---

<p align="left">
  <sub>© 2026 - Projeto Integrador SENAC</sub>
</p>
