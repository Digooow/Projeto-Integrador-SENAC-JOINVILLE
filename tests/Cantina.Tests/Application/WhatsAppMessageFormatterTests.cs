using Cantina.Application.Formatting;
using Cantina.Domain.Entities;
using Cantina.Domain.Enums;
using Xunit;

namespace Cantina.Tests.Application;

public sealed class WhatsAppMessageFormatterTests
{
    [Fact]
    public void FormatarCampanhaAntidesperdicio_DeveIncluirTantoDescontoPercentualQuantoValorFixoEmReais()
    {
        var itemPercentual = new Produto("Croissant Presunto e Queijo", "Folhado", "🥐", 1, 3.50m, 9.00m, 6);
        itemPercentual.AplicarDescontoPercentual(40m); // R$ 5,40

        var itemValorFixo = new Produto("Sanduíche Natural de Frango", "Pão integral", "🥪", 1, 4.00m, 10.00m, 4);
        itemValorFixo.AplicarDescontoValor(3.00m); // R$ 7,00 (Regressão do bug legado!)

        var itemSemDesconto = new Produto("Água Mineral", "500ml", "💧", 2, 1.00m, 3.50m, 30, perecivel: false);

        var itemEsgotado = new Produto("Empada de Palmito", "Assada", "🥧", 1, 3.00m, 7.50m, 0);
        itemEsgotado.AplicarDescontoPercentual(50m);

        var texto = WhatsAppMessageFormatter.FormatarCampanhaAntidesperdicio(
            "Cantina ALAMO",
            [itemPercentual, itemValorFixo, itemSemDesconto, itemEsgotado],
            "https://cantina-senac.onrender.com");

        Assert.Contains("Croissant Presunto e Queijo", texto);
        Assert.Contains("Sanduíche Natural de Frango", texto);
        Assert.DoesNotContain("Água Mineral", texto);
        Assert.DoesNotContain("Empada de Palmito", texto);
        Assert.Contains("https://cantina-senac.onrender.com", texto);
    }

    [Fact]
    public void GerarLinkWaMe_DeveCodificarMensagemENormalizarDdiBrasil()
    {
        var produto = new Produto("Enroladinho", "Assado", "🌭", 1, 2.50m, 6.50m, 10);
        produto.Id = 1;

        var pedido = new Pedido(
            OrigemPedido.SiteWeb,
            "Ana Paula",
            "(47) 99911-2233",
            null,
            TurnoSenac.Noite,
            FormaPagamento.PixNaRetirada,
            null,
            [new ItemPedido(produto, 1)],
            codigoRetirada: "AL-9K2M");

        var msg = WhatsAppMessageFormatter.FormatarPedidoPronto(pedido, "Cantina ALAMO");
        var link = WhatsAppMessageFormatter.GerarLinkWaMe(pedido.TelefoneWhatsApp, msg);

        Assert.Contains("#AL-9K2M", msg);
        Assert.StartsWith("https://wa.me/5547999112233?text=", link);
    }
}
