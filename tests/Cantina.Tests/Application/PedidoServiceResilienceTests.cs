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
