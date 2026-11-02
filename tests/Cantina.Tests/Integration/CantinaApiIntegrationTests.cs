using System.Net;
using System.Net.Http.Json;
using Cantina.Contracts.Dtos;
using Cantina.Infrastructure;
using Cantina.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Cantina.Tests.Integration;

public sealed class CantinaApiIntegrationTests : IAsyncLifetime
{
    private const string AdminKey = "senac-test-admin-key-2026";
    private readonly string _tempDbPath = Path.Combine(Path.GetTempPath(), $"cantina_test_{Guid.NewGuid():N}.db");
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _publicClient = null!;
    private HttpClient _adminClient = null!;

    public async Task InitializeAsync()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Database:Provider"] = "Sqlite",
                        ["ConnectionStrings:DefaultConnection"] = $"Data Source={_tempDbPath}",
                        ["Security:AdminApiKey"] = AdminKey,
                        ["WhatsApp:Provider"] = "Disabled",
                        ["CantinaOperacao:HabilitarQueimaAutomatica"] = "false"
                    });
                });
            });

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CantinaDbContext>();
            await CantinaDatabaseSeeder.InitializeAndSeedAsync(db);
        }

        _publicClient = _factory.CreateClient();
        _adminClient = _factory.CreateClient();
        _adminClient.DefaultRequestHeaders.Add("X-Api-Key", AdminKey);
    }

    public async Task DisposeAsync()
    {
        _publicClient.Dispose();
        _adminClient.Dispose();
        await _factory.DisposeAsync();

        try
        {
            if (File.Exists(_tempDbPath))
                File.Delete(_tempDbPath);
        }
        catch
        {
            // Ignora bloqueio residual do pool SQLite em limpeza temporária
        }
    }

    [Fact]
    public async Task HealthESiteDePedidos_DevemResponderComSucesso()
    {
        var healthResp = await _publicClient.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, healthResp.StatusCode);

        var homeResp = await _publicClient.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, homeResp.StatusCode);
        var html = await homeResp.Content.ReadAsStringAsync();
        Assert.Contains("Cantina ALAMO", html);
        Assert.Contains("closedby=\"any\"", html);
    }

    [Fact]
    public async Task EndpointsDeGestao_DevemExigirApiKeyQuandoConfigurada()
    {
        var semChave = await _publicClient.GetAsync("/api/v1/gestao/dashboard");
        Assert.Equal(HttpStatusCode.Unauthorized, semChave.StatusCode);

        var comChave = await _adminClient.GetAsync("/api/v1/gestao/dashboard");
        Assert.Equal(HttpStatusCode.OK, comChave.StatusCode);
    }

    [Fact]
    public async Task FluxoCompleto_QueimaDeEstoque_PedidoOnline_NotificacaoWhatsApp_E_Dashboard()
    {
        // 1. Consulta cardápio público inicial
        var cardapioInicial = await _publicClient.GetFromJsonAsync<CardapioPublicoDto>("/api/v1/cardapio");
        Assert.NotNull(cardapioInicial);
        Assert.NotEmpty(cardapioInicial.Produtos);
        Assert.NotEmpty(cardapioInicial.GruposWhatsApp);

        // 2. Operador dispara Queima de Estoque 20:45 (40% apenas em perecíveis)
        var queimaResp = await _adminClient.PostAsJsonAsync(
            "/api/v1/gestao/queima-estoque",
            new ExecutarQueimaEstoqueRequest(PercentualDesconto: 40m, ApenasPereciveis: true, DispararWhatsApp: true));
        queimaResp.EnsureSuccessStatusCode();

        var resultadoQueima = await queimaResp.Content.ReadFromJsonAsync<ResultadoQueimaEstoqueDto>();
        Assert.NotNull(resultadoQueima);
        Assert.True(resultadoQueima.ProdutosAfetados > 0);
        Assert.NotEmpty(resultadoQueima.DisparosRealizados);

        // 3. Verifica que perecíveis entraram em oferta e não-perecíveis mantiveram preço cheio
        var cardapioPosQueima = await _publicClient.GetFromJsonAsync<CardapioPublicoDto>("/api/v1/cardapio");
        Assert.NotNull(cardapioPosQueima);
        Assert.True(cardapioPosQueima.ModoAntidesperdicioAtivo);

        var perecivelOferta = cardapioPosQueima.Produtos.First(p => p.Perecivel && p.QuantidadeDisponivel >= 2);
        var naoPerecivel = cardapioPosQueima.Produtos.First(p => !p.Perecivel && p.QuantidadeDisponivel >= 1);
        Assert.True(perecivelOferta.TemDesconto);
        Assert.False(naoPerecivel.TemDesconto);

        int dispAntes = perecivelOferta.QuantidadeDisponivel;

        // 4. Aluno realiza pedido pelo site Fura-Fila
        var criarResp = await _publicClient.PostAsJsonAsync("/api/v1/pedidos", new CriarPedidoWebRequest(
            NomeCliente: "Rodrigo Teste SENAC",
            TelefoneWhatsApp: "(47) 99888-7766",
            MatriculaAluno: "2026999",
            Turno: "Noite",
            FormaPagamento: "PixNaRetirada",
            Observacoes: "Retirada rápida no intervalo",
            Itens:
            [
                new CriarItemPedidoRequest(perecivelOferta.Id, 2),
                new CriarItemPedidoRequest(naoPerecivel.Id, 1)
            ]));

        Assert.Equal(HttpStatusCode.Created, criarResp.StatusCode);
        var pedidoCriado = await criarResp.Content.ReadFromJsonAsync<PedidoDto>();
        Assert.NotNull(pedidoCriado);
        Assert.StartsWith("AL-", pedidoCriado.CodigoRetirada);
        Assert.Equal("Recebido", pedidoCriado.Status);
        Assert.True(pedidoCriado.ReceitaRecuperada > 0m);
        Assert.True(pedidoCriado.ReceitaNormal > 0m);
        Assert.StartsWith("https://wa.me/5547998887766", pedidoCriado.UltimoLinkWhatsApp);

        // Estoque disponível deve ter sido reservado imediatamente
        var cardapioAposReserva = await _publicClient.GetFromJsonAsync<CardapioPublicoDto>("/api/v1/cardapio");
        var prodReservado = cardapioAposReserva!.Produtos.First(p => p.Id == perecivelOferta.Id);
        Assert.Equal(dispAntes - 2, prodReservado.QuantidadeDisponivel);

        // 5. Aluno rastreia o pedido pelo código público no site
        var rastreado = await _publicClient.GetFromJsonAsync<PedidoDto>($"/api/v1/pedidos/{pedidoCriado.CodigoRetirada}");
        Assert.NotNull(rastreado);
        Assert.Equal(pedidoCriado.Id, rastreado.Id);

        // 6. Cantina avança para ProntoParaRetirada e depois Concluido
        var prontoResp = await _adminClient.PatchAsJsonAsync(
            $"/api/v1/gestao/pedidos/{pedidoCriado.Id}/status",
            new AtualizarStatusPedidoRequest("ProntoParaRetirada"));
        prontoResp.EnsureSuccessStatusCode();

        var concluidoResp = await _adminClient.PatchAsJsonAsync(
            $"/api/v1/gestao/pedidos/{pedidoCriado.Id}/status",
            new AtualizarStatusPedidoRequest("Concluido"));
        concluidoResp.EnsureSuccessStatusCode();

        // 7. Dashboard e Notificações WhatsApp devem refletir a receita recuperada e os alertas enviados
        var dashboard = await _adminClient.GetFromJsonAsync<DashboardResumoDto>("/api/v1/gestao/dashboard");
        Assert.NotNull(dashboard);
        Assert.True(dashboard.ReceitaRecuperadaHoje >= pedidoCriado.ReceitaRecuperada);

        var notificacoes = await _adminClient.GetFromJsonAsync<List<NotificacaoWhatsAppDto>>("/api/v1/gestao/notificacoes-whatsapp");
        Assert.NotNull(notificacoes);
        Assert.Contains(notificacoes, n => n.PedidoId == pedidoCriado.Id && n.Tipo == "PedidoProntoRetirada");
    }

    [Fact]
    public void ConverterDatabaseUrlParaNpgsql_DeveTraduzirUrlDeHospedagemGratuitaCorretamente()
    {
        var conn = DependencyInjection.ConverterDatabaseUrlParaNpgsql(
            "postgres://cantina_user:s3cr3t@ep-cool-cloud.us-east-1.aws.neon.tech:5432/cantina_alamo");

        Assert.Contains("Host=ep-cool-cloud.us-east-1.aws.neon.tech", conn);
        Assert.Contains("Port=5432", conn);
        Assert.Contains("Database=cantina_alamo", conn);
        Assert.Contains("Username=cantina_user", conn);
        Assert.Contains("Password=s3cr3t", conn);
    }
}
