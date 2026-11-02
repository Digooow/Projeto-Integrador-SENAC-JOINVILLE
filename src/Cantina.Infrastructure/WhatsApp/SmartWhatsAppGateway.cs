using System.Net.Http.Headers;
using System.Net.Http.Json;
using Cantina.Application.Abstractions;
using Cantina.Application.Formatting;
using Cantina.Domain.Entities;
using Cantina.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cantina.Infrastructure.WhatsApp;

public sealed class WhatsAppOptions
{
    public const string SectionName = "WhatsApp";

    /// <summary>
    /// Auto | Evolution | MetaCloud | WaMeFallback
    /// </summary>
    public string Provider { get; set; } = "Auto";

    // Evolution API (Open-Source Gratuito - suporta mensagens individuais e Grupos @g.us)
    public string? EvolutionBaseUrl { get; set; }
    public string? EvolutionInstance { get; set; }
    public string? EvolutionApiKey { get; set; }

    // WhatsApp Business Cloud API Oficial da Meta (Free Developer / Service Tier)
    public string? MetaPhoneNumberId { get; set; }
    public string? MetaAccessToken { get; set; }
    public string MetaApiVersion { get; set; } = "v21.0";
    public string? MetaBroadcastFallbackPhone { get; set; }
}

public sealed class SmartWhatsAppGateway : IWhatsAppGateway
{
    private readonly HttpClient _http;
    private readonly WhatsAppOptions _options;
    private readonly ILogger<SmartWhatsAppGateway> _logger;

    public SmartWhatsAppGateway(
        HttpClient http,
        IOptions<WhatsAppOptions> options,
        ILogger<SmartWhatsAppGateway> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public string NomeModoAtivo
    {
        get
        {
            if (UsarEvolution)
                return $"Evolution API ({_options.EvolutionInstance})";
            if (UsarMetaCloud)
                return "Meta WhatsApp Cloud API";
            return "Outbox + Link Direto (wa.me)";
        }
    }

    private bool UsarEvolution =>
        (_options.Provider.Equals("Evolution", StringComparison.OrdinalIgnoreCase) ||
         _options.Provider.Equals("Auto", StringComparison.OrdinalIgnoreCase)) &&
        !string.IsNullOrWhiteSpace(_options.EvolutionBaseUrl) &&
        !string.IsNullOrWhiteSpace(_options.EvolutionInstance);

    private bool UsarMetaCloud =>
        (_options.Provider.Equals("MetaCloud", StringComparison.OrdinalIgnoreCase) ||
         _options.Provider.Equals("Auto", StringComparison.OrdinalIgnoreCase)) &&
        !string.IsNullOrWhiteSpace(_options.MetaPhoneNumberId) &&
        !string.IsNullOrWhiteSpace(_options.MetaAccessToken);

    public async Task<ResultadoEnvioWhatsApp> EnviarMensagemIndividualAsync(
        string telefoneDestino,
        string mensagem,
        CancellationToken cancellationToken = default)
    {
        string numeroLimpo = Pedido.NormalizarTelefone(telefoneDestino);
        string linkWaMe = WhatsAppMessageFormatter.GerarLinkWaMe(numeroLimpo, mensagem);

        if (UsarEvolution && !string.IsNullOrWhiteSpace(numeroLimpo))
        {
            return await EnviarViaEvolutionAsync(numeroLimpo, mensagem, linkWaMe, cancellationToken);
        }

        if (UsarMetaCloud && !string.IsNullOrWhiteSpace(numeroLimpo))
        {
            return await EnviarViaMetaCloudAsync(numeroLimpo, mensagem, linkWaMe, cancellationToken);
        }

        return new ResultadoEnvioWhatsApp(
            CanalEnvioWhatsApp.OutboxWaMeFallback,
            EnviadoAutomaticamente: false,
            LinkFallbackWaMe: linkWaMe,
            DetalheStatus: "Registrado no Outbox local (link wa.me pronto para envio 1-clique)");
    }

    public async Task<ResultadoEnvioWhatsApp> EnviarMensagemGrupoAsync(
        string nomeGrupo,
        string? destinoApiJid,
        string mensagem,
        CancellationToken cancellationToken = default)
    {
        string linkWaMe = WhatsAppMessageFormatter.GerarLinkWaMe(null, mensagem);

        if (UsarEvolution && !string.IsNullOrWhiteSpace(destinoApiJid))
        {
            return await EnviarViaEvolutionAsync(destinoApiJid.Trim(), mensagem, linkWaMe, cancellationToken);
        }

        if (UsarMetaCloud && !string.IsNullOrWhiteSpace(_options.MetaBroadcastFallbackPhone))
        {
            string fone = Pedido.NormalizarTelefone(_options.MetaBroadcastFallbackPhone);
            return await EnviarViaMetaCloudAsync(fone, mensagem, linkWaMe, cancellationToken);
        }

        return new ResultadoEnvioWhatsApp(
            CanalEnvioWhatsApp.OutboxWaMeFallback,
            EnviadoAutomaticamente: false,
            LinkFallbackWaMe: linkWaMe,
            DetalheStatus: $"Campanha preparada para '{nomeGrupo}' (configure Evolution API / Meta API para envio 100% automático)");
    }

    private async Task<ResultadoEnvioWhatsApp> EnviarViaEvolutionAsync(
        string destino,
        string mensagem,
        string linkFallback,
        CancellationToken ct)
    {
        try
        {
            string url = $"{_options.EvolutionBaseUrl!.TrimEnd('/')}/message/sendText/{Uri.EscapeDataString(_options.EvolutionInstance!)}";
            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            if (!string.IsNullOrWhiteSpace(_options.EvolutionApiKey))
            {
                req.Headers.Add("apikey", _options.EvolutionApiKey);
            }

            req.Content = JsonContent.Create(new
            {
                number = destino,
                text = mensagem
            });

            using var resp = await _http.SendAsync(req, ct);
            if (resp.IsSuccessStatusCode)
            {
                return new ResultadoEnvioWhatsApp(
                    CanalEnvioWhatsApp.EvolutionApi,
                    EnviadoAutomaticamente: true,
                    LinkFallbackWaMe: linkFallback,
                    DetalheStatus: $"Enviado automaticamente via Evolution API (HTTP {(int)resp.StatusCode})");
            }

            _logger.LogWarning("Falha ao enviar via Evolution API: HTTP {StatusCode}", (int)resp.StatusCode);
            return new ResultadoEnvioWhatsApp(
                CanalEnvioWhatsApp.EvolutionApi,
                EnviadoAutomaticamente: false,
                LinkFallbackWaMe: linkFallback,
                DetalheStatus: $"Evolution API retornou HTTP {(int)resp.StatusCode}; link wa.me disponível como contingência");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro de rede ao contatar Evolution API.");
            return new ResultadoEnvioWhatsApp(
                CanalEnvioWhatsApp.OutboxWaMeFallback,
                EnviadoAutomaticamente: false,
                LinkFallbackWaMe: linkFallback,
                DetalheStatus: $"Contingência wa.me ativada ({ex.GetType().Name})");
        }
    }

    private async Task<ResultadoEnvioWhatsApp> EnviarViaMetaCloudAsync(
        string numeroDestino,
        string mensagem,
        string linkFallback,
        CancellationToken ct)
    {
        try
        {
            string versao = string.IsNullOrWhiteSpace(_options.MetaApiVersion) ? "v21.0" : _options.MetaApiVersion.Trim();
            string url = $"https://graph.facebook.com/{versao}/{Uri.EscapeDataString(_options.MetaPhoneNumberId!)}/messages";

            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.MetaAccessToken);
            req.Content = JsonContent.Create(new
            {
                messaging_product = "whatsapp",
                recipient_type = "individual",
                to = numeroDestino,
                type = "text",
                text = new
                {
                    preview_url = true,
                    body = mensagem
                }
            });

            using var resp = await _http.SendAsync(req, ct);
            if (resp.IsSuccessStatusCode)
            {
                return new ResultadoEnvioWhatsApp(
                    CanalEnvioWhatsApp.MetaCloudApi,
                    EnviadoAutomaticamente: true,
                    LinkFallbackWaMe: linkFallback,
                    DetalheStatus: $"Enviado via Meta Cloud API (HTTP {(int)resp.StatusCode})");
            }

            _logger.LogWarning("Falha ao enviar via Meta Cloud API: HTTP {StatusCode}", (int)resp.StatusCode);
            return new ResultadoEnvioWhatsApp(
                CanalEnvioWhatsApp.MetaCloudApi,
                EnviadoAutomaticamente: false,
                LinkFallbackWaMe: linkFallback,
                DetalheStatus: $"Meta Cloud API retornou HTTP {(int)resp.StatusCode}; link wa.me disponível");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro de rede ao contatar Meta Cloud API.");
            return new ResultadoEnvioWhatsApp(
                CanalEnvioWhatsApp.OutboxWaMeFallback,
                EnviadoAutomaticamente: false,
                LinkFallbackWaMe: linkFallback,
                DetalheStatus: $"Contingência wa.me ativada ({ex.GetType().Name})");
        }
    }
}

