namespace Cantina.Contracts.Dtos;

public sealed record CategoriaDto(
    int Id,
    string Nome,
    string IconeEmoji,
    bool PerecivelDiario);

public sealed record ProdutoDto(
    int Id,
    int CategoriaId,
    string CategoriaNome,
    string Nome,
    string Descricao,
    string IconeEmoji,
    decimal PrecoCusto,
    decimal PrecoVenda,
    decimal DescontoPercentual,
    decimal DescontoValor,
    decimal PrecoFinal,
    decimal EconomiaUnitaria,
    decimal PercentualDescontoEfetivo,
    bool TemDesconto,
    int QuantidadeEstoque,
    int QuantidadeReservada,
    int QuantidadeDisponivel,
    bool Perecivel,
    bool Ativo);

public sealed record SalvarProdutoRequest(
    string Nome,
    string Descricao,
    string IconeEmoji,
    int CategoriaId,
    decimal PrecoCusto,
    decimal PrecoVenda,
    int QuantidadeInicial,
    bool Perecivel = true,
    bool Ativo = true);

public sealed record AjustarEstoqueRequest(
    int Delta,
    string? Motivo = null);

public sealed record AplicarDescontoRequest(
    decimal? Percentual,
    decimal? ValorFixo,
    bool ApenasPereciveis = false);

public sealed record ExecutarQueimaEstoqueRequest(
    decimal PercentualDesconto = 40m,
    bool ApenasPereciveis = true,
    int? GrupoWhatsAppId = null,
    bool DispararWhatsApp = true);

public sealed record ResultadoQueimaEstoqueDto(
    int ProdutosAfetados,
    decimal ValorPotencialRecuperavel,
    string MensagemGerada,
    IReadOnlyList<NotificacaoWhatsAppDto> DisparosRealizados);

public sealed record ItemPedidoDto(
    int ProdutoId,
    string NomeProduto,
    int Quantidade,
    decimal PrecoUnitarioOriginal,
    decimal PrecoUnitarioFinal,
    decimal SubtotalFinal,
    bool ItemPromocional);

public sealed record PedidoDto(
    int Id,
    string CodigoRetirada,
    string Origem,
    string Status,
    string NomeCliente,
    string TelefoneWhatsApp,
    string? MatriculaAluno,
    string Turno,
    string FormaPagamento,
    string? Observacoes,
    decimal SubtotalOriginal,
    decimal DescontoTotal,
    decimal ValorTotal,
    decimal ReceitaRecuperada,
    decimal ReceitaNormal,
    DateTime CriadoEm,
    DateTime? ProntoEm,
    DateTime? ConcluidoEm,
    IReadOnlyList<ItemPedidoDto> Itens,
    string? UltimoLinkWhatsApp);

public sealed record CriarItemPedidoRequest(
    int ProdutoId,
    int Quantidade);

public sealed record CriarPedidoWebRequest(
    string NomeCliente,
    string TelefoneWhatsApp,
    string? MatriculaAluno,
    string Turno,
    string FormaPagamento,
    string? Observacoes,
    IReadOnlyList<CriarItemPedidoRequest> Itens);

public sealed record RegistrarVendaBalcaoRequest(
    string? NomeCliente,
    string FormaPagamento,
    IReadOnlyList<CriarItemPedidoRequest> Itens);

public sealed record AtualizarStatusPedidoRequest(
    string NovoStatus);

public sealed record GrupoWhatsAppDto(
    int Id,
    string Nome,
    string Turno,
    string LinkConvite,
    string? DestinoApiJid,
    int ParticipantesEstimados,
    bool Ativo);

public sealed record SalvarGrupoWhatsAppRequest(
    string Nome,
    string Turno,
    string LinkConvite,
    int ParticipantesEstimados,
    string? DestinoApiJid = null,
    bool Ativo = true);

public sealed record AlunoDto(
    int Id,
    string Nome,
    string Email,
    string TelefoneWhatsApp,
    string Matricula,
    string Turno,
    bool ReceberAlertasPromocao,
    DateTime CriadoEm);

public sealed record SalvarAlunoRequest(
    string Nome,
    string Email,
    string? TelefoneWhatsApp,
    string? Matricula,
    string Turno,
    bool ReceberAlertasPromocao = true);

public sealed record NotificacaoWhatsAppDto(
    int Id,
    string Tipo,
    string Canal,
    string Destinatario,
    string Mensagem,
    bool EnviadoAutomaticamente,
    string LinkFallbackWaMe,
    string DetalheStatus,
    int? PedidoId,
    DateTime CriadaEm);

public sealed record MovimentacaoEstoqueDto(
    int Id,
    int ProdutoId,
    string NomeProduto,
    string Tipo,
    int QuantidadeDelta,
    int SaldoPosterior,
    string Motivo,
    DateTime OcorridaEm);

public sealed record FechamentoDiarioDto(
    int Id,
    DateOnly DataReferencia,
    decimal ReceitaNormal,
    decimal ReceitaRecuperada,
    decimal ReceitaTotal,
    decimal DescontosConcedidos,
    int PedidosWebConcluidos,
    int VendasBalcaoConcluidas,
    int ItensVendidosPrecoCheio,
    int ItensVendidosPromocao,
    int ItensSobraramEstoque,
    string? Observacoes,
    DateTime FechadoEm);

public sealed record GerarFechamentoRequest(
    string? Observacoes);

public sealed record ClienteDestaqueDto(
    string Nome,
    string Turno,
    int TotalPedidos,
    decimal TotalGasto);

public sealed record DashboardResumoDto(
    int TotalProdutosAtivos,
    int ProdutosEmOferta,
    int UnidadesEmEstoque,
    int PedidosPendentesFila,
    int TotalAlunosCadastrados,
    decimal ReceitaNormalHoje,
    decimal ReceitaRecuperadaHoje,
    decimal ReceitaTotalHoje,
    decimal MetaReceitaRecuperadaDiaria,
    decimal PercentualMetaRecuperada,
    decimal EconomiaGeradaAosAlunosHoje,
    string HorarioProgramadoQueima,
    string ModoIntegracaoWhatsApp,
    IReadOnlyList<ClienteDestaqueDto> PrincipaisClientes,
    IReadOnlyList<ProdutoDto> OfertasAtivas,
    IReadOnlyList<GrupoWhatsAppDto> GruposWhatsApp);

public sealed record CardapioPublicoDto(
    string NomeCantina,
    string HorarioQueima,
    bool ModoAntidesperdicioAtivo,
    IReadOnlyList<CategoriaDto> Categorias,
    IReadOnlyList<ProdutoDto> Produtos,
    IReadOnlyList<GrupoWhatsAppDto> GruposWhatsApp);

