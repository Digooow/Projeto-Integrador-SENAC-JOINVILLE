using Cantina.Application.Abstractions;
using Cantina.Application.Options;
using Cantina.Application.Services;
using Cantina.Contracts.Dtos;
using Cantina.Domain.Entities;
using Cantina.Domain.Enums;
using Cantina.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Cantina.Tests.Application;

public sealed class PedidoServiceResilienceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly CantinaDbContext _db;

    public PedidoServiceResilienceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<CantinaDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new CantinaDbContext(options);
        _db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task AtualizarStatusAsync_DevePersistirTransicaoMesmoQuandoGatewayWhatsAppFalhar()
    {
        var categoria = new Categoria("Salgados", "🥐", perecivelDiario: true, ordemExibicao: 1);
        _db.Categorias.Add(categoria);
        await _db.SaveChangesAsync();

        var produto = new Produto("Coxinha", "Frango com catupiry", "🍗", categoria.Id, 3.00m, 7.50m, 10);
        _db.Produtos.Add(produto);
        await _db.SaveChangesAsync();

        var service = new PedidoService(
            _db,
            new ThrowingWhatsAppGateway(),
            Options.Create(new CantinaOperacaoOptions { NomeCantina = "Cantina Teste", UrlPublicaCardapio = "http://localhost" }),
            NullLogger<PedidoService>.Instance);

        var criado = await service.CriarPedidoWebAsync(new CriarPedidoWebRequest(
            NomeCliente: "Cliente Resiliência",
            TelefoneWhatsApp: "(47) 99999-1111",
            MatriculaAluno: null,
            Turno: "Noite",
            FormaPagamento: "PixNaRetirada",
            Observacoes: null,
            Itens: [new CriarItemPedidoRequest(produto.Id, 2)]));

        var atualizado = await service.AtualizarStatusAsync(
            criado.Id,
            new AtualizarStatusPedidoRequest("ProntoParaRetirada"));

        Assert.Equal("ProntoParaRetirada", atualizado.Status);

        var persistido = await _db.Pedidos.AsNoTracking().FirstAsync(p => p.Id == criado.Id);
        Assert.Equal(StatusPedido.ProntoParaRetirada, persistido.Status);
    }

    [Fact]
    public async Task CodigoRetirada_DevePossuirRestricaoUniqueNoBancoRelacional()
    {
        var categoria = new Categoria("Bebidas", "🥤", perecivelDiario: false, ordemExibicao: 1);
        _db.Categorias.Add(categoria);
        await _db.SaveChangesAsync();

        var produto = new Produto("Água", "500ml", "💧", categoria.Id, 1.00m, 3.50m, 20, perecivel: false);
        _db.Produtos.Add(produto);
        await _db.SaveChangesAsync();

        var p1 = new Pedido(
            OrigemPedido.BalcaoDesktop,
            "Cliente 1",
            null,
            null,
            TurnoSenac.Manha,
            FormaPagamento.DinheiroNoBalcao,
            null,
            [new ItemPedido(produto, 1)],
            codigoRetirada: "AL-2026-DUP001");

        var p2 = new Pedido(
            OrigemPedido.BalcaoDesktop,
            "Cliente 2",
            null,
            null,
            TurnoSenac.Manha,
            FormaPagamento.DinheiroNoBalcao,
            null,
            [new ItemPedido(produto, 1)],
            codigoRetirada: "AL-2026-DUP001");

        _db.Pedidos.AddRange(p1, p2);
        await Assert.ThrowsAsync<DbUpdateException>(() => _db.SaveChangesAsync());
    }

    [Fact]
    public async Task RegistrarVendaBalcaoAsync_DeveRegenerarCodigoRetiradaQuandoOcorrerColisaoConcorrenteNoSaveChanges()
    {
        var categoria = new Categoria("Bebidas", "🥤", perecivelDiario: false, ordemExibicao: 1);
        _db.Categorias.Add(categoria);
        await _db.SaveChangesAsync();

        var produto = new Produto("Suco", "300ml", "🧃", categoria.Id, 2.00m, 6.00m, 15, perecivel: false);
        _db.Produtos.Add(produto);
        await _db.SaveChangesAsync();

        var collidingDb = new CollidingFirstSaveDbContext(_db);
        var service = new PedidoService(
            collidingDb,
            new ThrowingWhatsAppGateway(),
            Options.Create(new CantinaOperacaoOptions { NomeCantina = "Cantina Teste", UrlPublicaCardapio = "http://localhost" }),
            NullLogger<PedidoService>.Instance);

        var resultado = await service.RegistrarVendaBalcaoAsync(new RegistrarVendaBalcaoRequest(
            NomeCliente: "Cliente Concorrente",
            FormaPagamento: "DinheiroNoBalcao",
            Itens: [new CriarItemPedidoRequest(produto.Id, 1)]));

        Assert.Equal(2, collidingDb.SaveAttempts);
        Assert.NotNull(collidingDb.FirstAttemptCode);
        Assert.NotEqual(collidingDb.FirstAttemptCode, resultado.CodigoRetirada);
    }

    private sealed class CollidingFirstSaveDbContext(CantinaDbContext inner) : ICantinaDbContext
    {
        public int SaveAttempts { get; private set; }
        public string? FirstAttemptCode { get; private set; }

        public DbSet<Categoria> Categorias => inner.Categorias;
        public DbSet<Produto> Produtos => inner.Produtos;
        public DbSet<Pedido> Pedidos => inner.Pedidos;
        public DbSet<ItemPedido> ItensPedido => inner.ItensPedido;
        public DbSet<GrupoWhatsApp> GruposWhatsApp => inner.GruposWhatsApp;
        public DbSet<Aluno> Alunos => inner.Alunos;
        public DbSet<MovimentacaoEstoque> MovimentacoesEstoque => inner.MovimentacoesEstoque;
        public DbSet<NotificacaoWhatsAppLog> NotificacoesWhatsApp => inner.NotificacoesWhatsApp;
        public DbSet<FechamentoDiario> FechamentosDiarios => inner.FechamentosDiarios;

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveAttempts++;
            if (SaveAttempts == 1)
            {
                var trackedPedido = inner.ChangeTracker.Entries<Pedido>().First().Entity;
                FirstAttemptCode = trackedPedido.CodigoRetirada;
                throw new DbUpdateException("UNIQUE constraint failed: Pedidos.CodigoRetirada");
            }

            return inner.SaveChangesAsync(cancellationToken);
        }
    }

    private sealed class ThrowingWhatsAppGateway : IWhatsAppGateway
    {
        public string NomeModoAtivo => "FaultInjection";

        public Task<ResultadoEnvioWhatsApp> EnviarMensagemIndividualAsync(
            string telefoneDestino,
            string mensagem,
            CancellationToken cancellationToken = default) =>
            throw new HttpRequestException("Falha simulada de rede no WhatsApp.");

        public Task<ResultadoEnvioWhatsApp> EnviarMensagemGrupoAsync(
            string nomeGrupo,
            string? destinoApiJid,
            string mensagem,
            CancellationToken cancellationToken = default) =>
            throw new HttpRequestException("Falha simulada de rede no WhatsApp.");
    }
}
