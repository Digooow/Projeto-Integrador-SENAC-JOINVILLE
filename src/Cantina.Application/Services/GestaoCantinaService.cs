using Cantina.Application.Abstractions;
using Cantina.Application.Options;
using Cantina.Contracts.Dtos;
using Cantina.Domain.Entities;
using Cantina.Domain.Enums;
using Cantina.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Cantina.Application.Services;

public interface IGestaoCantinaService
{
    Task<DashboardResumoDto> ObterDashboardAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AlunoDto>> ListarAlunosAsync(CancellationToken ct = default);
    Task<AlunoDto> CriarAlunoAsync(SalvarAlunoRequest req, CancellationToken ct = default);
    Task RemoverAlunoAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<GrupoWhatsAppDto>> ListarGruposWhatsAppAsync(CancellationToken ct = default);
    Task<GrupoWhatsAppDto> SalvarGrupoWhatsAppAsync(int? id, SalvarGrupoWhatsAppRequest req, CancellationToken ct = default);
    Task<IReadOnlyList<NotificacaoWhatsAppDto>> ListarNotificacoesWhatsAppAsync(CancellationToken ct = default);
    Task<IReadOnlyList<MovimentacaoEstoqueDto>> ListarMovimentacoesEstoqueAsync(CancellationToken ct = default);
    Task<FechamentoDiarioDto> GerarFechamentoDiarioAsync(GerarFechamentoRequest req, CancellationToken ct = default);
    Task<IReadOnlyList<FechamentoDiarioDto>> ListarFechamentosDiariosAsync(CancellationToken ct = default);
}

public sealed class GestaoCantinaService : IGestaoCantinaService
{
    private readonly ICantinaDbContext _db;
    private readonly IWhatsAppGateway _whatsApp;
    private readonly CantinaOperacaoOptions _options;

    public GestaoCantinaService(
        ICantinaDbContext db,
        IWhatsAppGateway whatsApp,
        IOptions<CantinaOperacaoOptions> options)
    {
        _db = db;
        _whatsApp = whatsApp;
        _options = options.Value;
    }

    public async Task<DashboardResumoDto> ObterDashboardAsync(CancellationToken ct = default)
    {
        var produtos = await _db.Produtos
            .Include(p => p.Categoria)
            .Where(p => p.Ativo)
            .ToListAsync(ct);

        var hojeInicioUtc = DateTime.UtcNow.Date;
        var pedidosHoje = await _db.Pedidos
            .Include(p => p.Itens)
            .Where(p => p.CriadoEm >= hojeInicioUtc)
            .ToListAsync(ct);

        var pedidosValidosHoje = pedidosHoje
            .Where(p => p.Status != StatusPedido.Cancelado)
            .ToList();

        int filaAtiva = await _db.Pedidos.CountAsync(p =>
            p.Status == StatusPedido.Recebido ||
            p.Status == StatusPedido.EmPreparo ||
            p.Status == StatusPedido.ProntoParaRetirada, ct);

        int totalAlunos = await _db.Alunos.CountAsync(ct);

        decimal receitaNormal = pedidosValidosHoje.Sum(p => p.ReceitaNormal);
        decimal receitaRecuperada = pedidosValidosHoje.Sum(p => p.ReceitaRecuperada);
        decimal economiaAlunos = pedidosValidosHoje.Sum(p => p.DescontoTotal);

        decimal meta = _options.MetaReceitaRecuperadaDiaria > 0m ? _options.MetaReceitaRecuperadaDiaria : 150m;
        decimal pctMeta = Math.Min(100m, Math.Round((receitaRecuperada / meta) * 100m, 1, MidpointRounding.AwayFromZero));

        var todosPedidosNaoCancelados = await _db.Pedidos
            .Where(p => p.Status != StatusPedido.Cancelado && p.NomeCliente != "Cliente Balcão")
            .ToListAsync(ct);

        var principaisClientes = todosPedidosNaoCancelados
            .GroupBy(p => new { p.NomeCliente, p.Turno })
            .Select(g => new ClienteDestaqueDto(
                g.Key.NomeCliente,
                $"SENAC {g.Key.Turno}",
                g.Count(),
                g.Sum(x => x.ValorTotal)))
            .OrderByDescending(c => c.TotalGasto)
            .Take(5)
            .ToList();

        var ofertasAtivas = produtos
            .Where(p => p.TemDesconto && p.QuantidadeDisponivel > 0)
            .OrderByDescending(p => p.PercentualDescontoEfetivo)
            .Select(p => p.ToDto())
            .ToList();

        var grupos = await _db.GruposWhatsApp
            .OrderBy(g => g.Turno)
            .ToListAsync(ct);

        return new DashboardResumoDto(
            TotalProdutosAtivos: produtos.Count,
            ProdutosEmOferta: ofertasAtivas.Count,
            UnidadesEmEstoque: produtos.Sum(p => p.QuantidadeDisponivel),
            PedidosPendentesFila: filaAtiva,
            TotalAlunosCadastrados: totalAlunos,
            ReceitaNormalHoje: receitaNormal,
            ReceitaRecuperadaHoje: receitaRecuperada,
            ReceitaTotalHoje: receitaNormal + receitaRecuperada,
            MetaReceitaRecuperadaDiaria: meta,
            PercentualMetaRecuperada: pctMeta,
            EconomiaGeradaAosAlunosHoje: economiaAlunos,
            HorarioProgramadoQueima: _options.HorarioQueimaAutomatica,
            ModoIntegracaoWhatsApp: _whatsApp.NomeModoAtivo,
            PrincipaisClientes: principaisClientes,
            OfertasAtivas: ofertasAtivas,
            GruposWhatsApp: grupos.Select(g => g.ToDto()).ToList());
    }

    public async Task<IReadOnlyList<AlunoDto>> ListarAlunosAsync(CancellationToken ct = default)
    {
        var lista = await _db.Alunos
            .OrderBy(a => a.Nome)
            .ToListAsync(ct);

        return lista.Select(a => a.ToDto()).ToList();
    }

    public async Task<AlunoDto> CriarAlunoAsync(SalvarAlunoRequest req, CancellationToken ct = default)
    {
        var turno = DtoMapper.ParseTurno(req.Turno);
        var aluno = new Aluno(
            req.Nome,
            req.Email,
            req.TelefoneWhatsApp,
            req.Matricula,
            turno,
            req.ReceberAlertasPromocao);

        _db.Alunos.Add(aluno);
        await _db.SaveChangesAsync(ct);
        return aluno.ToDto();
    }

    public async Task RemoverAlunoAsync(int id, CancellationToken ct = default)
    {
        var aluno = await _db.Alunos.FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new DomainException("Aluno não encontrado.");

        _db.Alunos.Remove(aluno);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<GrupoWhatsAppDto>> ListarGruposWhatsAppAsync(CancellationToken ct = default)
    {
        var grupos = await _db.GruposWhatsApp
            .OrderBy(g => g.Turno)
            .ToListAsync(ct);

        return grupos.Select(g => g.ToDto()).ToList();
    }

    public async Task<GrupoWhatsAppDto> SalvarGrupoWhatsAppAsync(int? id, SalvarGrupoWhatsAppRequest req, CancellationToken ct = default)
    {
        var turno = DtoMapper.ParseTurno(req.Turno);

        if (id.HasValue && id.Value > 0)
        {
            var existente = await _db.GruposWhatsApp.FirstOrDefaultAsync(g => g.Id == id.Value, ct)
                ?? throw new DomainException("Grupo de WhatsApp não encontrado.");

            existente.Atualizar(
                req.Nome,
                turno,
                req.LinkConvite,
                req.ParticipantesEstimados,
                req.DestinoApiJid,
                req.Ativo);

            await _db.SaveChangesAsync(ct);
            return existente.ToDto();
        }

        var novo = new GrupoWhatsApp(
            req.Nome,
            turno,
            req.LinkConvite,
            req.ParticipantesEstimados,
            req.DestinoApiJid,
            req.Ativo);

        _db.GruposWhatsApp.Add(novo);
        await _db.SaveChangesAsync(ct);
        return novo.ToDto();
    }

    public async Task<IReadOnlyList<NotificacaoWhatsAppDto>> ListarNotificacoesWhatsAppAsync(CancellationToken ct = default)
    {
        var logs = await _db.NotificacoesWhatsApp
            .OrderByDescending(n => n.CriadaEm)
            .Take(80)
            .ToListAsync(ct);

        return logs.Select(n => n.ToDto()).ToList();
    }

    public async Task<IReadOnlyList<MovimentacaoEstoqueDto>> ListarMovimentacoesEstoqueAsync(CancellationToken ct = default)
    {
        var movs = await _db.MovimentacoesEstoque
            .OrderByDescending(m => m.OcorridaEm)
            .Take(100)
            .ToListAsync(ct);

        return movs.Select(m => m.ToDto()).ToList();
    }

    public async Task<FechamentoDiarioDto> GerarFechamentoDiarioAsync(GerarFechamentoRequest req, CancellationToken ct = default)
    {
        var hojeInicioUtc = DateTime.UtcNow.Date;
        var pedidosConcluidosHoje = await _db.Pedidos
            .Include(p => p.Itens)
            .Where(p => p.Status == StatusPedido.Concluido && p.CriadoEm >= hojeInicioUtc)
            .ToListAsync(ct);

        var produtos = await _db.Produtos.Where(p => p.Ativo).ToListAsync(ct);

        decimal receitaNormal = pedidosConcluidosHoje.Sum(p => p.ReceitaNormal);
        decimal receitaRecuperada = pedidosConcluidosHoje.Sum(p => p.ReceitaRecuperada);
        decimal descontos = pedidosConcluidosHoje.Sum(p => p.DescontoTotal);

        int pedidosWeb = pedidosConcluidosHoje.Count(p => p.Origem == OrigemPedido.SiteWeb);
        int vendasBalcao = pedidosConcluidosHoje.Count(p => p.Origem == OrigemPedido.BalcaoDesktop);

        var todosItens = pedidosConcluidosHoje.SelectMany(p => p.Itens).ToList();
        int itensPrecoCheio = todosItens.Where(i => !i.ItemPromocional).Sum(i => i.Quantidade);
        int itensPromocao = todosItens.Where(i => i.ItemPromocional).Sum(i => i.Quantidade);
        int sobrasEstoque = produtos.Where(p => p.Perecivel).Sum(p => p.QuantidadeEstoque);

        var fechamento = new FechamentoDiario(
            DateOnly.FromDateTime(DateTime.Now),
            receitaNormal,
            receitaRecuperada,
            descontos,
            pedidosWeb,
            vendasBalcao,
            itensPrecoCheio,
            itensPromocao,
            sobrasEstoque,
            req.Observacoes);

        _db.FechamentosDiarios.Add(fechamento);
        await _db.SaveChangesAsync(ct);

        return fechamento.ToDto();
    }

    public async Task<IReadOnlyList<FechamentoDiarioDto>> ListarFechamentosDiariosAsync(CancellationToken ct = default)
    {
        var lista = await _db.FechamentosDiarios
            .OrderByDescending(f => f.FechadoEm)
            .Take(30)
            .ToListAsync(ct);

        return lista.Select(f => f.ToDto()).ToList();
    }
}

