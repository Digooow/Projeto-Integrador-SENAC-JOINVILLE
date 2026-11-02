using Cantina.Domain.Enums;

namespace Cantina.Application.Abstractions;

public sealed record ResultadoEnvioWhatsApp(
    CanalEnvioWhatsApp CanalUtilizado,
    bool EnviadoAutomaticamente,
    string LinkFallbackWaMe,
    string DetalheStatus);

public interface IWhatsAppGateway
{
    string NomeModoAtivo { get; }

    Task<ResultadoEnvioWhatsApp> EnviarMensagemIndividualAsync(
        string telefoneDestino,
        string mensagem,
        CancellationToken cancellationToken = default);

    Task<ResultadoEnvioWhatsApp> EnviarMensagemGrupoAsync(
        string nomeGrupo,
        string? destinoApiJid,
        string mensagem,
        CancellationToken cancellationToken = default);
}
