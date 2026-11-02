using Cantina.Domain.Exceptions;

namespace Cantina.Domain.Entities;

public class Produto
{
    public int Id { get; set; }
    public int CategoriaId { get; private set; }
    public Categoria? Categoria { get; private set; }

    public string Nome { get; private set; } = string.Empty;
    public string Descricao { get; private set; } = string.Empty;
    public string IconeEmoji { get; private set; } = "🥐";

    public decimal PrecoCusto { get; private set; }
    public decimal PrecoVenda { get; private set; }
    public decimal DescontoPercentual { get; private set; }
    public decimal DescontoValor { get; private set; }

    public int QuantidadeEstoque { get; private set; }
    public int QuantidadeReservada { get; private set; }

    public bool Perecivel { get; private set; } = true;
    public bool Ativo { get; private set; } = true;
    public DateTime CriadoEm { get; private set; }
    public DateTime AtualizadoEm { get; private set; }

    public int QuantidadeDisponivel => Math.Max(0, QuantidadeEstoque - QuantidadeReservada);

    public bool TemDesconto => DescontoPercentual > 0m || DescontoValor > 0m;

    public decimal PrecoFinal
    {
        get
        {
            decimal precoCalculado = PrecoVenda;

            if (DescontoPercentual > 0m)
            {
                precoCalculado -= PrecoVenda * (DescontoPercentual / 100m);
            }
            else if (DescontoValor > 0m)
            {
                precoCalculado -= DescontoValor;
            }

            return Math.Max(0m, Math.Round(precoCalculado, 2, MidpointRounding.AwayFromZero));
        }
    }

    public decimal EconomiaUnitaria => Math.Max(0m, PrecoVenda - PrecoFinal);

    public decimal PercentualDescontoEfetivo =>
        PrecoVenda > 0m
            ? Math.Round((EconomiaUnitaria / PrecoVenda) * 100m, 0, MidpointRounding.AwayFromZero)
            : 0m;

    protected Produto()
    {
    }

    public Produto(
        string nome,
        string descricao,
        string iconeEmoji,
        int categoriaId,
        decimal precoCusto,
        decimal precoVenda,
        int quantidadeInicial,
        bool perecivel = true,
        DateTime? agoraUtc = null)
    {
        var now = agoraUtc ?? DateTime.UtcNow;
        CriadoEm = now;
        AtualizadoEm = now;
        Ativo = true;

        AtualizarDados(nome, descricao, iconeEmoji, categoriaId, precoCusto, precoVenda, perecivel, ativo: true, now);

        if (quantidadeInicial < 0)
            throw new DomainException("A quantidade inicial de estoque não pode ser negativa.");

        QuantidadeEstoque = quantidadeInicial;
        QuantidadeReservada = 0;
    }

    public void AtualizarDados(
        string nome,
        string descricao,
        string iconeEmoji,
        int categoriaId,
        decimal precoCusto,
        decimal precoVenda,
        bool perecivel,
        bool ativo,
        DateTime? agoraUtc = null)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new DomainException("O nome do produto é obrigatório.");

        if (categoriaId <= 0)
            throw new DomainException("A categoria do produto é obrigatória.");

        if (precoVenda <= 0m)
            throw new DomainException("O preço de venda deve ser maior que zero.");

        if (precoCusto < 0m)
            throw new DomainException("O preço de custo não pode ser negativo.");

        Nome = nome.Trim();
        Descricao = (descricao ?? string.Empty).Trim();
        IconeEmoji = string.IsNullOrWhiteSpace(iconeEmoji) ? "🥐" : iconeEmoji.Trim();
        CategoriaId = categoriaId;
        PrecoCusto = Math.Round(precoCusto, 2, MidpointRounding.AwayFromZero);
        PrecoVenda = Math.Round(precoVenda, 2, MidpointRounding.AwayFromZero);
        Perecivel = perecivel;
        Ativo = ativo;

        if (DescontoValor > PrecoVenda)
        {
            DescontoValor = PrecoVenda;
        }

        AtualizadoEm = agoraUtc ?? DateTime.UtcNow;
    }

    public void AplicarDescontoPercentual(decimal percentual, DateTime? agoraUtc = null)
    {
        if (percentual < 0m || percentual > 100m)
            throw new DomainException("O desconto percentual deve estar entre 0% e 100%.");

        DescontoPercentual = Math.Round(percentual, 2, MidpointRounding.AwayFromZero);
        DescontoValor = 0m;
        AtualizadoEm = agoraUtc ?? DateTime.UtcNow;
    }

    public void AplicarDescontoValor(decimal valor, DateTime? agoraUtc = null)
    {
        if (valor < 0m)
            throw new DomainException("O valor de desconto não pode ser negativo.");

        DescontoValor = Math.Min(PrecoVenda, Math.Round(valor, 2, MidpointRounding.AwayFromZero));
        DescontoPercentual = 0m;
        AtualizadoEm = agoraUtc ?? DateTime.UtcNow;
    }

    public void ZerarDesconto(DateTime? agoraUtc = null)
    {
        DescontoPercentual = 0m;
        DescontoValor = 0m;
        AtualizadoEm = agoraUtc ?? DateTime.UtcNow;
    }

    public void AjustarEstoqueFisico(int delta, DateTime? agoraUtc = null)
    {
        int novoSaldo = QuantidadeEstoque + delta;

        if (novoSaldo < 0)
            throw new DomainException($"Estoque insuficiente para '{Nome}'. Saldo atual: {QuantidadeEstoque}.");

        if (novoSaldo < QuantidadeReservada)
            throw new DomainException($"Não é possível reduzir o estoque de '{Nome}' abaixo da quantidade já reservada por pedidos ({QuantidadeReservada} un.).");

        QuantidadeEstoque = novoSaldo;
        AtualizadoEm = agoraUtc ?? DateTime.UtcNow;
    }

    public void Reservar(int quantidade, DateTime? agoraUtc = null)
    {
        if (!Ativo)
            throw new DomainException($"O produto '{Nome}' não está disponível para pedidos no momento.");

        if (quantidade <= 0)
            throw new DomainException("A quantidade solicitada deve ser maior que zero.");

        if (quantidade > QuantidadeDisponivel)
            throw new DomainException($"Quantidade indisponível para '{Nome}'. Disponível: {QuantidadeDisponivel} un.");

        QuantidadeReservada += quantidade;
        AtualizadoEm = agoraUtc ?? DateTime.UtcNow;
    }

    public void ConfirmarSaidaReserva(int quantidade, DateTime? agoraUtc = null)
    {
        if (quantidade <= 0 || quantidade > QuantidadeReservada)
            throw new DomainException($"Inconsistência ao confirmar retirada da reserva para '{Nome}'.");

        QuantidadeReservada -= quantidade;
        QuantidadeEstoque = Math.Max(0, QuantidadeEstoque - quantidade);
        AtualizadoEm = agoraUtc ?? DateTime.UtcNow;
    }

    public void LiberarReserva(int quantidade, DateTime? agoraUtc = null)
    {
        if (quantidade <= 0 || quantidade > QuantidadeReservada)
            throw new DomainException($"Inconsistência ao liberar reserva de '{Nome}'.");

        QuantidadeReservada -= quantidade;
        AtualizadoEm = agoraUtc ?? DateTime.UtcNow;
    }

    public void BaixarVendaDireta(int quantidade, DateTime? agoraUtc = null)
    {
        if (quantidade <= 0)
            throw new DomainException("A quantidade da venda deve ser maior que zero.");

        if (quantidade > QuantidadeDisponivel)
            throw new DomainException($"Estoque disponível insuficiente para '{Nome}'. Disponível: {QuantidadeDisponivel} un.");

        QuantidadeEstoque -= quantidade;
        AtualizadoEm = agoraUtc ?? DateTime.UtcNow;
    }
}

