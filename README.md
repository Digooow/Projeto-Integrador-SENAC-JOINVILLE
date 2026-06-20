🍽️ Sistema da Cantina
https://img.shields.io/badge/.NET-10-blue?logo=dotnet&logoColor=white
https://img.shields.io/badge/WPF-UI-purple?logo=windows&logoColor=white
https://img.shields.io/badge/status-em%2520desenvolvimento-yellow

Sistema desktop para gerenciamento de cantina com interface moderna, desenvolvido em C# com WPF seguindo os padrões MVVM e SOLID.

📋 Sumário
Visão Geral

Tecnologias

Funcionalidades

Arquitetura

Como Executar

Visual Studio

Dotnet CLI

Estrutura do Projeto

Padrões e Boas Práticas

Próximos Passos

Contribuição

Licença

🎯 Visão Geral
O Sistema da Cantina é uma aplicação desktop desenvolvida para simplificar o gerenciamento de produtos, estoque e aplicação de descontos em estabelecimentos comerciais de pequeno e médio porte.

Com uma interface intuitiva e responsiva, o sistema permite:

Cadastro e edição de produtos

Controle de estoque em tempo real

Aplicação de descontos percentuais e em valor absoluto (R$)

Persistência local dos dados

🛠 Tecnologias
Tecnologia	Versão	Finalidade
.NET	10	Framework principal
WPF	-	Interface gráfica (XAML)
C#	12	Linguagem de programação
MVVM	-	Padrão arquitetural
Data Binding	-	Comunicação View-ViewModel
✨ Funcionalidades
✅ CRUD de Produtos – Cadastro, listagem, edição e exclusão

✅ Controle de Estoque – Atualização automática de quantidade

✅ Descontos Flexíveis:

Percentual (%) – Ex: 10% de desconto

Valor Fixo (R
)
–
E
x
:
R
)–Ex:R 5,00 de desconto

✅ Persistência Local – Dados salvos em arquivo (Storage)

✅ Interface Responsiva – DataGrids com edição inline

✅ Validação de Dados – Prevenção de entradas inválidas

✅ Conversores Personalizados – Formatação monetária automática

🧱 Arquitetura
O projeto segue o padrão MVVM (Model-View-ViewModel) com separação clara de responsabilidades:

text
┌─────────────────────────────────────────────────────────┐
│                       VIEWS (XAML)                      │
│  MainWindow │ CadastroWindow │ EstoqueWindow            │
├─────────────────────────────────────────────────────────┤
│                    VIEWMODELS                           │
│  MainViewModel │ CadastroViewModel │ EstoqueViewModel   │
│  BaseViewModel │ RelayCommand                           │
├─────────────────────────────────────────────────────────┤
│                     SERVICES                            │
│  ProdutoService │ DescontoService │ EstoqueService      │
│  StorageService │ IStorage                              │
├─────────────────────────────────────────────────────────┤
│                     MODELS                              │
│  Produto                                               │
├─────────────────────────────────────────────────────────┤
│                   CONVERTERS                            │
│  MoneyConverter │ TextMoneyConverter                    │
└─────────────────────────────────────────────────────────┘
🚀 Como Executar
Visual Studio (Recomendado)
Abra o arquivo Projeto-Integrador-SENAC.sln no Visual Studio 2022/2024/2026

Compile a solução: Ctrl + Shift + B

Execute o projeto: F5

Dotnet CLI
powershell
# Restaurar dependências
dotnet restore

# Compilar o projeto
dotnet build

# Executar a aplicação
dotnet run --project Projeto-Integrador-SENAC.csproj
⚠️ Nota: A execução via CLI pode apresentar limitações no WPF. Recomenda-se o uso do Visual Studio para melhor experiência.

📁 Estrutura do Projeto
text
Projeto-Integrador-SENAC/
├── Converters/
│   ├── MoneyConverter.cs          # Converte decimal para formato monetário
│   └── TextMoneyConverter.cs      # Converte string para decimal (edição)
├── Models/
│   └── Produto.cs                 # Entidade principal
├── Services/
│   ├── DescontoService.cs         # Lógica de aplicação de descontos
│   ├── EstoqueService.cs          # Gerenciamento de quantidades
│   ├── ProdutoService.cs          # Operações CRUD
│   └── StorageService.cs          # Persistência em arquivo
├── ViewModels/
│   ├── BaseViewModel.cs           # Base com INotifyPropertyChanged
│   ├── CadastroViewModel.cs       # Lógica da tela de cadastro
│   ├── EstoqueViewModel.cs        # Lógica da tela de estoque
│   ├── MainViewModel.cs           # ViewModel principal
│   └── RelayCommand.cs            # Implementação de ICommand
├── Views/
│   ├── MainWindow.xaml            # Janela principal
│   ├── CadastroWindow.xaml        # Tela de cadastro
│   └── EstoqueWindow.xaml         # Tela de gerenciamento de estoque
├── App.xaml                       # Configuração da aplicação
├── App.xaml.cs                    # Code-behind da aplicação
└── Projeto-Integrador-SENAC.csproj
📐 Padrões e Boas Práticas
Padrão/Prática	Descrição
MVVM	Separação completa entre interface (View) e lógica (ViewModel)
SOLID	Princípios aplicados, especialmente SRP e OCP
DRY	BaseViewModel centraliza INotifyPropertyChanged
Data Binding	Sincronização automática entre View e ViewModel
Injeção de Dependência	Uso de serviços com interfaces (IStorage)
RelayCommand	Implementação reutilizável de comandos para ações da UI
🔮 Próximos Passos
✅ Já implementado
CRUD completo de produtos

Sistema de descontos (percentual e valor absoluto)

Controle de estoque

Persistência local com StorageService

Interface com DataGrid para edição inline

Conversores monetários

MVVM com RelayCommand

📋 Futuras melhorias
Testes unitários (xUnit/NUnit)

Testes de integração e UI

Relatórios e gráficos

Histórico de movimentações

Categorização de produtos

Exportação de dados (CSV/Excel)

Autenticação de usuários

Banco de dados (SQLite/PostgreSQL)

Backups automáticos

🤝 Contribuição
Fork o repositório

Crie uma branch com sua feature: git checkout -b feature/nova-funcionalidade

Commit suas mudanças: git commit -m 'feat: adiciona nova funcionalidade'

Push para a branch: git push origin feature/nova-funcionalidade

Abra um Pull Request descrevendo suas alterações

📌 Consulte o arquivo CONTRIBUTING.md para mais detalhes (se disponível).

📄 Licença
Este projeto está sob a licença MIT. Veja o arquivo LICENSE para mais informações.

📊 Status do Projeto
https://img.shields.io/badge/vers%C3%A3o-1.0.0-blue
https://img.shields.io/badge/testes-0%2525-red
https://img.shields.io/badge/coverage-0%2525-red