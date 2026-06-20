# 🍽️ Sistema da Cantina

<p align="center">
  <img src="https://img.shields.io/badge/.NET-10-blue?logo=dotnet&logoColor=white" alt=".NET 10" />
  <img src="https://img.shields.io/badge/WPF-UI-purple?logo=windows&logoColor=white" alt="WPF" />
  <img src="https://img.shields.io/badge/status-em%20desenvolvimento-yellow" alt="Status" />
  <img src="https://img.shields.io/badge/versão-1.0.0-blue" alt="Versão" />
  <img src="https://img.shields.io/badge/testes-0%25-red" alt="Testes" />
  <img src="https://img.shields.io/badge/coverage-0%25-red" alt="Cobertura" />
  <img src="https://img.shields.io/badge/licença-MIT-green" alt="Licença" />
  <img src="https://img.shields.io/badge/build-passing-brightgreen" alt="Build" />
</p>

Sistema desktop para gerenciamento de cantinas e pequenos comércios, desenvolvido em C# com WPF seguindo os padrões **MVVM** e **SOLID**. Oferece controle completo de produtos, estoque e aplicação de descontos, com interface moderna e persistência local.

---

## 📋 Índice

- [Visão Geral](#visão-geral)
- [Público-Alvo](#público-alvo)
- [Tecnologias](#tecnologias)
- [Funcionalidades](#funcionalidades)
- [Capturas de Tela](#capturas-de-tela)
- [Arquitetura](#arquitetura)
- [Estrutura de Dados](#estrutura-de-dados)
- [Pré-requisitos](#pré-requisitos)
- [Como Executar](#como-executar)
  - [Visual Studio](#visual-studio-recomendado)
  - [Dotnet CLI](#dotnet-cli)
- [Resolução de Problemas](#resolução-de-problemas)
- [Estrutura do Projeto](#estrutura-do-projeto)
- [Padrões e Boas Práticas](#padrões-e-boas-práticas)
- [Roadmap](#roadmap)
- [Contribuição](#contribuição)
- [Licença](#licença)
- [Equipe](#equipe)
- [Agradecimentos](#agradecimentos)

---

## 🎯 Visão Geral

O **Sistema da Cantina** é uma aplicação desktop desenvolvida para simplificar o gerenciamento de produtos, estoque e descontos em estabelecimentos de pequeno e médio porte. Com uma interface intuitiva e responsiva, o sistema permite:

- Cadastro e edição de produtos
- Controle de estoque em tempo real
- Aplicação de descontos percentuais ou em valor absoluto (R$)
- Persistência local dos dados em arquivo (formato JSON)

---

## 👥 Público-Alvo

- Donos de cantinas, lanchonetes, padarias e pequenos mercados.
- Estudantes e desenvolvedores que desejam aprender WPF e MVVM.
- Projetos acadêmicos que necessitam de um sistema de gestão simples.

---

## 🛠 Tecnologias

| Tecnologia | Versão | Finalidade |
|------------|--------|------------|
| .NET       | 10     | Framework principal |
| WPF        | -      | Interface gráfica (XAML) |
| C#         | 12     | Linguagem de programação |
| MVVM       | -      | Padrão arquitetural ([saiba mais](https://docs.microsoft.com/pt-br/dotnet/architecture/maui/mvvm)) |
| Data Binding| -     | Comunicação View-ViewModel |

---

## ✨ Funcionalidades

- ✅ **CRUD de Produtos** – Cadastro, listagem, edição e exclusão.
- ✅ **Controle de Estoque** – Atualização automática da quantidade.
- ✅ **Descontos Flexíveis**:
  - Percentual (%) – Ex.: 10% de desconto.
  - Valor Fixo (R$) – Ex.: R$ 5,00 de desconto.
- ✅ **Persistência Local** – Dados salvos em arquivo (`produtos.json`).
- ✅ **Interface Responsiva** – DataGrids com edição inline.
- ✅ **Validação de Dados** – Prevenção de entradas inválidas.
- ✅ **Conversores Personalizados** – Formatação monetária automática.

---

## 🧱 Arquitetura

O projeto segue o padrão **MVVM (Model-View-ViewModel)** com separação clara de responsabilidades:

| Camada | Componentes | Responsabilidade |
|--------|-------------|------------------|
| **Views (XAML)** | `MainWindow`, `CadastroWindow`, `EstoqueWindow` | Interface gráfica, interação com o usuário, exibição de dados. |
| **ViewModels** | `MainViewModel`, `CadastroViewModel`, `EstoqueViewModel`, `BaseViewModel`, `RelayCommand` | Lógica de apresentação, estado da interface, comandos e bindings. |
| **Services** | `ProdutoService`, `DescontoService`, `EstoqueService`, `StorageService` (implements `IStorage`) | Regras de negócio, persistência, cálculos de desconto e estoque. |
| **Models** | `Produto` | Entidade de dados (propriedades: Id, Nome, Preço, QuantidadeEstoque, Categoria). |
| **Converters** | `MoneyConverter`, `TextMoneyConverter` | Formatação e conversão de valores monetários para exibição e edição. |

### Serviços principais

- **ProdutoService**: operações CRUD e regras de negócio para produtos.
- **EstoqueService**: gerencia a quantidade em estoque e validações.
- **DescontoService**: aplica descontos percentuais ou fixos, calcula o valor final.
- **StorageService**: responsável pela leitura/gravação do arquivo de dados (persistência).

---

## 📦 Estrutura de Dados

Os produtos são persistidos em um arquivo JSON (`produtos.json`) com a seguinte estrutura:

```json
[
  {
    "Id": 1,
    "Nome": "Coxinha",
    "Preco": 6.50,
    "QuantidadeEstoque": 10,
    "Categoria": "Salgados"
  },
  {
    "Id": 2,
    "Nome": "Refrigerante 350ml",
    "Preco": 4.00,
    "QuantidadeEstoque": 25,
    "Categoria": "Bebidas"
  }
]
````
O arquivo é criado automaticamente na primeira execução, na mesma pasta do executável.

---

## 📋 Pré-requisitos

Sistema Operacional: Windows 10 ou superior (WPF não é multiplataforma).

.NET SDK 10.0 ou superior – Baixar aqui.

Visual Studio 2022 (versão 17.8 ou superior) com a carga de trabalho Desenvolvimento para desktop .NET.

(Opcional) Git para clonar o repositório.

---

## 📁 Estrutura do Projeto

````text
Projeto-Integrador-SENAC/
├── Converters/
│   ├── MoneyConverter.cs          # Converte decimal para formato monetário (ex.: R$ 10,00)
│   └── TextMoneyConverter.cs      # Converte string para decimal (edição no DataGrid)
├── Models/
│   └── Produto.cs                 # Entidade Produto (Id, Nome, Preço, Estoque, etc.)
├── Services/
│   ├── DescontoService.cs         # Lógica de aplicação de descontos
│   ├── EstoqueService.cs          # Gerenciamento de quantidades
│   ├── ProdutoService.cs          # Operações CRUD e regras de negócio
│   └── StorageService.cs          # Persistência em arquivo (JSON)
├── ViewModels/
│   ├── BaseViewModel.cs           # Classe base com INotifyPropertyChanged
│   ├── CadastroViewModel.cs       # Lógica da tela de cadastro/edição
│   ├── EstoqueViewModel.cs        # Lógica da tela de estoque
│   ├── MainViewModel.cs           # ViewModel principal (listagem e ações)
│   └── RelayCommand.cs            # Implementação de ICommand para ações da UI
├── Views/
│   ├── MainWindow.xaml            # Janela principal
│   ├── CadastroWindow.xaml        # Tela de cadastro/edição
│   └── EstoqueWindow.xaml         # Tela de gerenciamento de estoque
├── App.xaml                       # Configuração da aplicação (recursos, estilos)
├── App.xaml.cs                    # Code-behind da aplicação (Startup)
└── Projeto-Integrador-SENAC.csproj
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
- [x] Sistema de descontos (percentual e valor absoluto).
- [x] Controle de estoque.
- [x] Persistência local com `StorageService`.
- [x] Interface com DataGrid para edição inline.
- [x] Conversores monetários.
- [x] MVVM com `RelayCommand`.

---

### 📋 Futuras melhorias

#### Curto prazo (próximas 2 semanas)
- [ ] Testes unitários com xUnit para os serviços.
- [ ] Configuração de CI/CD com GitHub Actions.
- [ ] Correção de bugs reportados.

#### Médio prazo (1–2 meses)
- [ ] Relatórios e gráficos (produtos mais vendidos, estoque baixo).
- [ ] Histórico de movimentações (log de alterações).
- [ ] Categorização de produtos (ex.: bebidas, salgados, doces).
- [ ] Exportação de dados (CSV/Excel).

#### Longo prazo (3+ meses)
- [ ] Autenticação de usuários (login com perfis).
- [ ] Banco de dados (SQLite/PostgreSQL) como opção de persistência.
- [ ] Backups automáticos.
- [ ] Versão multiplataforma com MAUI (opcional).

---

## 🤝 Contribuição

Contribuições são muito bem-vindas! Para contribuir com o projeto, siga os passos abaixo:

1. **Verifique as issues** abertas ou abra uma nova para discutir a alteração que você deseja fazer.
2. **Fork** o repositório para sua conta do GitHub.
3. Crie uma branch com um nome descritivo para a sua contribuição:

   ```bash
   git checkout -b feature/nova-funcionalidade
   
Ou, para correções de bugs:

```
git checkout -b fix/correcao-bug
````
---   

## 📄 Licença

Este projeto está sob a licença MIT. Veja o arquivo LICENSE para mais informações.

© 2026 - Projeto Integrador SENAC

---

## 👨‍🏫 Equipe

##### Allan Benitez

##### Matheus Souza

##### Maurício Sant'anna

##### Rodrigo da Cruz Godinho

---

## 🙏 Agradecimentos

##### Aos professores e orientadores do curso.

##### À comunidade open source por fornecer ferramentas incríveis.

##### A todos que testaram e deram feedback durante o desenvolvimento.
