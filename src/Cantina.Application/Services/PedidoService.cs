using Cantina.Application.Abstractions;
using Cantina.Application.Formatting;
using Cantina.Application.Options;
using Cantina.Contracts.Dtos;
using Cantina.Domain.Entities;
using Cantina.Domain.Enums;
using Cantina.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cantina.Application.Services;

public interface IPedidoService
{
    Task<PedidoDto> CriarPedidoWebAsync(CriarPedidoWebRequest req, CancellationToken ct = default);
    Task<PedidoDto> RegistrarVendaBalcaoAsync(RegistrarVendaBalcaoRequest req, CancellationToken ct = default);
    Task<PedidoDto?> ObterPorCodigoAsync(string codigoRetirada, CancellationToken ct = default);
    Task<IReadOnlyList<PedidoDto>> ListarPedidosAsync(bool apenasFilaAtiva = false, CancellationToken ct = default);
    Task<PedidoDto> AtualizarStatusAsync(int pedidoId, AtualizarStatusPedidoRequest req, CancellationToken ct = default);
}

public sealed class PedidoService : IPedidoService
{
    private readonly ICantinaDbContext _db;
    private readonly IWhatsAppGateway _whatsApp;
    private readonly CantinaOperacaoOptions _options;
    private readonly ILogger<PedidoService> _logger;

    public PedidoService(
        ICantinaDbContext db,
        IWhatsAppGateway whatsApp,
        IOptions<CantinaOperacaoOptions> options,
        ILogger<PedidoService>? logger = null)
    {
        _db = db;
        _whatsApp = whatsApp;
        _options = options.Value;
        _logger = logger ?? NullLogger<PedidoService>.Instance;
    }

    public async Task<PedidoDto> CriarPedidoWebAsync(CriarPedidoWebRequest req, CancellationToken ct = default)
    {
        if (req.Itens is null || req.Itens.Count == 0)
            throw new DomainException("Selecione ao menos um item do cardápio para enviar o pedido.");

        var idsProdutos = req.Itens.Select(i => i.ProdutoId).Distinct().ToList();
        var produtos = await _db.Produtos
            .Where(p => idsProdutos.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);

        var itensDominio = new List<ItemPedido>();
        var movimentacoes = new List<MovimentacaoEstoque>();

        foreach (var itemReq in req.Itens.GroupBy(i => i.ProdutoId))
        {
            int qtdTotal = itemReq.Sum(x => x.Quantidade);
            if (!produtos.TryGetValue(itemReq.Key, out var produto))
                throw new DomainException($"Produto #{itemReq.Key} não encontrado.");

            produto.Reservar(qtdTotal);
            itensDominio.Add(new ItemPedido(produto, qtdTotal));
            movimentacoes.Add(new MovimentacaoEstoque(
                produto,
                TipoMovimentacaoEstoque.ReservaPedido,
                0,
                $"Reserva de {qtdTotal} un. via site por {req.NomeCliente}"));
        }

        var turno = DtoMapper.ParseTurno(req.Turno);
        var pagamento = DtoMapper.ParseFormaPagamento(req.FormaPagamento);

        var pedido = new Pedido(
            OrigemPedido.SiteWeb,
            req.NomeCliente,
            req.TelefoneWhatsApp,
            req.MatriculaAluno,
            turno,
            pagamento,
            req.Observacoes,
            itensDominio);

        _db.Pedidos.Add(pedido);
        _db.MovimentacoesEstoque.AddRange(movimentacoes);
        await SalvarPedidoComCodigoUnicoAsync(pedido, ct);

        string msgConfirmacao = WhatsAppMessageFormatter.FormatarConfirmacaoPedido(
            pedido,
            _options.NomeCantina,
            _options.UrlPublicaCardapio);

        ResultadoEnvioWhatsApp envio;
        try
        {
            envio = await _whatsApp.EnviarMensagemIndividualAsync(pedido.TelefoneWhatsApp, msgConfirmacao, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Falha ao notificar WhatsApp de confirmação do pedido {PedidoId} (#{CodigoRetirada})",
                pedido.Id,
                pedido.CodigoRetirada);

            envio = new ResultadoEnvioWhatsApp(
                CanalEnvioWhatsApp.OutboxWaMeFallback,
                false,
                WhatsAppMessageFormatter.GerarLinkWaMe(pedido.TelefoneWhatsApp, msgConfirmacao),
                $"Falha de rede ao disparar gateway: {ex.Message}");
        }

        try
        {
            _db.NotificacoesWhatsApp.Add(new NotificacaoWhatsAppLog(
                TipoNotificacaoWhatsApp.ConfirmacaoPedido,
                envio.CanalUtilizado,
                pedido.TelefoneWhatsApp,
                msgConfirmacao,
                envio.EnviadoAutomaticamente,
                envio.LinkFallbackWaMe,
                envio.DetalheStatus,
                pedido.Id));

            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Falha ao persistir log de notificação WhatsApp do pedido {PedidoId}",
                pedido.Id);
        }

        return pedido.ToDto(envio.LinkFallbackWaMe);
    }

    public async Task<PedidoDto> RegistrarVendaBalcaoAsync(RegistrarVendaBalcaoRequest req, CancellationToken ct = default)
    {
        if (req.Itens is null || req.Itens.Count == 0)
            throw new DomainException("Adicione ao menos um produto para concluir a venda de balcão.");

        var idsProdutos = req.Itens.Select(i => i.ProdutoId).Distinct().ToList();
        var produtos = await _db.Produtos
            .Where(p => idsProdutos.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);

        var itensDominio = new List<ItemPedido>();
        var movimentacoes = new List<MovimentacaoEstoque>();

        foreach (var itemReq in req.Itens.GroupBy(i => i.ProdutoId))
        {
            int qtdTotal = itemReq.Sum(x => x.Quantidade);
            if (!produtos.TryGetValue(itemReq.Key, out var produto))
                throw new DomainException($"Produto #{itemReq.Key} não encontrado.");

            produto.BaixarVendaDireta(qtdTotal);
            itensDominio.Add(new ItemPedido(produto, qtdTotal));
            movimentacoes.Add(new MovimentacaoEstoque(
                produto,
                TipoMovimentacaoEstoque.VendaBalcao,
                -qtdTotal,
                $"Venda direta no balcão ({qtdTotal} un.)"));
        }

        var pagamento = DtoMapper.ParseFormaPagamento(req.FormaPagamento);
        var nomeCliente = string.IsNullOrWhiteSpace(req.NomeCliente) ? "Cliente Balcão" : req.NomeCliente;

        var pedido = new Pedido(
            OrigemPedido.BalcaoDesktop,
            nomeCliente,
            telefoneWhatsApp: null,
            matriculaAluno: null,
            TurnoSenac.Noite,
            pagamento,
            observacoes: "Venda rápida registrada no PDV Desktop",
            itensDominio);

        _db.Pedidos.Add(pedido);
        _db.MovimentacoesEstoque.AddRange(movimentacoes);
        await SalvarPedidoComCodigoUnicoAsync(pedido, ct);

        return pedido.ToDto();
    }

    public async Task<PedidoDto?> ObterPorCodigoAsync(string codigoRetirada, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(codigoRetirada))
            return null;

        string codigoNormalizado = codigoRetirada.Trim().TrimStart('#').ToUpperInvariant();
        var pedido = await _db.Pedidos
            .Include(p => p.Itens)
            .FirstOrDefaultAsync(p => p.CodigoRetirada == codigoNormalizado, ct);

        return pedido?.ToDto();
    }

    public async Task<IReadOnlyList<PedidoDto>> ListarPedidosAsync(bool apenasFilaAtiva = false, CancellationToken ct = default)
    {
        var query = _db.Pedidos.Include(p => p.Itens).AsQueryable();

        if (apenasFilaAtiva)
        {
            query = query.Where(p =>
                p.Status == StatusPedido.Recebido ||
                p.Status == StatusPedido.EmPreparo ||
                p.Status == StatusPedido.ProntoParaRetirada);
        }

        var lista = await query
            .OrderByDescending(p => p.CriadoEm)
            .Take(100)
            .ToListAsync(ct);

        return lista.Select(p => p.ToDto()).ToList();
    }

    public async Task<PedidoDto> AtualizarStatusAsync(int pedidoId, AtualizarStatusPedidoRequest req, CancellationToken ct = default)
    {
        var novoStatus = DtoMapper.ParseStatusPedido(req.NovoStatus);

        var pedido = await _db.Pedidos
            .Include(p => p.Itens)
            .FirstOrDefaultAsync(p => p.Id == pedidoId, ct)
            ?? throw new DomainException("Pedido não encontrado.");

        var idsProdutos = pedido.Itens.Select(i => i.ProdutoId).Distinct().ToList();
        var produtos = await _db.Produtos
            .Where(p => idsProdutos.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);

        string? mensagemNotificacao = null;
        TipoNotificacaoWhatsApp? tipoNotificacao = null;

        switch (novoStatus)
        {
            case StatusPedido.EmPreparo:
                pedido.IniciarPreparo();
                mensagemNotificacao = WhatsAppMessageFormatter.FormatarPedidoEmPreparo(pedido, _options.NomeCantina);
                tipoNotificacao = TipoNotificacaoWhatsApp.PedidoEmPreparo;
                break;

            case StatusPedido.ProntoParaRetirada:
                pedido.MarcarProntoParaRetirada();
                mensagemNotificacao = WhatsAppMessageFormatter.FormatarPedidoPronto(pedido, _options.NomeCantina);
                tipoNotificacao = TipoNotificacaoWhatsApp.PedidoProntoRetirada;
                break;

            case StatusPedido.Concluido:
                if (pedido.ReservaEstoqueAtiva)
                {
                    foreach (var item in pedido.Itens)
                    {
                        if (produtos.TryGetValue(item.ProdutoId, out var prod))
                        {
                            prod.ConfirmarSaidaReserva(item.Quantidade);
                            _db.MovimentacoesEstoque.Add(new MovimentacaoEstoque(
                                prod,
                                TipoMovimentacaoEstoque.BaixaEntregaPedido,
                                -item.Quantidade,
                                $"Retirada confirmada do pedido #{pedido.CodigoRetirada}"));
                        }
                    }
                }
                pedido.ConcluirEntrega();
                break;

            case StatusPedido.Cancelado:
                if (pedido.ReservaEstoqueAtiva)
                {
                    foreach (var item in pedido.Itens)
                    {
                        if (produtos.TryGetValue(item.ProdutoId, out var prod))
                        {
                            prod.LiberarReserva(item.Quantidade);
                            _db.MovimentacoesEstoque.Add(new MovimentacaoEstoque(
                                prod,
                                TipoMovimentacaoEstoque.EstornoCancelamento,
                                0,
                                $"Reserva liberada pelo cancelamento do pedido #{pedido.CodigoRetirada}"));
                        }
                    }
                }
                pedido.Cancelar();
                mensagemNotificacao = WhatsAppMessageFormatter.FormatarPedidoCancelado(pedido, _options.NomeCantina);
                tipoNotificacao = TipoNotificacaoWhatsApp.PedidoCancelado;
                break;

            default:
                throw new DomainException($"Transição para '{novoStatus}' não é suportada.");
        }

        // 1. Persiste a transição do pedido e a movimentação de estoque primeiro (elimina Dual Write)
        await _db.SaveChangesAsync(ct);

        // 2. Dispara a notificação externa apenas pós-commit, isolando falhas de rede em log
        string? linkWaMe = null;
        if (tipoNotificacao.HasValue &&
            !string.IsNullOrWhiteSpace(mensagemNotificacao) &&
            !string.IsNullOrWhiteSpace(pedido.TelefoneWhatsApp))
        {
            ResultadoEnvioWhatsApp envio;
            try
            {
                envio = await _whatsApp.EnviarMensagemIndividualAsync(pedido.TelefoneWhatsApp, mensagemNotificacao, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Falha ao notificar WhatsApp do pedido {PedidoId} (#{CodigoRetirada}) para o status {NovoStatus}",
                    pedido.Id,
                    pedido.CodigoRetirada,
                    novoStatus);

                envio = new ResultadoEnvioWhatsApp(
                    CanalEnvioWhatsApp.OutboxWaMeFallback,
                    false,
                    WhatsAppMessageFormatter.GerarLinkWaMe(pedido.TelefoneWhatsApp, mensagemNotificacao),
                    $"Falha de rede ao disparar gateway: {ex.Message}");
            }

            linkWaMe = envio.LinkFallbackWaMe;

            try
            {
                _db.NotificacoesWhatsApp.Add(new NotificacaoWhatsAppLog(
                    tipoNotificacao.Value,
                    envio.CanalUtilizado,
                    pedido.TelefoneWhatsApp,
                    mensagemNotificacao,
                    envio.EnviadoAutomaticamente,
                    envio.LinkFallbackWaMe,
                    envio.DetalheStatus,
                    pedido.Id));

                await _db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Falha ao persistir log de notificação WhatsApp do pedido {PedidoId}",
                    pedido.Id);
            }
        }

        return pedido.ToDto(linkWaMe);
    }

    private async Task SalvarPedidoComCodigoUnicoAsync(Pedido pedido, CancellationToken ct)
    {
        const int maxTentativas = 5;

        for (int tentativa = 1; tentativa <= maxTentativas; tentativa++)
        {
            await GarantirCodigoRetiradaUnicoAsync(pedido, ct);

            try
            {
                await _db.SaveChangesAsync(ct);
                return;
            }
            catch (DbUpdateException ex) when (ex is not DbUpdateConcurrencyException && tentativa < maxTentativas)
            {
                _logger.LogWarning(
                    ex,
                    "Colisão concorrente detectada ao persistir CodigoRetirada {CodigoRetirada} (tentativa {Tentativa}/{MaxTentativas}). Regenerando código.",
                    pedido.CodigoRetirada,
                    tentativa,
                    maxTentativas);

                pedido.RegenerarCodigoRetirada();
            }
        }
    }

    private async Task GarantirCodigoRetiradaUnicoAsync(Pedido pedido, CancellationToken ct)
    {
        while (await _db.Pedidos.AnyAsync(p => p.CodigoRetirada == pedido.CodigoRetirada, ct))
        {
            pedido.RegenerarCodigoRetirada();
        }
    }
}

