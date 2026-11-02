using Cantina.Domain.Entities;
using Cantina.Domain.Exceptions;
using Xunit;

namespace Cantina.Tests.Domain;

public sealed class ProdutoTests
{
    [Fact]
    public void AplicarDescontoPercentual_DeveCalcularPrecoFinalEEconomiaCorretamente()
    {
        var produto = new Produto(
            nome: "Coxinha de Frango com Catupiry",
            descricao: "Salgado assado do dia",
            iconeEmoji: "🍗",
            categoriaId: 1,
            precoCusto: 3.50m,
            precoVenda: 8.00m,
            quantidadeInicial: 20,
            perecivel: true);

        produto.AplicarDescontoPercentual(40m);

        Assert.True(produto.TemDesconto);
        Assert.Equal(4.80m, produto.PrecoFinal);
        Assert.Equal(3.20m, produto.EconomiaUnitaria);
        Assert.Equal(40m, produto.PercentualDescontoEfetivo);
    }

    [Fact]
    public void AplicarDescontoValor_DeveCalcularPrecoFinalELimitarAoPrecoDeVenda()
    {
        var produto = new Produto(
            nome: "Pastel Assado de Carne",
            descricao: "Massa leve",
            iconeEmoji: "🥟",
            categoriaId: 1,
            precoCusto: 3.00m,
            precoVenda: 7.50m,
            quantidadeInicial: 12);

        produto.AplicarDescontoValor(2.50m);

        Assert.True(produto.TemDesconto);
        Assert.Equal(0m, produto.DescontoPercentual);
        Assert.Equal(2.50m, produto.DescontoValor);
        Assert.Equal(5.00m, produto.PrecoFinal);
        Assert.Equal(33m, produto.PercentualDescontoEfetivo);

        // Desconto maior que o preço de venda nunca gera preço negativo
        produto.AplicarDescontoValor(25.00m);
        Assert.Equal(0m, produto.PrecoFinal);
    }

    [Fact]
    public void CicloDeReservaDeEstoque_DeveProtegerDisponibilidadeEBaixarNaEntrega()
    {
        var produto = new Produto(
            nome: "Pão de Queijo Mineiro",
            descricao: "Fornada da tarde",
            iconeEmoji: "🧀",
            categoriaId: 1,
            precoCusto: 1.80m,
            precoVenda: 5.00m,
            quantidadeInicial: 10);

        produto.Reservar(4);

        Assert.Equal(10, produto.QuantidadeEstoque);
        Assert.Equal(4, produto.QuantidadeReservada);
        Assert.Equal(6, produto.QuantidadeDisponivel);

        // Não permite reservar além do disponível
        Assert.Throws<DomainException>(() => produto.Reservar(7));

        // Não permite reduzir estoque físico abaixo da reserva ativa
        Assert.Throws<DomainException>(() => produto.AjustarEstoqueFisico(-8));

        // Confirma retirada de 3 unidades e cancela 1 unidade
        produto.ConfirmarSaidaReserva(3);
        produto.LiberarReserva(1);

        Assert.Equal(7, produto.QuantidadeEstoque);
        Assert.Equal(0, produto.QuantidadeReservada);
        Assert.Equal(7, produto.QuantidadeDisponivel);
    }
}
