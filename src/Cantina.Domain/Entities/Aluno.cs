using Cantina.Domain.Enums;
using Cantina.Domain.Exceptions;

namespace Cantina.Domain.Entities;

public class Aluno
{
    public int Id { get; set; }
    public string Nome { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string TelefoneWhatsApp { get; private set; } = string.Empty;
    public string Matricula { get; private set; } = string.Empty;
    public TurnoSenac Turno { get; private set; }
    public bool ReceberAlertasPromocao { get; private set; } = true;
    public DateTime CriadoEm { get; private set; }

    protected Aluno()
    {
    }

    public Aluno(
        string nome,
        string email,
        string? telefoneWhatsApp,
        string? matricula,
        TurnoSenac turno,
        bool receberAlertasPromocao = true,
        DateTime? agoraUtc = null)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new DomainException("O nome do aluno/cliente é obrigatório.");

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            throw new DomainException("Informe um e-mail válido para o aluno/cliente.");

        Nome = nome.Trim();
        Email = email.Trim().ToLowerInvariant();
        TelefoneWhatsApp = Pedido.NormalizarTelefone(telefoneWhatsApp);
        Matricula = (matricula ?? string.Empty).Trim();
        Turno = turno;
        ReceberAlertasPromocao = receberAlertasPromocao;
        CriadoEm = agoraUtc ?? DateTime.UtcNow;
    }
}
