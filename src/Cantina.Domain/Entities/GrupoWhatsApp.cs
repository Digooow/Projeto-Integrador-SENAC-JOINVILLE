using Cantina.Domain.Enums;
using Cantina.Domain.Exceptions;

namespace Cantina.Domain.Entities;

public class GrupoWhatsApp
{
    public int Id { get; set; }
    public string Nome { get; private set; } = string.Empty;
    public TurnoSenac Turno { get; private set; }
    public string LinkConvite { get; private set; } = string.Empty;
    public string? DestinoApiJid { get; private set; }
    public int ParticipantesEstimados { get; private set; }
    public bool Ativo { get; private set; } = true;

    protected GrupoWhatsApp()
    {
    }

    public GrupoWhatsApp(
        string nome,
        TurnoSenac turno,
        string linkConvite,
        int participantesEstimados,
        string? destinoApiJid = null,
        bool ativo = true)
    {
        Atualizar(nome, turno, linkConvite, participantesEstimados, destinoApiJid, ativo);
    }

    public void Atualizar(
        string nome,
        TurnoSenac turno,
        string linkConvite,
        int participantesEstimados,
        string? destinoApiJid,
        bool ativo)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new DomainException("O nome do grupo de WhatsApp é obrigatório.");

        Nome = nome.Trim();
        Turno = turno;
        LinkConvite = (linkConvite ?? string.Empty).Trim();
        ParticipantesEstimados = Math.Max(0, participantesEstimados);
        DestinoApiJid = string.IsNullOrWhiteSpace(destinoApiJid) ? null : destinoApiJid.Trim();
        Ativo = ativo;
    }
}

