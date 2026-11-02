using Cantina.Domain.Enums;

namespace Cantina.Domain.Entities;

public class NotificacaoWhatsAppLog
{
    public int Id { get; set; }
    public TipoNotificacaoWhatsApp Tipo { get; private set; }
    public CanalEnvioWhatsApp Canal { get; private set; }
    public string Destinatario { get; private set; } = string.Empty;
    public string Mensagem { get; private set; } = string.Empty;
    public bool EnviadoAutomaticamente { get; private set; }
    public string LinkFallbackWaMe { get; private set; } = string.Empty;
    public string DetalheStatus { get; private set; } = string.Empty;
    public int? PedidoId { get; private set; }
    public DateTime CriadaEm { get; private set; }

    protected NotificacaoWhatsAppLog()
    {
    }

    public NotificacaoWhatsAppLog(
        TipoNotificacaoWhatsApp tipo,
        CanalEnvioWhatsApp canal,
        string destinatario,
        string mensagem,
        bool enviadoAutomaticamente,
        string linkFallbackWaMe,
        string detalheStatus,
        int? pedidoId = null,
        DateTime? agoraUtc = null)
    {
        Tipo = tipo;
        Canal = canal;
        Destinatario = (destinatario ?? string.Empty).Trim();
        Mensagem = mensagem ?? string.Empty;
        EnviadoAutomaticamente = enviadoAutomaticamente;
        LinkFallbackWaMe = linkFallbackWaMe ?? string.Empty;
        DetalheStatus = (detalheStatus ?? string.Empty).Trim();
        PedidoId = pedidoId;
        CriadaEm = agoraUtc ?? DateTime.UtcNow;
    }
}
