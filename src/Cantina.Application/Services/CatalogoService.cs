using Cantina.Application.Abstractions;
using Cantina.Application.Formatting;
using Cantina.Application.Options;
using Cantina.Contracts.Dtos;
using Cantina.Domain.Entities;
using Cantina.Domain.Enums;
using Cantina.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Cantina.Application.Services;

public interface ICatalogoService
{
    Task<CardapioPublicoDto> ObterCardapioPublicoAsync(CancellationToken ct = default);
    Task<IReadOnlyList<CategoriaDto>> ListarCategoriasAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ProdutoDto>> ListarProdutosAsync(bool apenasAtivos = false, CancellationToken ct = default);
    Task<ProdutoDto> CriarProdutoAsync(SalvarProdutoRequest req, CancellationToken ct = default);
    Task<ProdutoDto> AtualizarProdutoAsync(int id, SalvarProdutoRequest req, CancellationToken ct = default);
    Task RemoverProdutoAsync(int id, CancellationToken ct = default);
    Task<ProdutoDto> AjustarEstoqueAsync(int id, AjustarEstoqueRequest req, CancellationToken ct = default);
    Task<ProdutoDto> AplicarDescontoProdutoAsync(int id, AplicarDescontoRequest req, CancellationToken ct = default);
    Task<IReadOnlyList<ProdutoDto>> AplicarDescontoLoteAsync(AplicarDescontoRequest req, CancellationToken ct = default);
    Task<ProdutoDto> ZerarDescontoProdutoAsync(int id, CancellationToken ct = default);
    Task ZerarTodosDescontosAsync(CancellationToken ct = default);
    Task<ResultadoQueimaEstoqueDto> ExecutarQueimaEstoqueAsync(ExecutarQueimaEstoqueRequest req, CancellationToken ct = default);
}

public sealed class CatalogoService : ICatalogoService
{
    private readonly ICantinaDbContext _db;
    private readonly IWhatsAppGateway _whatsApp;
    private readonly CantinaOperacaoOptions _options;

    public CatalogoService(
        ICantinaDbContext db,
        IWhatsAppGateway whatsApp,
        IOptions<CantinaOperacaoOptions> options)
    {
        _db = db;
        _whatsApp = whatsApp;
        _options = options.Value;
    }

    public async Task<CardapioPublicoDto> ObterCardapioPublicoAsync(CancellationToken ct = default)
    {
        var categorias = await _db.Categorias
            .OrderBy(c => c.OrdemExibicao)
            .ThenBy(c => c.Nome)
            .ToListAsync(ct);

        var produtos = await _db.Produtos
            .Include(p => p.Categoria)
            .Where(p => p.Ativo)
            .OrderByDescending(p => p.DescontoPercentual > 0 || p.DescontoValor > 0)
            .ThenBy(p => p.CategoriaId)
            .ThenBy(p => p.Nome)
            .ToListAsync(ct);

        var grupos = await _db.GruposWhatsApp
            .Where(g => g.Ativo)
            .OrderBy(g => g.Turno)
            .ToListAsync(ct);

        bool modoAntidesperdicio = produtos.Any(p => p.TemDesconto && p.QuantidadeDisponivel > 0);

        return new CardapioPublicoDto(
            _options.NomeCantina,
            _options.HorarioQueimaAutomatica,
            modoAntidesperdicio,
            categorias.Select(c => c.ToDto()).ToList(),
            produtos.Select(p => p.ToDto()).ToList(),
            grupos.Select(g => g.ToDto()).ToList());
    }

    public async Task<IReadOnlyList<CategoriaDto>> ListarCategoriasAsync(CancellationToken ct = default)
    {
        var lista = await _db.Categorias
            .OrderBy(c => c.OrdemExibicao)
            .ThenBy(c => c.Nome)
            .ToListAsync(ct);

        return lista.Select(c => c.ToDto()).ToList();
    }

    public async Task<IReadOnlyList<ProdutoDto>> ListarProdutosAsync(bool apenasAtivos = false, CancellationToken ct = default)
    {
        var query = _db.Produtos.Include(p => p.Categoria).AsQueryable();
        if (apenasAtivos)
        {
            query = query.Where(p => p.Ativo);
        }

        var lista = await query
            .OrderBy(p => p.CategoriaId)
            .ThenBy(p => p.Nome)
            .ToListAsync(ct);

        return lista.Select(p => p.ToDto()).ToList();
    }

    public async Task<ProdutoDto> CriarProdutoAsync(SalvarProdutoRequest req, CancellationToken ct = default)
    {
        var categoria = await _db.Categorias.FirstOrDefaultAsync(c => c.Id == req.CategoriaId, ct)
            ?? throw new DomainException("Categoria informada não encontrada.");

        var produto = new Produto(
            req.Nome,
            req.Descricao,
            req.IconeEmoji,
            categoria.Id,
            req.PrecoCusto,
            req.PrecoVenda,
            req.QuantidadeInicial,
            req.Perecivel);

        if (!req.Ativo)
        {
            produto.AtualizarDados(
                req.Nome,
                req.Descricao,
                req.IconeEmoji,
                categoria.Id,
                req.PrecoCusto,
                req.PrecoVenda,
                req.Perecivel,
                ativo: false);
        }

        _db.Produtos.Add(produto);
        await _db.SaveChangesAsync(ct);

        if (req.QuantidadeInicial > 0)
        {
            _db.MovimentacoesEstoque.Add(new MovimentacaoEstoque(
                produto,
                TipoMovimentacaoEstoque.EntradaProducao,
                req.QuantidadeInicial,
                "Estoque inicial no cadastro"));
            await _db.SaveChangesAsync(ct);
        }

        await _db.Produtos.Entry(produto).Reference(p => p.Categoria).LoadAsync(ct);
        return produto.ToDto();
    }

    public async Task<ProdutoDto> AtualizarProdutoAsync(int id, SalvarProdutoRequest req, CancellationToken ct = default)
    {
        var produto = await _db.Produtos
            .Include(p => p.Categoria)
            .FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new DomainException("Produto não encontrado.");

        bool categoriaExiste = await _db.Categorias.AnyAsync(c => c.Id == req.CategoriaId, ct);
        if (!categoriaExiste)
            throw new DomainException("Categoria informada não encontrada.");

        produto.AtualizarDados(
            req.Nome,
            req.Descricao,
            req.IconeEmoji,
            req.CategoriaId,
            req.PrecoCusto,
            req.PrecoVenda,
            req.Perecivel,
            req.Ativo);

        int deltaEstoque = req.QuantidadeInicial - produto.QuantidadeEstoque;
        if (deltaEstoque != 0)
        {
            produto.AjustarEstoqueFisico(deltaEstoque);
            _db.MovimentacoesEstoque.Add(new MovimentacaoEstoque(
                produto,
                TipoMovimentacaoEstoque.AjusteManual,
                deltaEstoque,
                "Ajuste via edição de cadastro"));
        }

        await _db.SaveChangesAsync(ct);
        await _db.Produtos.Entry(produto).Reference(p => p.Categoria).LoadAsync(ct);
        return produto.ToDto();
    }

    public async Task RemoverProdutoAsync(int id, CancellationToken ct = default)
    {
        var produto = await _db.Produtos.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new DomainException("Produto não encontrado.");

        if (produto.QuantidadeReservada > 0)
            throw new DomainException("Não é possível remover um produto com pedidos pendentes de retirada.");

        _db.Produtos.Remove(produto);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<ProdutoDto> AjustarEstoqueAsync(int id, AjustarEstoqueRequest req, CancellationToken ct = default)
    {
        if (req.Delta == 0)
            throw new DomainException("Informe uma variação de estoque diferente de zero.");

        var produto = await _db.Produtos
            .Include(p => p.Categoria)
            .FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new DomainException("Produto não encontrado.");

        produto.AjustarEstoqueFisico(req.Delta);

        var tipo = req.Delta > 0
            ? TipoMovimentacaoEstoque.EntradaProducao
            : TipoMovimentacaoEstoque.AjusteManual;

        _db.MovimentacoesEstoque.Add(new MovimentacaoEstoque(
            produto,
            tipo,
            req.Delta,
            string.IsNullOrWhiteSpace(req.Motivo)
                ? (req.Delta > 0 ? "Reposição de estoque" : "Redução manual de estoque")
                : req.Motivo));

        await _db.SaveChangesAsync(ct);
        return produto.ToDto();
    }

    public async Task<ProdutoDto> AplicarDescontoProdutoAsync(int id, AplicarDescontoRequest req, CancellationToken ct = default)
    {
        var produto = await _db.Produtos
            .Include(p => p.Categoria)
            .FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new DomainException("Produto não encontrado.");

        AplicarRegraDesconto(produto, req);
        await _db.SaveChangesAsync(ct);
        return produto.ToDto();
    }

    public async Task<IReadOnlyList<ProdutoDto>> AplicarDescontoLoteAsync(AplicarDescontoRequest req, CancellationToken ct = default)
    {
        var query = _db.Produtos.Include(p => p.Categoria).Where(p => p.Ativo);
        if (req.ApenasPereciveis)
        {
            query = query.Where(p => p.Perecivel);
        }

        var produtos = await query.ToListAsync(ct);
        foreach (var p in produtos)
        {
            AplicarRegraDesconto(p, req);
        }

        await _db.SaveChangesAsync(ct);
        return produtos.Select(p => p.ToDto()).ToList();
    }

    public async Task<ProdutoDto> ZerarDescontoProdutoAsync(int id, CancellationToken ct = default)
    {
        var produto = await _db.Produtos
            .Include(p => p.Categoria)
            .FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new DomainException("Produto não encontrado.");

        produto.ZerarDesconto();
        await _db.SaveChangesAsync(ct);
        return produto.ToDto();
    }

    public async Task ZerarTodosDescontosAsync(CancellationToken ct = default)
    {
        var produtos = await _db.Produtos.ToListAsync(ct);
        foreach (var p in produtos)
        {
            p.ZerarDesconto();
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<ResultadoQueimaEstoqueDto> ExecutarQueimaEstoqueAsync(ExecutarQueimaEstoqueRequest req, CancellationToken ct = default)
    {
        decimal percentual = req.PercentualDesconto > 0m
            ? req.PercentualDesconto
            : _options.PercentualPadraoQueima;

        var produtosAtivos = await _db.Produtos
            .Include(p => p.Categoria)
            .Where(p => p.Ativo)
            .ToListAsync(ct);

        var elegiveis = produtosAtivos
            .Where(p => p.QuantidadeDisponivel > 0 && (!req.ApenasPereciveis || p.Perecivel))
            .ToList();

        foreach (var item in elegiveis)
        {
            // Se o produto ainda não possui desconto maior, aplica o desconto da queima
            if (item.PercentualDescontoEfetivo < percentual)
            {
                item.AplicarDescontoPercentual(percentual);
            }
        }

        await _db.SaveChangesAsync(ct);

        string mensagem = WhatsAppMessageFormatter.FormatarCampanhaAntidesperdicio(
            _options.NomeCantina,
            produtosAtivos,
            _options.UrlPublicaCardapio);

        decimal potencialRecuperavel = produtosAtivos
            .Where(p => p.TemDesconto && p.QuantidadeDisponivel > 0)
            .Sum(p => p.QuantidadeDisponivel * p.PrecoFinal);

        var disparos = new List<NotificacaoWhatsAppDto>();

        if (req.DispararWhatsApp && !string.IsNullOrWhiteSpace(mensagem))
        {
            var gruposQuery = _db.GruposWhatsApp.Where(g => g.Ativo);
            if (req.GrupoWhatsAppId.HasValue)
            {
                gruposQuery = gruposQuery.Where(g => g.Id == req.GrupoWhatsAppId.Value);
            }

            var grupos = await gruposQuery.OrderBy(g => g.Turno).ToListAsync(ct);
            if (grupos.Count == 0)
            {
                var envioGeral = await _whatsApp.EnviarMensagemGrupoAsync("Divulgação Geral Cantina", null, mensagem, ct);
                var log = new NotificacaoWhatsAppLog(
                    TipoNotificacaoWhatsApp.CampanhaQueimaEstoque,
                    envioGeral.CanalUtilizado,
                    "Divulgação Geral Cantina",
                    mensagem,
                    envioGeral.EnviadoAutomaticamente,
                    envioGeral.LinkFallbackWaMe,
                    envioGeral.DetalheStatus);
                _db.NotificacoesWhatsApp.Add(log);
                await _db.SaveChangesAsync(ct);
                disparos.Add(log.ToDto());
            }
            else
            {
                foreach (var grupo in grupos)
                {
                    var envio = await _whatsApp.EnviarMensagemGrupoAsync(grupo.Nome, grupo.DestinoApiJid, mensagem, ct);
                    var log = new NotificacaoWhatsAppLog(
                        TipoNotificacaoWhatsApp.CampanhaQueimaEstoque,
                        envio.CanalUtilizado,
                        grupo.Nome,
                        mensagem,
                        envio.EnviadoAutomaticamente,
                        envio.LinkFallbackWaMe,
                        envio.DetalheStatus);
                    _db.NotificacoesWhatsApp.Add(log);
                    await _db.SaveChangesAsync(ct);
                    disparos.Add(log.ToDto());
                }
            }
        }

        return new ResultadoQueimaEstoqueDto(
            elegiveis.Count,
            potencialRecuperavel,
            mensagem,
            disparos);
    }

    private static void AplicarRegraDesconto(Produto produto, AplicarDescontoRequest req)
    {
        if (req.ValorFixo.HasValue && req.ValorFixo.Value > 0m)
        {
            produto.AplicarDescontoValor(req.ValorFixo.Value);
        }
        else if (req.Percentual.HasValue && req.Percentual.Value > 0m)
        {
            produto.AplicarDescontoPercentual(req.Percentual.Value);
        }
        else
        {
            produto.ZerarDesconto();
        }
    }
}
