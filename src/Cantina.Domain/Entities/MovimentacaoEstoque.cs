using Cantina.Domain.Enums;

namespace Cantina.Domain.Entities;

public class MovimentacaoEstoque
{
    public int Id { get; set; }
    public int ProdutoId { get; private set; }
    public string NomeProduto { get; private set; } = string.Empty;
    public TipoMovimentacaoEstoque Tipo { get; private set; }
    public int QuantidadeDelta { get; private set; }
    public int SaldoPosterior { get; private set; }
    public string Motivo { get; private set; } = string.Empty;
    public DateTime OcorridaEm { get; private set; }

    protected MovimentacaoEstoque()
    {
    }

    public MovimentacaoEstoque(
        Produto produto,
        TipoMovimentacaoEstoque tipo,
        int quantidadeDelta,
        string motivo,
        DateTime? agoraUtc = null)
    {
        ProdutoId = produto.Id;
        NomeProduto = produto.Nome;
        Tipo = tipo;
        QuantidadeDelta = quantidadeDelta;
        SaldoPosterior = produto.QuantidadeEstoque;
        Motivo = (motivo ?? string.Empty).Trim();
        OcorridaEm = agoraUtc ?? DateTime.UtcNow;
    }
}
