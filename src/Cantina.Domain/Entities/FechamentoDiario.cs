namespace Cantina.Domain.Entities;

public class FechamentoDiario
{
    public int Id { get; set; }
    public DateOnly DataReferencia { get; private set; }
    public decimal ReceitaNormal { get; private set; }
    public decimal ReceitaRecuperada { get; private set; }
    public decimal DescontosConcedidos { get; private set; }
    public int PedidosWebConcluidos { get; private set; }
    public int VendasBalcaoConcluidas { get; private set; }
    public int ItensVendidosPrecoCheio { get; private set; }
    public int ItensVendidosPromocao { get; private set; }
    public int ItensSobraramEstoque { get; private set; }
    public string? Observacoes { get; private set; }
    public DateTime FechadoEm { get; private set; }

    public decimal ReceitaTotal => ReceitaNormal + ReceitaRecuperada;

    protected FechamentoDiario()
    {
    }

    public FechamentoDiario(
        DateOnly dataReferencia,
        decimal receitaNormal,
        decimal receitaRecuperada,
        decimal descontosConcedidos,
        int pedidosWebConcluidos,
        int vendasBalcaoConcluidas,
        int itensVendidosPrecoCheio,
        int itensVendidosPromocao,
        int itensSobraramEstoque,
        string? observacoes = null,
        DateTime? agoraUtc = null)
    {
        DataReferencia = dataReferencia;
        ReceitaNormal = receitaNormal;
        ReceitaRecuperada = receitaRecuperada;
        DescontosConcedidos = descontosConcedidos;
        PedidosWebConcluidos = pedidosWebConcluidos;
        VendasBalcaoConcluidas = vendasBalcaoConcluidas;
        ItensVendidosPrecoCheio = itensVendidosPrecoCheio;
        ItensVendidosPromocao = itensVendidosPromocao;
        ItensSobraramEstoque = itensSobraramEstoque;
        Observacoes = string.IsNullOrWhiteSpace(observacoes) ? null : observacoes.Trim();
        FechadoEm = agoraUtc ?? DateTime.UtcNow;
    }
}
