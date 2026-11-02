using Cantina.Domain.Entities;
using Cantina.Domain.Enums;
using Cantina.Domain.Exceptions;
using Xunit;

namespace Cantina.Tests.Domain;

public sealed class PedidoTests
{
    [Fact]
    public void CriarPedidoWeb_DeveNormalizarTelefoneESepararReceitaRecuperadaDeReceitaNormal()
    {
        var salgadoOferta = new Produto("Esfirra de Carne", "Fornada do dia", "🥙", 1, 2.50m, 7.00m, 10, perecivel: true);
        salgadoOferta.Id = 1;
        salgadoOferta.AplicarDescontoPercentual(50m); // R$ 3,50 cada

        var bebidaNormal = new Produto("Refrigerante Lata", "350ml gelado", "🥤", 2, 2.80m, 6.00m, 20, perecivel: false);
        bebidaNormal.Id = 2;

        var pedido = new Pedido(
            origem: OrigemPedido.SiteWeb,
            nomeCliente: "Lucas Andrade",
            telefoneWhatsApp: "(47) 99123-4567",
            matriculaAluno: "2026105",
            turno: TurnoSenac.Noite,
            formaPagamento: FormaPagamento.PixNaRetirada,
            observacoes: "Sem canudo",
            itens:
            [
                new ItemPedido(salgadoOferta, 2), // Original 14,00 -> Final 7,00 (Recuperada)
                new ItemPedido(bebidaNormal, 1)   // Original  6,00 -> Final 6,00 (Normal)
            ]);

        Assert.Equal("5547991234567", pedido.TelefoneWhatsApp);
        Assert.Equal(StatusPedido.Recebido, pedido.Status);
        Assert.Equal(20.00m, pedido.SubtotalOriginal);
        Assert.Equal(13.00m, pedido.ValorTotal);
        Assert.Equal(7.00m, pedido.DescontoTotal);
        Assert.Equal(7.00m, pedido.ReceitaRecuperada);
        Assert.Equal(6.00m, pedido.ReceitaNormal);
    }

    [Fact]
    public void TransicoesDeStatus_DevemRespeitarMaquinaDeEstadosDoPedido()
    {
        var produto = new Produto("Bolo de Cenoura", "Cobertura de chocolate", "🍰", 3, 3.00m, 8.00m, 8);
        produto.Id = 10;

        var pedido = new Pedido(
            OrigemPedido.SiteWeb,
            "Mariana Costa",
            "47988776655",
            null,
            TurnoSenac.Tarde,
            FormaPagamento.PixNaRetirada,
            null,
            [new ItemPedido(produto, 1)]);

        pedido.IniciarPreparo();
        Assert.Equal(StatusPedido.EmPreparo, pedido.Status);

        pedido.MarcarProntoParaRetirada();
        Assert.Equal(StatusPedido.ProntoParaRetirada, pedido.Status);
        Assert.NotNull(pedido.ProntoEm);

        pedido.ConcluirEntrega();
        Assert.Equal(StatusPedido.Concluido, pedido.Status);
        Assert.NotNull(pedido.ConcluidoEm);

        // Pedido já entregue não pode ser cancelado
        Assert.Throws<DomainException>(() => pedido.Cancelar());
    }
}
