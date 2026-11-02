using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Cantina.Contracts.Dtos;

namespace Cantina.Wpf.Services;

public sealed class CantinaApiClient : IDisposable
{
    private readonly HttpClient _http;
    private CantinaWpfSettings _settings;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public string BaseUrl => _settings.ApiBaseUrl;

    public CantinaApiClient(CantinaWpfSettings settings)
    {
        _settings = settings;
        _http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };
        ConfigureClient(settings);
    }

    public void UpdateSettings(CantinaWpfSettings settings)
    {
        _settings = settings;
        ConfigureClient(settings);
    }

    private void ConfigureClient(CantinaWpfSettings settings)
    {
        _http.DefaultRequestHeaders.Remove("X-Api-Key");
        if (!string.IsNullOrWhiteSpace(settings.AdminApiKey))
        {
            _http.DefaultRequestHeaders.Add("X-Api-Key", settings.AdminApiKey.Trim());
        }
    }

    private string Url(string path) => $"{_settings.ApiBaseUrl.TrimEnd('/')}{path}";

    public async Task<bool> CheckHealthAsync()
    {
        try
        {
            using var resp = await _http.GetAsync(Url("/health"));
            return resp.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public Task<DashboardResumoDto?> GetDashboardAsync() =>
        GetAsync<DashboardResumoDto>("/api/v1/gestao/dashboard");

    public async Task<IReadOnlyList<CategoriaDto>> GetCategoriasAsync() =>
        await GetAsync<List<CategoriaDto>>("/api/v1/gestao/categorias") ?? new List<CategoriaDto>();

    public async Task<IReadOnlyList<ProdutoDto>> GetProdutosAsync() =>
        await GetAsync<List<ProdutoDto>>("/api/v1/gestao/produtos") ?? new List<ProdutoDto>();

    public Task<ProdutoDto> CriarProdutoAsync(SalvarProdutoRequest req) =>
        SendAsync<ProdutoDto>(HttpMethod.Post, "/api/v1/gestao/produtos", req);

    public Task<ProdutoDto> AtualizarProdutoAsync(int id, SalvarProdutoRequest req) =>
        SendAsync<ProdutoDto>(HttpMethod.Put, $"/api/v1/gestao/produtos/{id}", req);

    public Task ExcluirProdutoAsync(int id) =>
        SendNoContentAsync(HttpMethod.Delete, $"/api/v1/gestao/produtos/{id}");

    public Task<ProdutoDto> AjustarEstoqueAsync(int id, int delta, string? motivo = null) =>
        SendAsync<ProdutoDto>(HttpMethod.Patch, $"/api/v1/gestao/produtos/{id}/estoque", new AjustarEstoqueRequest(delta, motivo));

    public Task<ProdutoDto> AplicarDescontoProdutoAsync(int id, AplicarDescontoRequest req) =>
        SendAsync<ProdutoDto>(HttpMethod.Post, $"/api/v1/gestao/produtos/{id}/desconto", req);

    public Task<ProdutoDto> ZerarDescontoProdutoAsync(int id) =>
        SendAsync<ProdutoDto>(HttpMethod.Delete, $"/api/v1/gestao/produtos/{id}/desconto");

    public Task AplicarDescontoLoteAsync(AplicarDescontoRequest req) =>
        SendAsync<List<ProdutoDto>>(HttpMethod.Post, "/api/v1/gestao/produtos/descontos-lote", req);

    public Task ZerarDescontoLoteAsync() =>
        SendNoContentAsync(HttpMethod.Delete, "/api/v1/gestao/produtos/descontos-lote");

    public Task<ResultadoQueimaEstoqueDto> ExecutarQueimaEstoqueAsync(ExecutarQueimaEstoqueRequest req) =>
        SendAsync<ResultadoQueimaEstoqueDto>(HttpMethod.Post, "/api/v1/gestao/queima-estoque", req);

    public async Task<IReadOnlyList<PedidoDto>> GetPedidosAsync(bool apenasFila = false) =>
        await GetAsync<List<PedidoDto>>($"/api/v1/gestao/pedidos?apenasFila={apenasFila.ToString().ToLowerInvariant()}") ?? new List<PedidoDto>();

    public Task<PedidoDto> RegistrarVendaBalcaoAsync(RegistrarVendaBalcaoRequest req) =>
        SendAsync<PedidoDto>(HttpMethod.Post, "/api/v1/gestao/pedidos/venda-balcao", req);

    public Task<PedidoDto> AtualizarStatusPedidoAsync(int id, string novoStatus) =>
        SendAsync<PedidoDto>(HttpMethod.Patch, $"/api/v1/gestao/pedidos/{id}/status", new AtualizarStatusPedidoRequest(novoStatus));

    public async Task<IReadOnlyList<AlunoDto>> GetAlunosAsync() =>
        await GetAsync<List<AlunoDto>>("/api/v1/gestao/alunos") ?? new List<AlunoDto>();

    public Task<AlunoDto> CriarAlunoAsync(SalvarAlunoRequest req) =>
        SendAsync<AlunoDto>(HttpMethod.Post, "/api/v1/gestao/alunos", req);

    public Task ExcluirAlunoAsync(int id) =>
        SendNoContentAsync(HttpMethod.Delete, $"/api/v1/gestao/alunos/{id}");

    public async Task<IReadOnlyList<GrupoWhatsAppDto>> GetGruposWhatsAppAsync() =>
        await GetAsync<List<GrupoWhatsAppDto>>("/api/v1/gestao/grupos-whatsapp") ?? new List<GrupoWhatsAppDto>();

    public Task<GrupoWhatsAppDto> SalvarGrupoWhatsAppAsync(int? id, SalvarGrupoWhatsAppRequest req) =>
        id.HasValue && id.Value > 0
            ? SendAsync<GrupoWhatsAppDto>(HttpMethod.Put, $"/api/v1/gestao/grupos-whatsapp/{id.Value}", req)
            : SendAsync<GrupoWhatsAppDto>(HttpMethod.Post, "/api/v1/gestao/grupos-whatsapp", req);

    public async Task<IReadOnlyList<NotificacaoWhatsAppDto>> GetNotificacoesWhatsAppAsync() =>
        await GetAsync<List<NotificacaoWhatsAppDto>>("/api/v1/gestao/notificacoes-whatsapp") ?? new List<NotificacaoWhatsAppDto>();

    public async Task<IReadOnlyList<MovimentacaoEstoqueDto>> GetMovimentacoesEstoqueAsync() =>
        await GetAsync<List<MovimentacaoEstoqueDto>>("/api/v1/gestao/movimentacoes-estoque") ?? new List<MovimentacaoEstoqueDto>();

    public async Task<IReadOnlyList<FechamentoDiarioDto>> GetFechamentosAsync() =>
        await GetAsync<List<FechamentoDiarioDto>>("/api/v1/gestao/fechamentos") ?? new List<FechamentoDiarioDto>();

    public Task<FechamentoDiarioDto> GerarFechamentoAsync(string? obs) =>
        SendAsync<FechamentoDiarioDto>(HttpMethod.Post, "/api/v1/gestao/fechamentos", new GerarFechamentoRequest(obs));

    private async Task<T?> GetAsync<T>(string path)
    {
        using var resp = await _http.GetAsync(Url(path));
        await EnsureSuccessOrThrowDetailAsync(resp);
        return await resp.Content.ReadFromJsonAsync<T>(JsonOpts);
    }

    private async Task<T> SendAsync<T>(HttpMethod method, string path, object? body = null)
    {
        using var req = new HttpRequestMessage(method, Url(path));
        if (body is not null)
        {
            req.Content = JsonContent.Create(body);
        }

        using var resp = await _http.SendAsync(req);
        await EnsureSuccessOrThrowDetailAsync(resp);
        var result = await resp.Content.ReadFromJsonAsync<T>(JsonOpts);
        return result ?? throw new InvalidOperationException("Resposta vazia do servidor.");
    }

    private async Task SendNoContentAsync(HttpMethod method, string path)
    {
        using var req = new HttpRequestMessage(method, Url(path));
        using var resp = await _http.SendAsync(req);
        await EnsureSuccessOrThrowDetailAsync(resp);
    }

    private static async Task EnsureSuccessOrThrowDetailAsync(HttpResponseMessage resp)
    {
        if (resp.IsSuccessStatusCode)
            return;

        string raw = await resp.Content.ReadAsStringAsync();
        try
        {
            using var doc = JsonDocument.Parse(raw);
            if (doc.RootElement.TryGetProperty("detail", out var detailProp))
            {
                throw new InvalidOperationException(detailProp.GetString() ?? "Erro na requisição.");
            }
        }
        catch (JsonException)
        {
            // Usa mensagem HTTP padrão abaixo
        }

        throw new InvalidOperationException($"Servidor respondeu HTTP {(int)resp.StatusCode}.");
    }

    public void Dispose() => _http.Dispose();
}

