using System.Globalization;
using System.Net;
using System.Text;
using Cantina.Domain.Entities;

namespace Cantina.Application.Formatting;

public static class WhatsAppMessageFormatter
{
    private static readonly CultureInfo PtBr = new("pt-BR");

    public static string FormatarMoeda(decimal valor) =>
        valor.ToString("C2", PtBr);

    public static string GerarLinkWaMe(string? telefone, string mensagem)
    {
        var telLimpo = Pedido.NormalizarTelefone(telefone);
        var textoCodificado = Uri.EscapeDataString(mensagem ?? string.Empty);
        return string.IsNullOrWhiteSpace(telLimpo)
            ? $"https://wa.me/?text={textoCodificado}"
            : $"https://wa.me/{telLimpo}?text={textoCodificado}";
    }

    public static string FormatarCampanhaAntidesperdicio(
        string nomeCantina,
        IEnumerable<Produto> produtos,
        string? urlCardapioOnline = null)
    {
        // Corrige o bug legado: inclui TANTO descontos percentuais QUANTO descontos fixos em R$!
        var ofertados = (produtos ?? Enumerable.Empty<Produto>())
            .Where(p => p.Ativo && p.TemDesconto && p.QuantidadeDisponivel > 0)
            .OrderByDescending(p => p.PercentualDescontoEfetivo)
            .ThenBy(p => p.Nome)
            .ToList();

        if (ofertados.Count == 0)
            return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine($"🔥 *CARDÁPIO ANTIDESPERDÍCIO — {nomeCantina.ToUpperInvariant()}* 🔥");
        sb.AppendLine("_Não vendeu, não desperdice! Ofertas especiais de fim de turno:_");
        sb.AppendLine();

        foreach (var p in ofertados)
        {
            sb.AppendLine($"{p.IconeEmoji} *{p.Nome}* ({p.QuantidadeDisponivel} disp.)");
            sb.AppendLine($"   De ~{FormatarMoeda(p.PrecoVenda)}~ por *{FormatarMoeda(p.PrecoFinal)}* (-{p.PercentualDescontoEfetivo:0}%)");
        }

        if (!string.IsNullOrWhiteSpace(urlCardapioOnline))
        {
            sb.AppendLine();
            sb.AppendLine($"👉 *Peça sem fila pelo site:* {urlCardapioOnline.Trim()}");
        }

        return sb.ToString().TrimEnd();
    }

    public static string FormatarConfirmacaoPedido(Pedido pedido, string nomeCantina, string? urlCardapio = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"✅ *{nomeCantina} — Pedido Recebido!*");
        sb.AppendLine($"Olá, *{pedido.NomeCliente}*! Seu pedido *#{pedido.CodigoRetirada}* foi registrado.");
        sb.AppendLine();
        sb.AppendLine("*Itens reservados:*");

        foreach (var item in pedido.Itens)
        {
            var tagOferta = item.ItemPromocional ? " _(Oferta Antidesperdício)_" : string.Empty;
            sb.AppendLine($"• {item.Quantidade}x {item.NomeProduto} — {FormatarMoeda(item.SubtotalFinal)}{tagOferta}");
        }

        sb.AppendLine();
        sb.AppendLine($"💰 *Total:* {FormatarMoeda(pedido.ValorTotal)}");
        if (pedido.DescontoTotal > 0m)
        {
            sb.AppendLine($"🌱 *Você economizou:* {FormatarMoeda(pedido.DescontoTotal)} evitando desperdício!");
        }

        sb.AppendLine("Avisaremos aqui no WhatsApp assim que estiver *pronto para retirada sem fila*!");
        if (!string.IsNullOrWhiteSpace(urlCardapio))
        {
            sb.AppendLine($"🔎 Acompanhe: {urlCardapio.TrimEnd('/')}/?pedido={WebUtility.UrlEncode(pedido.CodigoRetirada)}");
        }

        return sb.ToString().TrimEnd();
    }

    public static string FormatarPedidoEmPreparo(Pedido pedido, string nomeCantina) =>
        $"👩‍🍳 *{nomeCantina}:* Olá, *{pedido.NomeCliente}*! Seu pedido *#{pedido.CodigoRetirada}* já entrou em preparo.";

    public static string FormatarPedidoPronto(Pedido pedido, string nomeCantina) =>
        $"🔔 *PEDIDO PRONTO PARA RETIRADA!*\n\n" +
        $"Olá, *{pedido.NomeCliente}*! Passe direto no balcão *Fura-Fila* da *{nomeCantina}* e informe o código *#{pedido.CodigoRetirada}*.\n" +
        $"Valor total: *{FormatarMoeda(pedido.ValorTotal)}*. Bom lanche!";

    public static string FormatarPedidoCancelado(Pedido pedido, string nomeCantina) =>
        $"⚠️ *{nomeCantina}:* Olá, *{pedido.NomeCliente}*. O pedido *#{pedido.CodigoRetirada}* foi cancelado e os itens retornaram ao estoque.";
}

