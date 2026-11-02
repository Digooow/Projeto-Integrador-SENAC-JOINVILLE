using Cantina.Contracts.Dtos;
using Cantina.Domain.Entities;
using Cantina.Domain.Enums;
using Cantina.Domain.Exceptions;

namespace Cantina.Application.Services;

public static class DtoMapper
{
    public static CategoriaDto ToDto(this Categoria c) =>
        new(c.Id, c.Nome, c.IconeEmoji, c.PerecivelDiario);

    public static ProdutoDto ToDto(this Produto p) =>
        new(
            p.Id,
            p.CategoriaId,
            p.Categoria?.Nome ?? "Geral",
            p.Nome,
            p.Descricao,
            p.IconeEmoji,
            p.PrecoCusto,
            p.PrecoVenda,
            p.DescontoPercentual,
            p.DescontoValor,
            p.PrecoFinal,
            p.EconomiaUnitaria,
            p.PercentualDescontoEfetivo,
            p.TemDesconto,
            p.QuantidadeEstoque,
            p.QuantidadeReservada,
            p.QuantidadeDisponivel,
            p.Perecivel,
            p.Ativo);

    public static PedidoDto ToDto(this Pedido p, string? ultimoLinkWhatsApp = null) =>
        new(
            p.Id,
            p.CodigoRetirada,
            p.Origem.ToString(),
            p.Status.ToString(),
            p.NomeCliente,
            p.TelefoneWhatsApp,
            p.MatriculaAluno,
            p.Turno.ToString(),
            p.FormaPagamento.ToString(),
            p.Observacoes,
            p.SubtotalOriginal,
            p.DescontoTotal,
            p.ValorTotal,
            p.ReceitaRecuperada,
            p.ReceitaNormal,
            p.CriadoEm,
            p.ProntoEm,
            p.ConcluidoEm,
            p.Itens.Select(i => new ItemPedidoDto(
                i.ProdutoId,
                i.NomeProduto,
                i.Quantidade,
                i.PrecoUnitarioOriginal,
                i.PrecoUnitarioFinal,
                i.SubtotalFinal,
                i.ItemPromocional)).ToList(),
            ultimoLinkWhatsApp);

    public static GrupoWhatsAppDto ToDto(this GrupoWhatsApp g) =>
        new(g.Id, g.Nome, g.Turno.ToString(), g.LinkConvite, g.DestinoApiJid, g.ParticipantesEstimados, g.Ativo);

    public static AlunoDto ToDto(this Aluno a) =>
        new(a.Id, a.Nome, a.Email, a.TelefoneWhatsApp, a.Matricula, a.Turno.ToString(), a.ReceberAlertasPromocao, a.CriadoEm);

    public static NotificacaoWhatsAppDto ToDto(this NotificacaoWhatsAppLog n) =>
        new(
            n.Id,
            n.Tipo.ToString(),
            n.Canal.ToString(),
            n.Destinatario,
            n.Mensagem,
            n.EnviadoAutomaticamente,
            n.LinkFallbackWaMe,
            n.DetalheStatus,
            n.PedidoId,
            n.CriadaEm);

    public static MovimentacaoEstoqueDto ToDto(this MovimentacaoEstoque m) =>
        new(m.Id, m.ProdutoId, m.NomeProduto, m.Tipo.ToString(), m.QuantidadeDelta, m.SaldoPosterior, m.Motivo, m.OcorridaEm);

    public static FechamentoDiarioDto ToDto(this FechamentoDiario f) =>
        new(
            f.Id,
            f.DataReferencia,
            f.ReceitaNormal,
            f.ReceitaRecuperada,
            f.ReceitaTotal,
            f.DescontosConcedidos,
            f.PedidosWebConcluidos,
            f.VendasBalcaoConcluidas,
            f.ItensVendidosPrecoCheio,
            f.ItensVendidosPromocao,
            f.ItensSobraramEstoque,
            f.Observacoes,
            f.FechadoEm);

    public static TurnoSenac ParseTurno(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return TurnoSenac.Noite;

        if (Enum.TryParse<TurnoSenac>(valor.Trim(), ignoreCase: true, out var turno))
            return turno;

        return valor.Trim().ToLowerInvariant() switch
        {
            "manhã" or "dia" => TurnoSenac.Manha,
            "tarde" => TurnoSenac.Tarde,
            "noite" => TurnoSenac.Noite,
            _ => throw new DomainException($"Turno inválido: '{valor}'. Use Manha, Tarde ou Noite.")
        };
    }

    public static FormaPagamento ParseFormaPagamento(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return FormaPagamento.PixNaRetirada;

        if (Enum.TryParse<FormaPagamento>(valor.Trim(), ignoreCase: true, out var forma))
            return forma;

        throw new DomainException($"Forma de pagamento inválida: '{valor}'.");
    }

    public static StatusPedido ParseStatusPedido(string? valor)
    {
        if (!string.IsNullOrWhiteSpace(valor) &&
            Enum.TryParse<StatusPedido>(valor.Trim(), ignoreCase: true, out var status))
        {
            return status;
        }

        throw new DomainException($"Status de pedido inválido: '{valor}'.");
    }
}

