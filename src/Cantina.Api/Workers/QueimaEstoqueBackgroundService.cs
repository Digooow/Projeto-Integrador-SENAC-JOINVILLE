using Cantina.Application.Options;
using Cantina.Application.Services;
using Cantina.Contracts.Dtos;
using Microsoft.Extensions.Options;

namespace Cantina.Api.Workers;

public sealed class QueimaEstoqueBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<CantinaOperacaoOptions> _optionsMonitor;
    private readonly ILogger<QueimaEstoqueBackgroundService> _logger;
    private DateOnly? _ultimaExecucaoData;

    public QueimaEstoqueBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<CantinaOperacaoOptions> optionsMonitor,
        ILogger<QueimaEstoqueBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _optionsMonitor = optionsMonitor;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                var opts = _optionsMonitor.CurrentValue;
                if (!opts.HabilitarWorkerQueimaAutomatica)
                    continue;

                if (!TimeOnly.TryParse(opts.HorarioQueimaAutomatica, out var horarioAlvo))
                    continue;

                var agoraLocal = ObterHorarioBrasilia();
                var hoje = DateOnly.FromDateTime(agoraLocal);
                var horaAtual = TimeOnly.FromDateTime(agoraLocal);

                // Janela de disparo entre o horário configurado (ex.: 20:45) e 23:30, uma vez por dia
                if (_ultimaExecucaoData != hoje &&
                    horaAtual >= horarioAlvo &&
                    horaAtual <= new TimeOnly(23, 30))
                {
                    _ultimaExecucaoData = hoje;
                    using var scope = _scopeFactory.CreateScope();
                    var catalogo = scope.ServiceProvider.GetRequiredService<ICatalogoService>();

                    var resultado = await catalogo.ExecutarQueimaEstoqueAsync(
                        new ExecutarQueimaEstoqueRequest(
                            opts.PercentualPadraoQueima,
                            ApenasPereciveis: true,
                            GrupoWhatsAppId: null,
                            DispararWhatsApp: true),
                        stoppingToken);

                    _logger.LogInformation(
                        "Queima de estoque automática executada às {Horario}: {Qtd} produtos em oferta.",
                        agoraLocal.ToString("HH:mm"),
                        resultado.ProdutosAfetados);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro durante verificação agendada de queima de estoque.");
            }
        }
    }

    private static DateTime ObterHorarioBrasilia()
    {
        var utc = DateTime.UtcNow;
        foreach (var tzId in new[] { "America/Sao_Paulo", "E. South America Standard Time" })
        {
            try
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById(tzId);
                return TimeZoneInfo.ConvertTimeFromUtc(utc, tz);
            }
            catch
            {
                // Tenta próximo identificador compatível entre Linux e Windows
            }
        }

        return utc.AddHours(-3);
    }
}
