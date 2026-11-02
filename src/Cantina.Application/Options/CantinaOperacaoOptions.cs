namespace Cantina.Application.Options;

public sealed class CantinaOperacaoOptions
{
    public const string SectionName = "CantinaOperacao";

    public string NomeCantina { get; set; } = "Cantina ALAMO — SENAC Joinville";
    public string HorarioQueimaAutomatica { get; set; } = "20:45";
    public decimal PercentualPadraoQueima { get; set; } = 40m;
    public decimal MetaReceitaRecuperadaDiaria { get; set; } = 150.00m;
    public string UrlPublicaCardapio { get; set; } = "http://localhost:5180";
    public bool HabilitarWorkerQueimaAutomatica { get; set; } = true;
}
