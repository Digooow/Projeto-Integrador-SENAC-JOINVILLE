using Cantina.Domain.Entities;
using Cantina.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Cantina.Infrastructure.Persistence;

public static class CantinaDatabaseSeeder
{
    public static async Task InitializeAndSeedAsync(CantinaDbContext db, CancellationToken ct = default)
    {
        await db.Database.EnsureCreatedAsync(ct);

        if (await db.Categorias.AnyAsync(ct))
            return;

        var catAssados = new Categoria("Salgados Assados", "🥐", perecivelDiario: true, ordemExibicao: 1);
        var catFritos = new Categoria("Salgados Fritos", "🍗", perecivelDiario: true, ordemExibicao: 2);
        var catLanches = new Categoria("Lanches & Especiais", "🌭", perecivelDiario: true, ordemExibicao: 3);
        var catDoces = new Categoria("Doces & Bolos", "🍰", perecivelDiario: true, ordemExibicao: 4);
        var catBebidas = new Categoria("Bebidas Geladas", "🥤", perecivelDiario: false, ordemExibicao: 5);

        db.Categorias.AddRange(catAssados, catFritos, catLanches, catDoces, catBebidas);
        await db.SaveChangesAsync(ct);

        var coxinha = new Produto(
            "Coxinha de Frango c/ Catupiry",
            "Massa crocante dourada com recheio cremoso preparado no dia.",
            "🍗",
            catFritos.Id,
            precoCusto: 2.80m,
            precoVenda: 6.50m,
            quantidadeInicial: 18,
            perecivel: true);
        coxinha.AplicarDescontoPercentual(30m);

        var dogao = new Produto(
            "Dogão Assado ALAMO",
            "Pão macio assado com salsicha, molho caseiro, queijo e batata palha.",
            "🌭",
            catLanches.Id,
            precoCusto: 3.50m,
            precoVenda: 8.50m,
            quantidadeInicial: 10,
            perecivel: true);
        dogao.AplicarDescontoPercentual(25m);

        var esfiha = new Produto(
            "Esfiha de Carne Temperada",
            "Assada na hora com carne bovina selecionada, tomate e limão.",
            "🥟",
            catAssados.Id,
            precoCusto: 2.40m,
            precoVenda: 6.00m,
            quantidadeInicial: 14,
            perecivel: true);

        var pastel = new Produto(
            "Pastel de Carne Fresquinho",
            "Pastel grande sequinho com carne moída e ovo.",
            "🥙",
            catFritos.Id,
            precoCusto: 3.10m,
            precoVenda: 7.50m,
            quantidadeInicial: 9,
            perecivel: true);

        var paoDeQueijo = new Produto(
            "Pão de Queijo Mineiro",
            "Receita tradicional com queijo meia-cura, sempre quentinho.",
            "🧀",
            catAssados.Id,
            precoCusto: 1.50m,
            precoVenda: 4.50m,
            quantidadeInicial: 25,
            perecivel: true);

        var sanduicheNatural = new Produto(
            "Sanduíche Natural Integral",
            "Pão integral, pasta leve de frango desfiado, cenoura e alface fresca.",
            "🥪",
            catLanches.Id,
            precoCusto: 4.20m,
            precoVenda: 9.50m,
            quantidadeInicial: 7,
            perecivel: true);
        sanduicheNatural.AplicarDescontoValor(2.50m);

        var boloCenoura = new Produto(
            "Bolo de Cenoura c/ Brigadeiro",
            "Fatia generosa com cobertura cremosa de chocolate.",
            "🍰",
            catDoces.Id,
            precoCusto: 2.90m,
            precoVenda: 7.00m,
            quantidadeInicial: 8,
            perecivel: true);

        var sucoLaranja = new Produto(
            "Suco Natural de Laranja 300ml",
            "Espremido na hora, sem conservantes.",
            "🍊",
            catBebidas.Id,
            precoCusto: 2.50m,
            precoVenda: 6.50m,
            quantidadeInicial: 20,
            perecivel: false);

        var refrigerante = new Produto(
            "Refrigerante Lata 350ml",
            "Opções tradicional e zero açúcar bem geladas.",
            "🥤",
            catBebidas.Id,
            precoCusto: 2.60m,
            precoVenda: 5.50m,
            quantidadeInicial: 32,
            perecivel: false);

        db.Produtos.AddRange(
            coxinha, dogao, esfiha, pastel, paoDeQueijo,
            sanduicheNatural, boloCenoura, sucoLaranja, refrigerante);
        await db.SaveChangesAsync(ct);

        db.GruposWhatsApp.AddRange(
            new GrupoWhatsApp(
                "SENAC Dia",
                TurnoSenac.Manha,
                "https://chat.whatsapp.com/CantinaAlamoSenacDia",
                participantesEstimados: 60),
            new GrupoWhatsApp(
                "SENAC Tarde",
                TurnoSenac.Tarde,
                "https://chat.whatsapp.com/CantinaAlamoSenacTarde",
                participantesEstimados: 40),
            new GrupoWhatsApp(
                "SENAC Noite",
                TurnoSenac.Noite,
                "https://chat.whatsapp.com/CantinaAlamoSenacNoite",
                participantesEstimados: 52));

        db.Alunos.AddRange(
            new Aluno("Jacinta da Silva", "jacinta.silva@alunos.sc.senac.br", "47991112233", "2026101", TurnoSenac.Manha),
            new Aluno("Maurício Linhares", "mauricio.linhares@alunos.sc.senac.br", "47992223344", "2026102", TurnoSenac.Tarde),
            new Aluno("Jaqueline Arruda", "jaqueline.arruda@alunos.sc.senac.br", "47993334455", "2026103", TurnoSenac.Noite));

        // Pedido concluído de exemplo (alimenta indicadores iniciais de Receita Recuperada e Principais Clientes)
        coxinha.BaixarVendaDireta(2);
        sucoLaranja.BaixarVendaDireta(1);
        var pedidoConcluido = new Pedido(
            OrigemPedido.SiteWeb,
            "Jaqueline Arruda",
            "47993334455",
            "2026103",
            TurnoSenac.Noite,
            FormaPagamento.PixCopiaECola,
            "Retirada no intervalo das 20:30",
            new[]
            {
                new ItemPedido(coxinha, 2),
                new ItemPedido(sucoLaranja, 1)
            },
            codigoRetirada: "AL-9K2M");
        pedidoConcluido.ConcluirEntrega();

        // Pedido ativo na fila Fura-Fila para demonstração imediata
        dogao.Reservar(1);
        refrigerante.Reservar(1);
        var pedidoFila = new Pedido(
            OrigemPedido.SiteWeb,
            "Maurício Linhares",
            "47992223344",
            "2026102",
            TurnoSenac.Tarde,
            FormaPagamento.PixNaRetirada,
            "Sem batata palha, por favor",
            new[]
            {
                new ItemPedido(dogao, 1),
                new ItemPedido(refrigerante, 1)
            },
            codigoRetirada: "AL-4F8P");

        db.Pedidos.AddRange(pedidoConcluido, pedidoFila);
        await db.SaveChangesAsync(ct);
    }
}
