using Cantina.Domain.Enums;
using Cantina.Domain.Exceptions;

namespace Cantina.Domain.Entities;

public class Pedido
{
    private readonly List<ItemPedido> _itens = new();

    public int Id { get; set; }
    public string CodigoRetirada { get; private set; } = string.Empty;
    public OrigemPedido Origem { get; private set; }
    public StatusPedido Status { get; private set; }

    public string NomeCliente { get; private set; } = string.Empty;
    public string TelefoneWhatsApp { get; private set; } = string.Empty;
    public string? MatriculaAluno { get; private set; }
    public TurnoSenac Turno { get; private set; }
    public FormaPagamento FormaPagamento { get; private set; }
    public string? Observacoes { get; private set; }

    public decimal SubtotalOriginal { get; private set; }
    public decimal DescontoTotal { get; private set; }
    public decimal ValorTotal { get; private set; }
    public decimal ReceitaRecuperada { get; private set; }
    public decimal ReceitaNormal { get; private set; }

    public DateTime CriadoEm { get; private set; }
    public DateTime AtualizadoEm { get; private set; }
    public DateTime? ProntoEm { get; private set; }
    public DateTime? ConcluidoEm { get; private set; }

    public IReadOnlyCollection<ItemPedido> Itens => _itens.AsReadOnly();

    public bool ReservaEstoqueAtiva =>
        Status is StatusPedido.Recebido or StatusPedido.EmPreparo or StatusPedido.ProntoParaRetirada;

    protected Pedido()
    {
    }

    public Pedido(
        OrigemPedido origem,
        string nomeCliente,
        string? telefoneWhatsApp,
        string? matriculaAluno,
        TurnoSenac turno,
        FormaPagamento formaPagamento,
        string? observacoes,
        IEnumerable<ItemPedido> itens,
        string? codigoRetirada = null,
        DateTime? agoraUtc = null)
    {
        if (string.IsNullOrWhiteSpace(nomeCliente))
            throw new DomainException("Informe o nome do cliente para identificar a retirada do pedido.");

        var listaItens = itens?.ToList() ?? new List<ItemPedido>();
        if (listaItens.Count == 0)
            throw new DomainException("O pedido deve conter pelo menos um item.");

        var telLimpo = NormalizarTelefone(telefoneWhatsApp);
        if (origem == OrigemPedido.SiteWeb && telLimpo.Length < 10)
            throw new DomainException("Informe um número de WhatsApp válido com DDD para receber os avisos do pedido.");

        var now = agoraUtc ?? DateTime.UtcNow;

        CodigoRetirada = string.IsNullOrWhiteSpace(codigoRetirada) ? GerarCodigoRetirada() : codigoRetirada.Trim().ToUpperInvariant();
        Origem = origem;
        Status = origem == OrigemPedido.BalcaoDesktop ? StatusPedido.Concluido : StatusPedido.Recebido;
        NomeCliente = nomeCliente.Trim();
        TelefoneWhatsApp = telLimpo;
        MatriculaAluno = string.IsNullOrWhiteSpace(matriculaAluno) ? null : matriculaAluno.Trim();
        Turno = turno;
        FormaPagamento = formaPagamento;
        Observacoes = string.IsNullOrWhiteSpace(observacoes) ? null : observacoes.Trim();

        CriadoEm = now;
        AtualizadoEm = now;
        if (Status == StatusPedido.Concluido)
        {
            ConcluidoEm = now;
        }

        _itens.AddRange(listaItens);
        RecalcularTotais();
    }

    public void IniciarPreparo(DateTime? agoraUtc = null)
    {
        if (Status != StatusPedido.Recebido)
            throw new DomainException($"Não é possível iniciar preparo de um pedido com status '{Status}'.");

        Status = StatusPedido.EmPreparo;
        AtualizadoEm = agoraUtc ?? DateTime.UtcNow;
    }

    public void MarcarProntoParaRetirada(DateTime? agoraUtc = null)
    {
        if (Status is not (StatusPedido.Recebido or StatusPedido.EmPreparo))
            throw new DomainException($"Não é possível marcar como pronto um pedido com status '{Status}'.");

        var now = agoraUtc ?? DateTime.UtcNow;
        Status = StatusPedido.ProntoParaRetirada;
        ProntoEm = now;
        AtualizadoEm = now;
    }

    public void ConcluirEntrega(DateTime? agoraUtc = null)
    {
        if (Status is StatusPedido.Concluido or StatusPedido.Cancelado)
            throw new DomainException($"O pedido já está finalizado com status '{Status}'.");

        var now = agoraUtc ?? DateTime.UtcNow;
        Status = StatusPedido.Concluido;
        ConcluidoEm = now;
        AtualizadoEm = now;
    }

    public void Cancelar(DateTime? agoraUtc = null)
    {
        if (Status == StatusPedido.Concluido)
            throw new DomainException("Não é possível cancelar um pedido que já foi entregue.");

        if (Status == StatusPedido.Cancelado)
            throw new DomainException("O pedido já está cancelado.");

        Status = StatusPedido.Cancelado;
        AtualizadoEm = agoraUtc ?? DateTime.UtcNow;
    }

    private void RecalcularTotais()
    {
        SubtotalOriginal = _itens.Sum(i => i.SubtotalOriginal);
        ValorTotal = _itens.Sum(i => i.SubtotalFinal);
        DescontoTotal = Math.Max(0m, SubtotalOriginal - ValorTotal);
        ReceitaRecuperada = _itens.Where(i => i.ItemPromocional).Sum(i => i.SubtotalFinal);
        ReceitaNormal = _itens.Where(i => !i.ItemPromocional).Sum(i => i.SubtotalFinal);
    }

    public static string NormalizarTelefone(string? telefone)
    {
        if (string.IsNullOrWhiteSpace(telefone))
            return string.Empty;

        var digitos = new string(telefone.Where(char.IsDigit).ToArray());
        if (digitos.Length is 10 or 11 && !digitos.StartsWith("55", StringComparison.Ordinal))
        {
            digitos = "55" + digitos;
        }

        return digitos;
    }

    public static string GerarCodigoRetirada()
    {
        const string alfabeto = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        Span<char> sufixo = stackalloc char[4];
        for (int i = 0; i < sufixo.Length; i++)
        {
            sufixo[i] = alfabeto[Random.Shared.Next(alfabeto.Length)];
        }

        return $"AL-{new string(sufixo)}";
    }
}

public class ItemPedido
{
    public int Id { get; set; }
    public int PedidoId { get; set; }
    public int ProdutoId { get; private set; }
    public string NomeProduto { get; private set; } = string.Empty;
    public int Quantidade { get; private set; }
    public decimal PrecoUnitarioOriginal { get; private set; }
    public decimal PrecoUnitarioFinal { get; private set; }
    public bool ItemPromocional { get; private set; }

    public decimal SubtotalOriginal => Quantidade * PrecoUnitarioOriginal;
    public decimal SubtotalFinal => Quantidade * PrecoUnitarioFinal;

    protected ItemPedido()
    {
    }

    public ItemPedido(Produto produto, int quantidade)
    {
        if (produto is null)
            throw new DomainException("Produto inválido no item do pedido.");

        if (quantidade <= 0)
            throw new DomainException("A quantidade do item deve ser maior que zero.");

        ProdutoId = produto.Id;
        NomeProduto = produto.Nome;
        Quantidade = quantidade;
        PrecoUnitarioOriginal = produto.PrecoVenda;
        PrecoUnitarioFinal = produto.PrecoFinal;
        ItemPromocional = produto.TemDesconto && produto.PrecoFinal < produto.PrecoVenda;
    }
}
