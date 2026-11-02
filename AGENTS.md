# 🤖 AGENTS.md — Guia de Arquitetura Multi-Agente (Cantina ALAMO • SENAC Joinville)

Este documento é o **contrato oficial de engenharia** para qualquer Agente de IA (**Antigravity**, **Claude Code**, **Cursor**, **GitHub Copilot**, **Windsurf**, **v0.dev**, **Bolt**) que trabalhe neste repositório. Leia este arquivo **antes** de modificar qualquer código.

---

## 1. Visão Geral do Ecossistema

O projeto **DesperdícioZero — Cantina ALAMO (SENAC Joinville)** é uma plataforma completa dividida em **3 superfícies sincronizadas**:

1. **API Cloud & Motor Antidesperdício (`src/Cantina.Api` + Clean Architecture .NET 10)**
   - Hospedada gratuitamente no **Render** (`https://cantina-alamo-senac.onrender.com`) ou executada embarcada localmente pelo app Desktop (`http://localhost:5180`).
   - Possui Background Worker para a **Queima de Estoque Automática às 20:45**, reserva atômica de estoque e disparo de alertas para **WhatsApp** (Evolution API / Cloud API + Fallback `wa.me`).
2. **Vitrine Web Fura-Fila (`src/cantina-web` → `src/Cantina.Api/wwwroot`)**
   - Arquitetura em **4 camadas** baseada em **Angular 19+ Signals** e Design System **"Alamo Fresh & ZeroWaste"** (consulte o blueprint completo em [`docs/FRONTEND_DESIGN_AND_ARCHITECTURE.md`](docs/FRONTEND_DESIGN_AND_ARCHITECTURE.md)).
3. **Programa Desktop Instalável com Auto-Update (`src/Cantina.Wpf`)**
   - Aplicativo Windows nativo (**WPF .NET 10 MVVM**) para administradores gerenciarem preços, produtos, descontos em lote, fila de pedidos e fechamento de caixa.
   - Empacotado via **Velopack + GitHub Releases** (`CantinaAlamo-win-Setup.exe`) com verificador e instalador de atualizações **Over-The-Air (OTA)** integrado (`AppUpdateService`).

---

## 2. Divisão de Papéis para Trabalho Paralelo entre IAs

Para evitar conflitos de merge e regressões, cada agente deve respeitar estritamente as fronteiras de diretório abaixo:

| Papel / Agente | Escopo de Escrita Permitido | Arquivos de Contrato (Somente Leitura / Sincronia) |
| :--- | :--- | :--- |
| **Agente 1 — Backend & Domain (.NET 10)** | `src/Cantina.Domain/`, `src/Cantina.Application/`, `src/Cantina.Infrastructure/`, `src/Cantina.Api/Program.cs`, `tests/Cantina.Tests/` | `src/Cantina.Contracts/Dtos/CantinaContracts.cs` |
| **Agente 2 — Frontend Web (Angular & UI/UX)** | `src/cantina-web/`, `src/Cantina.Api/wwwroot/`, `docs/FRONTEND_DESIGN_AND_ARCHITECTURE.md` | `src/Cantina.Contracts/Dtos/CantinaContracts.cs`, `src/cantina-web/src/app/core/models/cantina.models.ts` |
| **Agente 3 — Desktop Admin & Installer (WPF)** | `src/Cantina.Wpf/`, `scripts/pack-installer.ps1`, `.github/workflows/release-desktop.yml` | `src/Cantina.Contracts/Dtos/CantinaContracts.cs` |
| **Agente 4 — DevOps & Cloud** | `Dockerfile`, `docker-compose.yml`, `render.yaml`, `.github/workflows/ci.yml`, `README.md` | `Projeto-Integrador-SENAC.slnx` |

---

## 3. Regras Inegociáveis de Arquitetura e Design

### 3.1. Sincronia de Contratos (Backend ↔ Web ↔ Desktop)
- A fonte da verdade dos DTOs é [`src/Cantina.Contracts/Dtos/CantinaContracts.cs`](src/Cantina.Contracts/Dtos/CantinaContracts.cs).
- Sempre que um campo for adicionado ou alterado em `CantinaContracts.cs`, atualize imediatamente [`src/cantina-web/src/app/core/models/cantina.models.ts`](src/cantina-web/src/app/core/models/cantina.models.ts).
- **Nunca quebre os endpoints públicos consumidos pelo site e pelos testes de integração:**
  - `GET /api/v1/health`
  - `GET /api/v1/cardapio`
  - `POST /api/v1/pedidos`
  - `GET /api/v1/pedidos/{codigo}`

### 3.2. Design System Web ("Alamo Fresh & ZeroWaste")
- **Guia Mestre Obrigatório:** Leia [`docs/FRONTEND_DESIGN_AND_ARCHITECTURE.md`](docs/FRONTEND_DESIGN_AND_ARCHITECTURE.md) antes de alterar o front-end.
- **PROIBIDO usar emojis (`🥐`, `🥤`, `🛒`) como foto de produto ou ícone de interface.**
  - Todo produto deve exibir **fotografia gastronômica real em alta definição** (`aspect-ratio: 4 / 3`, `object-fit: cover` diretamente na tag `<img>`) resolvida pelo `ProductImageResolver`.
  - Todos os ícones de interface devem ser **SVGs vetoriais (Lucide Icons)** com `stroke-width="2"`.
- **Padrões Modernos de HTML/CSS (Baseline Web):**
  - **Cards (`<article class="card">`):** Use `container-type: inline-size` e `subgrid` (`grid-template-rows: subgrid; grid-row: span 4`). Os filhos diretos do `<article class="card">` devem ser `<img>`, `<header>`, `<p>` e `<footer>` (não envolva a `<img>` em uma `<div class="media">` nem crie `.card__body`).
  - **LCP & Imagens:** A primeira imagem em destaque recebe `fetchpriority="high"` (sem `loading="lazy"`). As demais imagens abaixo da dobra usam `loading="lazy"` (sem `fetchpriority="low"`).
  - **Modais e Drawers (`<dialog>`):** Sempre inclua `closedby="any"` e abra via `.showModal()`, mantendo o fallback JS de clique no backdrop (`!('closedBy' in HTMLDialogElement.prototype)`).
  - **Formulários:** Posicione dicas de formato (`<span class="hint">`) **acima** do `<input>` para que o teclado virtual mobile e o autocomplete não as cubram. Use `:user-invalid` e `:user-valid` com sincronização de `aria-invalid`.

### 3.3. Programa Desktop Instalável e Auto-Atualizável (`Cantina.Wpf`)
- O aplicativo Desktop pode operar em **2 modos**:
  1. **Modo Cloud Remoto (Produção):** Apontado para `https://cantina-alamo-senac.onrender.com` (gerencia o site na nuvem de qualquer PC do SENAC).
  2. **Modo Local Integrado (Offline/Kiosk):** Apontado para `http://localhost:5180` (sobe o servidor Kestrel + SQLite automaticamente em background).
- **Fluxo de Atualização OTA (`AppUpdateService`):**
  - Consulta a versão instalada (`AssemblyInformationalVersion`) e verifica novas releases publicadas em `https://github.com/Digooow/Projeto-Integrador-SENAC-JOINVILLE/releases/latest`.
  - Suporta atualização silenciosa/1-clique via **Velopack** ou download automático do instalador `CantinaAlamo-win-Setup.exe` com execução imediata.

---

## 4. Comandos de Build, Teste e Empacotamento

Execute estes comandos na raiz do repositório para validar qualquer alteração:

```powershell
# 1. Compilar toda a Solution .NET 10 (Zero Warnings = Zero Errors)
dotnet build Projeto-Integrador-SENAC.slnx -c Release -m:1 --disable-build-servers -p:UseSharedCompilation=false

# 2. Executar todos os testes automatizados (Unitários + Integração HTTP/Web)
dotnet test Projeto-Integrador-SENAC.slnx -c Release --no-build

# 3. Gerar o instalador Desktop Windows (CantinaAlamo-win-Setup.exe)
pwsh -ExecutionPolicy Bypass -File .\scripts\pack-installer.ps1
```

> **Regra de Ouro:** `Directory.Build.props` define `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`. Nenhum PR ou commit pode introduzir avisos de compilação C# ou quebrar os 11 testes em `Cantina.Tests`.

