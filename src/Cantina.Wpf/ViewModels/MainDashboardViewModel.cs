using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Threading;
using Cantina.Contracts.Dtos;
using Cantina.Wpf.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Cantina.Wpf.ViewModels;

public sealed partial class MainDashboardViewModel : ObservableObject, IDisposable
{
    private readonly CantinaApiClient _api;
    private readonly CantinaWpfSettings _settings;
    private readonly DispatcherTimer _pollTimer;
    private bool _sincronizando;

    private static readonly CultureInfo PtBr = new("pt-BR");

    public IReadOnlyList<string> TurnosDisponiveis { get; } = ["Manha", "Tarde", "Noite"];
    public IReadOnlyList<string> FormasPagamentoDisponiveis { get; } = ["PixNaRetirada", "PixCopiaECola", "CartaoNoBalcao", "DinheiroNoBalcao"];

    public ObservableCollection<CategoriaDto> Categorias { get; } = [];
    public ObservableCollection<ProdutoDto> Produtos { get; } = [];
    public ObservableCollection<ProdutoDto> OfertasAtivas { get; } = [];
    public ObservableCollection<ClienteDestaqueDto> PrincipaisClientes { get; } = [];
    public ObservableCollection<PedidoDto> PedidosFila { get; } = [];
    public ObservableCollection<PedidoDto> HistoricoPedidos { get; } = [];
    public ObservableCollection<AlunoDto> Alunos { get; } = [];
    public ObservableCollection<GrupoWhatsAppDto> GruposWhatsApp { get; } = [];
    public ObservableCollection<NotificacaoWhatsAppDto> NotificacoesWhatsApp { get; } = [];
    public ObservableCollection<MovimentacaoEstoqueDto> MovimentacoesEstoque { get; } = [];
    public ObservableCollection<FechamentoDiarioDto> Fechamentos { get; } = [];

    // Navegação lateral
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VisibilidadeDashboard))]
    [NotifyPropertyChangedFor(nameof(VisibilidadePedidos))]
    [NotifyPropertyChangedFor(nameof(VisibilidadeCardapio))]
    [NotifyPropertyChangedFor(nameof(VisibilidadeWhatsApp))]
    [NotifyPropertyChangedFor(nameof(VisibilidadeFechamento))]
    [NotifyPropertyChangedFor(nameof(IsSecaoDashboard))]
    [NotifyPropertyChangedFor(nameof(IsSecaoPedidos))]
    [NotifyPropertyChangedFor(nameof(IsSecaoCardapio))]
    [NotifyPropertyChangedFor(nameof(IsSecaoWhatsApp))]
    [NotifyPropertyChangedFor(nameof(IsSecaoFechamento))]
    private string _secaoAtiva = "Dashboard";

    public Visibility VisibilidadeDashboard => SecaoAtiva == "Dashboard" ? Visibility.Visible : Visibility.Collapsed;
    public Visibility VisibilidadePedidos => SecaoAtiva == "Pedidos" ? Visibility.Visible : Visibility.Collapsed;
    public Visibility VisibilidadeCardapio => SecaoAtiva == "Cardapio" ? Visibility.Visible : Visibility.Collapsed;
    public Visibility VisibilidadeWhatsApp => SecaoAtiva == "WhatsApp" ? Visibility.Visible : Visibility.Collapsed;
    public Visibility VisibilidadeFechamento => SecaoAtiva == "Fechamento" ? Visibility.Visible : Visibility.Collapsed;

    public bool IsSecaoDashboard => SecaoAtiva == "Dashboard";
    public bool IsSecaoPedidos => SecaoAtiva == "Pedidos";
    public bool IsSecaoCardapio => SecaoAtiva == "Cardapio";
    public bool IsSecaoWhatsApp => SecaoAtiva == "WhatsApp";
    public bool IsSecaoFechamento => SecaoAtiva == "Fechamento";

    // Conexão e feedback
    [ObservableProperty]
    private bool _servidorOnline;

    [ObservableProperty]
    private string _textoStatusConexao = "Conectando ao serviço...";

    [ObservableProperty]
    private bool _estaCarregando;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VisibilidadeFeedback))]
    private string _mensagemFeedback = string.Empty;

    [ObservableProperty]
    private bool _feedbackErro;

    public Visibility VisibilidadeFeedback =>
        string.IsNullOrWhiteSpace(MensagemFeedback) ? Visibility.Collapsed : Visibility.Visible;

    [ObservableProperty]
    private string _ultimaAtualizacaoTexto = "--:--:--";

    // KPIs do Dashboard DesperdícioZero
    [ObservableProperty] private int _totalProdutosAtivos;
    [ObservableProperty] private int _produtosEmOferta;
    [ObservableProperty] private int _unidadesEmEstoque;
    [ObservableProperty] private int _pedidosPendentesFila;
    [ObservableProperty] private int _totalAlunosCadastrados;
    [ObservableProperty] private decimal _receitaNormalHoje;
    [ObservableProperty] private decimal _receitaRecuperadaHoje;
    [ObservableProperty] private decimal _receitaTotalHoje;
    [ObservableProperty] private decimal _metaReceitaRecuperadaDiaria = 150m;
    [ObservableProperty] private double _percentualMetaBarra;
    [ObservableProperty] private string _percentualMetaTexto = "0%";
    [ObservableProperty] private decimal _economiaGeradaAosAlunosHoje;
    [ObservableProperty] private string _horarioProgramadoQueima = "20:45";
    [ObservableProperty] private string _modoIntegracaoWhatsApp = "Link Manual / Outbox";

    // Campanha Queima de Estoque 20:45
    [ObservableProperty] private string _percentualQueimaInput = "40";
    [ObservableProperty] private bool _apenasPereciveisQueima = true;
    [ObservableProperty] private bool _dispararWhatsAppQueima = true;
    [ObservableProperty] private string _ultimaMensagemQueima = "Clique em 'Disparar Queima 20:45' para aplicar descontos automáticos nos perecíveis e notificar os grupos do SENAC.";

    // Fila Web & PDV Balcão
    [ObservableProperty] private PedidoDto? _pedidoFilaSelecionado;
    [ObservableProperty] private bool _abrirWaMeAoNotificar;
    [ObservableProperty] private ProdutoDto? _produtoBalcaoSelecionado;
    [ObservableProperty] private string _quantidadeBalcaoInput = "1";
    [ObservableProperty] private string _clienteBalcaoInput = string.Empty;
    [ObservableProperty] private string _formaPagamentoBalcao = "PixNaRetirada";

    // Gestão de Cardápio, Estoque e Descontos
    [ObservableProperty] private ProdutoDto? _produtoSelecionado;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TituloFormularioProduto))]
    private int? _produtoEmEdicaoId;

    public string TituloFormularioProduto =>
        ProdutoEmEdicaoId.HasValue ? $"Editando Produto #{ProdutoEmEdicaoId.Value}" : "Cadastrar Novo Produto";

    [ObservableProperty] private string _formProdutoNome = string.Empty;
    [ObservableProperty] private string _formProdutoDescricao = string.Empty;
    [ObservableProperty] private string _formProdutoEmoji = "🥐";
    [ObservableProperty] private CategoriaDto? _categoriaFormularioSelecionada;
    [ObservableProperty] private string _formProdutoPrecoCusto = "3,50";
    [ObservableProperty] private string _formProdutoPrecoVenda = "7,00";
    [ObservableProperty] private string _formProdutoEstoque = "15";
    [ObservableProperty] private bool _formProdutoPerecivel = true;
    [ObservableProperty] private bool _formProdutoAtivo = true;

    [ObservableProperty] private string _deltaEstoqueManualInput = "5";
    [ObservableProperty] private string _motivoEstoqueManualInput = "Reposição de fornada";
    [ObservableProperty] private string _descontoPercentualInput = "30";
    [ObservableProperty] private string _descontoValorFixoInput = string.Empty;
    [ObservableProperty] private bool _descontoApenasPereciveis = true;

    // Grupos WhatsApp & Alunos
    [ObservableProperty] private GrupoWhatsAppDto? _grupoSelecionado;
    [ObservableProperty] private int? _formGrupoId;
    [ObservableProperty] private string _formGrupoNome = string.Empty;
    [ObservableProperty] private string _formGrupoTurno = "Noite";
    [ObservableProperty] private string _formGrupoLink = string.Empty;
    [ObservableProperty] private string _formGrupoJid = string.Empty;
    [ObservableProperty] private string _formGrupoParticipantes = "50";
    [ObservableProperty] private bool _formGrupoAtivo = true;

    [ObservableProperty] private string _formAlunoNome = string.Empty;
    [ObservableProperty] private string _formAlunoEmail = string.Empty;
    [ObservableProperty] private string _formAlunoTelefone = string.Empty;
    [ObservableProperty] private string _formAlunoMatricula = string.Empty;
    [ObservableProperty] private string _formAlunoTurno = "Noite";
    [ObservableProperty] private bool _formAlunoAlertas = true;

    // Fechamento Diário & Servidor Nuvem
    [ObservableProperty] private string _observacoesFechamentoInput = string.Empty;
    [ObservableProperty] private string _apiBaseUrlConfig;
    [ObservableProperty] private string _adminApiKeyConfig;
    [ObservableProperty] private bool _autoStartLocalConfig;

    public MainDashboardViewModel(CantinaApiClient api, CantinaWpfSettings settings)
    {
        _api = api;
        _settings = settings;

        _apiBaseUrlConfig = settings.ApiBaseUrl;
        _adminApiKeyConfig = settings.AdminApiKey;
        _autoStartLocalConfig = settings.AutoStartLocalServer;

        _pollTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(Math.Clamp(settings.PollIntervalSeconds, 3, 60))
        };
        _pollTimer.Tick += async (_, _) => await SincronizarSilenciosoAsync();
    }

    partial void OnProdutoSelecionadoChanged(ProdutoDto? value)
    {
        if (value is null)
            return;

        ProdutoEmEdicaoId = value.Id;
        FormProdutoNome = value.Nome;
        FormProdutoDescricao = value.Descricao;
        FormProdutoEmoji = value.IconeEmoji;
        CategoriaFormularioSelecionada = Categorias.FirstOrDefault(c => c.Id == value.CategoriaId);
        FormProdutoPrecoCusto = value.PrecoCusto.ToString("N2", PtBr);
        FormProdutoPrecoVenda = value.PrecoVenda.ToString("N2", PtBr);
        FormProdutoEstoque = value.QuantidadeEstoque.ToString(CultureInfo.InvariantCulture);
        FormProdutoPerecivel = value.Perecivel;
        FormProdutoAtivo = value.Ativo;
    }

    partial void OnGrupoSelecionadoChanged(GrupoWhatsAppDto? value)
    {
        if (value is null)
            return;

        FormGrupoId = value.Id;
        FormGrupoNome = value.Nome;
        FormGrupoTurno = value.Turno;
        FormGrupoLink = value.LinkConvite;
        FormGrupoJid = value.DestinoApiJid ?? string.Empty;
        FormGrupoParticipantes = value.ParticipantesEstimados.ToString(CultureInfo.InvariantCulture);
        FormGrupoAtivo = value.Ativo;
    }

    public async Task InicializarAsync()
    {
        await AtualizarTudoAsync();
        _pollTimer.Start();
    }

    [RelayCommand]
    private void Navegar(string? secao)
    {
        if (!string.IsNullOrWhiteSpace(secao))
        {
            SecaoAtiva = secao;
        }
    }

    [RelayCommand]
    private void FecharFeedback() => MensagemFeedback = string.Empty;

    [RelayCommand]
    private async Task AtualizarTudoAsync()
    {
        await ExecutarAcaoSeguraAsync(async () =>
        {
            await CarregarDadosRemotosAsync();
        });
    }

    private async Task SincronizarSilenciosoAsync()
    {
        if (_sincronizando || EstaCarregando)
            return;

        try
        {
            _sincronizando = true;
            await CarregarDadosRemotosAsync();
        }
        catch
        {
            ServidorOnline = false;
            TextoStatusConexao = $"Offline ({_api.BaseUrl})";
        }
        finally
        {
            _sincronizando = false;
        }
    }

    private async Task CarregarDadosRemotosAsync()
    {
        var dashTask = _api.GetDashboardAsync();
        var catsTask = _api.GetCategoriasAsync();
        var prodsTask = _api.GetProdutosAsync();
        var filaTask = _api.GetPedidosAsync(apenasFila: true);
        var todosPedidosTask = _api.GetPedidosAsync(apenasFila: false);
        var alunosTask = _api.GetAlunosAsync();
        var gruposTask = _api.GetGruposWhatsAppAsync();
        var notifsTask = _api.GetNotificacoesWhatsAppAsync();
        var movsTask = _api.GetMovimentacoesEstoqueAsync();
        var fechsTask = _api.GetFechamentosAsync();

        await Task.WhenAll(
            dashTask, catsTask, prodsTask, filaTask, todosPedidosTask,
            alunosTask, gruposTask, notifsTask, movsTask, fechsTask);

        ServidorOnline = true;
        TextoStatusConexao = $"Conectado • {_api.BaseUrl}";
        UltimaAtualizacaoTexto = DateTime.Now.ToString("HH:mm:ss", PtBr);

        var dash = await dashTask;
        if (dash is not null)
        {
            TotalProdutosAtivos = dash.TotalProdutosAtivos;
            ProdutosEmOferta = dash.ProdutosEmOferta;
            UnidadesEmEstoque = dash.UnidadesEmEstoque;
            PedidosPendentesFila = dash.PedidosPendentesFila;
            TotalAlunosCadastrados = dash.TotalAlunosCadastrados;
            ReceitaNormalHoje = dash.ReceitaNormalHoje;
            ReceitaRecuperadaHoje = dash.ReceitaRecuperadaHoje;
            ReceitaTotalHoje = dash.ReceitaTotalHoje;
            MetaReceitaRecuperadaDiaria = dash.MetaReceitaRecuperadaDiaria;
            PercentualMetaBarra = Math.Clamp((double)dash.PercentualMetaRecuperada, 0d, 100d);
            PercentualMetaTexto = $"{dash.PercentualMetaRecuperada:N1}% da meta";
            EconomiaGeradaAosAlunosHoje = dash.EconomiaGeradaAosAlunosHoje;
            HorarioProgramadoQueima = dash.HorarioProgramadoQueima;
            ModoIntegracaoWhatsApp = dash.ModoIntegracaoWhatsApp;

            SincronizarColecao(OfertasAtivas, dash.OfertasAtivas);
            SincronizarColecao(PrincipaisClientes, dash.PrincipaisClientes);
        }

        var idCatAnterior = CategoriaFormularioSelecionada?.Id;
        SincronizarColecao(Categorias, await catsTask);
        CategoriaFormularioSelecionada = idCatAnterior.HasValue
            ? Categorias.FirstOrDefault(c => c.Id == idCatAnterior.Value) ?? Categorias.FirstOrDefault()
            : Categorias.FirstOrDefault();

        var idProdSel = ProdutoSelecionado?.Id;
        var idProdBalcao = ProdutoBalcaoSelecionado?.Id;
        var produtos = await prodsTask;
        SincronizarColecao(Produtos, produtos);
        if (idProdSel.HasValue)
            ProdutoSelecionado = Produtos.FirstOrDefault(p => p.Id == idProdSel.Value);
        ProdutoBalcaoSelecionado = idProdBalcao.HasValue
            ? Produtos.FirstOrDefault(p => p.Id == idProdBalcao.Value) ?? Produtos.FirstOrDefault(p => p.Ativo && p.QuantidadeDisponivel > 0)
            : Produtos.FirstOrDefault(p => p.Ativo && p.QuantidadeDisponivel > 0);

        SincronizarColecao(PedidosFila, await filaTask);
        SincronizarColecao(HistoricoPedidos, await todosPedidosTask);
        SincronizarColecao(Alunos, await alunosTask);
        SincronizarColecao(GruposWhatsApp, await gruposTask);
        SincronizarColecao(NotificacoesWhatsApp, await notifsTask);
        SincronizarColecao(MovimentacoesEstoque, await movsTask);
        SincronizarColecao(Fechamentos, await fechsTask);
    }

    // --- Campanha Queima de Estoque 20:45 ---

    [RelayCommand]
    private async Task DispararQueimaEstoqueAsync()
    {
        await ExecutarAcaoSeguraAsync(async () =>
        {
            var percentual = ParseDecimal(PercentualQueimaInput, "Percentual da queima");
            var resultado = await _api.ExecutarQueimaEstoqueAsync(new ExecutarQueimaEstoqueRequest(
                PercentualDesconto: percentual,
                ApenasPereciveis: ApenasPereciveisQueima,
                GrupoWhatsAppId: null,
                DispararWhatsApp: DispararWhatsAppQueima));

            UltimaMensagemQueima = resultado.MensagemGerada;
            await CarregarDadosRemotosAsync();
            MostrarSucesso($"Queima aplicada em {resultado.ProdutosAfetados} produto(s)! Potencial recuperável: {resultado.ValorPotencialRecuperavel.ToString("C2", PtBr)}.");
        });
    }

    [RelayCommand]
    private void CopiarMensagemQueima()
    {
        if (string.IsNullOrWhiteSpace(UltimaMensagemQueima))
            return;

        Clipboard.SetText(UltimaMensagemQueima);
        MostrarSucesso("Mensagem promocional copiada para a área de transferência!");
    }

    // --- Fila de Pedidos Web & PDV Balcão ---

    [RelayCommand]
    private Task IniciarPreparoPedidoAsync(PedidoDto? pedido) =>
        AlterarStatusPedidoInternoAsync(pedido ?? PedidoFilaSelecionado, "EmPreparo", "Pedido enviado para preparo!");

    [RelayCommand]
    private Task MarcarProntoPedidoAsync(PedidoDto? pedido) =>
        AlterarStatusPedidoInternoAsync(pedido ?? PedidoFilaSelecionado, "ProntoParaRetirada", "Pedido marcado como PRONTO e notificação WhatsApp registrada!");

    [RelayCommand]
    private Task ConcluirRetiradaPedidoAsync(PedidoDto? pedido) =>
        AlterarStatusPedidoInternoAsync(pedido ?? PedidoFilaSelecionado, "Concluido", "Retirada concluída e estoque baixado com sucesso!");

    [RelayCommand]
    private Task CancelarPedidoAsync(PedidoDto? pedido) =>
        AlterarStatusPedidoInternoAsync(pedido ?? PedidoFilaSelecionado, "Cancelado", "Pedido cancelado e reserva devolvida ao estoque.");

    private async Task AlterarStatusPedidoInternoAsync(PedidoDto? alvo, string novoStatus, string msgSucesso)
    {
        if (alvo is null)
        {
            MostrarErro("Selecione um pedido na fila para atualizar o status.");
            return;
        }

        await ExecutarAcaoSeguraAsync(async () =>
        {
            var atualizado = await _api.AtualizarStatusPedidoAsync(alvo.Id, novoStatus);
            await CarregarDadosRemotosAsync();
            MostrarSucesso($"{msgSucesso} ({atualizado.CodigoRetirada})");

            if (AbrirWaMeAoNotificar && !string.IsNullOrWhiteSpace(atualizado.UltimoLinkWhatsApp))
            {
                AbrirUrlExterna(atualizado.UltimoLinkWhatsApp);
            }
        });
    }

    [RelayCommand]
    private void AbrirWhatsAppPedido(PedidoDto? pedido)
    {
        var alvo = pedido ?? PedidoFilaSelecionado;
        if (alvo is null || string.IsNullOrWhiteSpace(alvo.UltimoLinkWhatsApp))
        {
            MostrarErro("Este pedido não possui link de WhatsApp disponível.");
            return;
        }

        AbrirUrlExterna(alvo.UltimoLinkWhatsApp);
    }

    [RelayCommand]
    private async Task RegistrarVendaBalcaoAsync()
    {
        if (ProdutoBalcaoSelecionado is null)
        {
            MostrarErro("Selecione um produto para registrar a venda de balcão.");
            return;
        }

        await ExecutarAcaoSeguraAsync(async () =>
        {
            int qtd = ParseInt(QuantidadeBalcaoInput, "Quantidade");
            var pedido = await _api.RegistrarVendaBalcaoAsync(new RegistrarVendaBalcaoRequest(
                NomeCliente: string.IsNullOrWhiteSpace(ClienteBalcaoInput) ? null : ClienteBalcaoInput.Trim(),
                FormaPagamento: FormaPagamentoBalcao,
                Itens: [new CriarItemPedidoRequest(ProdutoBalcaoSelecionado.Id, qtd)]));

            QuantidadeBalcaoInput = "1";
            ClienteBalcaoInput = string.Empty;
            await CarregarDadosRemotosAsync();
            MostrarSucesso($"Venda de balcão {pedido.CodigoRetirada} registrada ({pedido.ValorTotal.ToString("C2", PtBr)})!");
        });
    }

    // --- Produtos, Estoque e Descontos ---

    [RelayCommand]
    private void LimparFormularioProduto()
    {
        ProdutoSelecionado = null;
        ProdutoEmEdicaoId = null;
        FormProdutoNome = string.Empty;
        FormProdutoDescricao = string.Empty;
        FormProdutoEmoji = "🥐";
        CategoriaFormularioSelecionada = Categorias.FirstOrDefault();
        FormProdutoPrecoCusto = "3,50";
        FormProdutoPrecoVenda = "7,00";
        FormProdutoEstoque = "15";
        FormProdutoPerecivel = true;
        FormProdutoAtivo = true;
    }

    [RelayCommand]
    private async Task SalvarProdutoAsync()
    {
        if (CategoriaFormularioSelecionada is null)
        {
            MostrarErro("Selecione uma categoria para o produto.");
            return;
        }

        await ExecutarAcaoSeguraAsync(async () =>
        {
            var req = new SalvarProdutoRequest(
                Nome: FormProdutoNome.Trim(),
                Descricao: FormProdutoDescricao.Trim(),
                IconeEmoji: string.IsNullOrWhiteSpace(FormProdutoEmoji) ? "🍽️" : FormProdutoEmoji.Trim(),
                CategoriaId: CategoriaFormularioSelecionada.Id,
                PrecoCusto: ParseDecimal(FormProdutoPrecoCusto, "Preço de custo"),
                PrecoVenda: ParseDecimal(FormProdutoPrecoVenda, "Preço de venda"),
                QuantidadeInicial: ParseInt(FormProdutoEstoque, "Estoque"),
                Perecivel: FormProdutoPerecivel,
                Ativo: FormProdutoAtivo);

            if (ProdutoEmEdicaoId.HasValue)
            {
                var atualizado = await _api.AtualizarProdutoAsync(ProdutoEmEdicaoId.Value, req);
                await CarregarDadosRemotosAsync();
                MostrarSucesso($"Produto '{atualizado.Nome}' atualizado!");
            }
            else
            {
                var criado = await _api.CriarProdutoAsync(req);
                LimparFormularioProduto();
                await CarregarDadosRemotosAsync();
                MostrarSucesso($"Produto '{criado.Nome}' cadastrado no cardápio web!");
            }
        });
    }

    [RelayCommand]
    private async Task ExcluirProdutoSelecionadoAsync()
    {
        if (ProdutoSelecionado is null)
        {
            MostrarErro("Selecione um produto na tabela para remover ou inativar.");
            return;
        }

        var nome = ProdutoSelecionado.Nome;
        var id = ProdutoSelecionado.Id;

        await ExecutarAcaoSeguraAsync(async () =>
        {
            await _api.ExcluirProdutoAsync(id);
            LimparFormularioProduto();
            await CarregarDadosRemotosAsync();
            MostrarSucesso($"Produto '{nome}' removido/inativado com segurança.");
        });
    }

    [RelayCommand]
    private async Task AjustarEstoqueRapidoAsync(string? deltaTexto)
    {
        if (ProdutoSelecionado is null)
        {
            MostrarErro("Selecione um produto na tabela para ajustar o estoque.");
            return;
        }

        if (!int.TryParse(deltaTexto, NumberStyles.Integer, CultureInfo.InvariantCulture, out var delta))
            return;

        await ExecutarAcaoSeguraAsync(async () =>
        {
            var atualizado = await _api.AjustarEstoqueAsync(ProdutoSelecionado.Id, delta, "Ajuste rápido no painel desktop");
            await CarregarDadosRemotosAsync();
            MostrarSucesso($"Estoque de '{atualizado.Nome}' atualizado para {atualizado.QuantidadeEstoque} un.");
        });
    }

    [RelayCommand]
    private async Task AjustarEstoqueManualAsync()
    {
        if (ProdutoSelecionado is null)
        {
            MostrarErro("Selecione um produto na tabela para ajustar o estoque.");
            return;
        }

        await ExecutarAcaoSeguraAsync(async () =>
        {
            int delta = ParseInt(DeltaEstoqueManualInput, "Variação de estoque");
            var atualizado = await _api.AjustarEstoqueAsync(ProdutoSelecionado.Id, delta, MotivoEstoqueManualInput);
            await CarregarDadosRemotosAsync();
            MostrarSucesso($"Estoque de '{atualizado.Nome}' ajustado para {atualizado.QuantidadeEstoque} un.");
        });
    }

    [RelayCommand]
    private async Task AplicarDescontoIndividualAsync()
    {
        if (ProdutoSelecionado is null)
        {
            MostrarErro("Selecione um produto na tabela para aplicar desconto individual.");
            return;
        }

        await ExecutarAcaoSeguraAsync(async () =>
        {
            var req = ConstruirDescontoRequest();
            var atualizado = await _api.AplicarDescontoProdutoAsync(ProdutoSelecionado.Id, req);
            await CarregarDadosRemotosAsync();
            MostrarSucesso($"Oferta aplicada em '{atualizado.Nome}' -> novo preço {atualizado.PrecoFinal.ToString("C2", PtBr)}!");
        });
    }

    [RelayCommand]
    private async Task ZerarDescontoIndividualAsync()
    {
        if (ProdutoSelecionado is null)
        {
            MostrarErro("Selecione um produto na tabela para restaurar o preço cheio.");
            return;
        }

        await ExecutarAcaoSeguraAsync(async () =>
        {
            var atualizado = await _api.ZerarDescontoProdutoAsync(ProdutoSelecionado.Id);
            await CarregarDadosRemotosAsync();
            MostrarSucesso($"Preço original de '{atualizado.Nome}' restaurado ({atualizado.PrecoVenda.ToString("C2", PtBr)}).");
        });
    }

    [RelayCommand]
    private async Task AplicarDescontoLoteAsync()
    {
        await ExecutarAcaoSeguraAsync(async () =>
        {
            var req = ConstruirDescontoRequest();
            await _api.AplicarDescontoLoteAsync(req);
            await CarregarDadosRemotosAsync();
            MostrarSucesso("Desconto em lote aplicado no cardápio!");
        });
    }

    [RelayCommand]
    private async Task ZerarTodosDescontosAsync()
    {
        await ExecutarAcaoSeguraAsync(async () =>
        {
            await _api.ZerarDescontoLoteAsync();
            await CarregarDadosRemotosAsync();
            MostrarSucesso("Todos os descontos promocionais foram encerrados.");
        });
    }

    private AplicarDescontoRequest ConstruirDescontoRequest()
    {
        decimal? perc = string.IsNullOrWhiteSpace(DescontoPercentualInput)
            ? null
            : ParseDecimal(DescontoPercentualInput, "Desconto percentual");

        decimal? fixo = string.IsNullOrWhiteSpace(DescontoValorFixoInput)
            ? null
            : ParseDecimal(DescontoValorFixoInput, "Desconto em R$");

        if ((!perc.HasValue || perc.Value <= 0m) && (!fixo.HasValue || fixo.Value <= 0m))
        {
            throw new InvalidOperationException("Informe um desconto percentual (%) ou um valor fixo em R$ maior que zero.");
        }

        // Se o operador preencheu desconto fixo em R$, prioriza o valor fixo; caso contrário usa percentual
        if (fixo.HasValue && fixo.Value > 0m)
        {
            return new AplicarDescontoRequest(null, fixo.Value, DescontoApenasPereciveis);
        }

        return new AplicarDescontoRequest(perc, null, DescontoApenasPereciveis);
    }

    // --- Grupos WhatsApp, Alunos & Logs ---

    [RelayCommand]
    private void NovoGrupo()
    {
        GrupoSelecionado = null;
        FormGrupoId = null;
        FormGrupoNome = string.Empty;
        FormGrupoTurno = "Noite";
        FormGrupoLink = string.Empty;
        FormGrupoJid = string.Empty;
        FormGrupoParticipantes = "50";
        FormGrupoAtivo = true;
    }

    [RelayCommand]
    private async Task SalvarGrupoAsync()
    {
        await ExecutarAcaoSeguraAsync(async () =>
        {
            var req = new SalvarGrupoWhatsAppRequest(
                Nome: FormGrupoNome.Trim(),
                Turno: FormGrupoTurno,
                LinkConvite: FormGrupoLink.Trim(),
                ParticipantesEstimados: ParseInt(FormGrupoParticipantes, "Participantes estimados"),
                DestinoApiJid: string.IsNullOrWhiteSpace(FormGrupoJid) ? null : FormGrupoJid.Trim(),
                Ativo: FormGrupoAtivo);

            var salvo = await _api.SalvarGrupoWhatsAppAsync(FormGrupoId, req);
            NovoGrupo();
            await CarregarDadosRemotosAsync();
            MostrarSucesso($"Grupo '{salvo.Nome}' salvo com sucesso!");
        });
    }

    [RelayCommand]
    private void AbrirLinkGrupo(GrupoWhatsAppDto? grupo)
    {
        var alvo = grupo ?? GrupoSelecionado;
        if (alvo is null || string.IsNullOrWhiteSpace(alvo.LinkConvite))
            return;

        AbrirUrlExterna(alvo.LinkConvite);
    }

    [RelayCommand]
    private async Task CadastrarAlunoAsync()
    {
        await ExecutarAcaoSeguraAsync(async () =>
        {
            var salvo = await _api.CriarAlunoAsync(new SalvarAlunoRequest(
                Nome: FormAlunoNome.Trim(),
                Email: FormAlunoEmail.Trim(),
                TelefoneWhatsApp: FormAlunoTelefone.Trim(),
                Matricula: FormAlunoMatricula.Trim(),
                Turno: FormAlunoTurno,
                ReceberAlertasPromocao: FormAlunoAlertas));

            FormAlunoNome = string.Empty;
            FormAlunoEmail = string.Empty;
            FormAlunoTelefone = string.Empty;
            FormAlunoMatricula = string.Empty;
            await CarregarDadosRemotosAsync();
            MostrarSucesso($"Aluno '{salvo.Nome}' cadastrado!");
        });
    }

    [RelayCommand]
    private async Task ExcluirAlunoAsync(AlunoDto? aluno)
    {
        if (aluno is null)
            return;

        await ExecutarAcaoSeguraAsync(async () =>
        {
            await _api.ExcluirAlunoAsync(aluno.Id);
            await CarregarDadosRemotosAsync();
            MostrarSucesso($"Cadastro de '{aluno.Nome}' removido.");
        });
    }

    [RelayCommand]
    private void AbrirLinkNotificacao(NotificacaoWhatsAppDto? notificacao)
    {
        if (notificacao is null || string.IsNullOrWhiteSpace(notificacao.LinkFallbackWaMe))
            return;

        AbrirUrlExterna(notificacao.LinkFallbackWaMe);
    }

    // --- Fechamento Diário & Conexão Nuvem ---

    [RelayCommand]
    private async Task GerarFechamentoDiarioAsync()
    {
        await ExecutarAcaoSeguraAsync(async () =>
        {
            var fechamento = await _api.GerarFechamentoAsync(ObservacoesFechamentoInput);
            ObservacoesFechamentoInput = string.Empty;
            await CarregarDadosRemotosAsync();
            MostrarSucesso($"Fechamento de {fechamento.DataReferencia:dd/MM/yyyy} consolidado! Receita total: {fechamento.ReceitaTotal.ToString("C2", PtBr)}.");
        });
    }

    [RelayCommand]
    private async Task SalvarConfiguracaoServidorAsync()
    {
        if (string.IsNullOrWhiteSpace(ApiBaseUrlConfig))
        {
            MostrarErro("Informe a URL do servidor local ou na nuvem (ex.: https://cantina-senac.onrender.com).");
            return;
        }

        _settings.ApiBaseUrl = ApiBaseUrlConfig.Trim().TrimEnd('/');
        _settings.AdminApiKey = AdminApiKeyConfig.Trim();
        _settings.AutoStartLocalServer = AutoStartLocalConfig;
        _settings.Save();
        _api.UpdateSettings(_settings);

        await ExecutarAcaoSeguraAsync(async () =>
        {
            await CarregarDadosRemotosAsync();
            MostrarSucesso($"Conectado com sucesso ao servidor {_settings.ApiBaseUrl}!");
        });
    }

    [RelayCommand]
    private void AbrirSitePedidos()
    {
        AbrirUrlExterna(_api.BaseUrl);
    }

    // --- Utilitários ---

    private async Task ExecutarAcaoSeguraAsync(Func<Task> acao)
    {
        try
        {
            EstaCarregando = true;
            await acao();
        }
        catch (Exception ex)
        {
            ServidorOnline = await _api.CheckHealthAsync();
            if (!ServidorOnline)
            {
                TextoStatusConexao = $"Offline ({_api.BaseUrl})";
            }
            MostrarErro(ex.Message);
        }
        finally
        {
            EstaCarregando = false;
        }
    }

    private void MostrarSucesso(string msg)
    {
        FeedbackErro = false;
        MensagemFeedback = msg;
    }

    private void MostrarErro(string msg)
    {
        FeedbackErro = true;
        MensagemFeedback = msg;
    }

    private static decimal ParseDecimal(string texto, string nomeCampo)
    {
        var normalizado = (texto ?? string.Empty).Trim();
        if (decimal.TryParse(normalizado, NumberStyles.Number, PtBr, out var valorBr))
            return valorBr;

        if (decimal.TryParse(normalizado, NumberStyles.Number, CultureInfo.InvariantCulture, out var valorInv))
            return valorInv;

        throw new InvalidOperationException($"Valor inválido para '{nomeCampo}'. Exemplo válido: 6,50.");
    }

    private static int ParseInt(string texto, string nomeCampo)
    {
        if (int.TryParse((texto ?? string.Empty).Trim(), NumberStyles.Integer, PtBr, out var valor))
            return valor;

        throw new InvalidOperationException($"Número inteiro inválido para '{nomeCampo}'.");
    }

    private static void SincronizarColecao<T>(ObservableCollection<T> destino, IEnumerable<T> novosItens)
    {
        destino.Clear();
        foreach (var item in novosItens)
        {
            destino.Add(item);
        }
    }

    private static void AbrirUrlExterna(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch
        {
            // Ignora falha caso não haja navegador associado no ambiente
        }
    }

    public void Dispose()
    {
        _pollTimer.Stop();
    }
}
