using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Projeto_Integrador_SENAC.Models;

namespace Projeto_Integrador_SENAC.Services
{
    public class DescontoService
    {
        public decimal ConverterTextoParaPercentual(string? texto)
        {
            texto ??= "0";

            var apenasNumeros = new string(texto.Where(char.IsDigit).ToArray());

            if (string.IsNullOrWhiteSpace(apenasNumeros))
                return 0;

            if (!decimal.TryParse(apenasNumeros, out decimal percentual))
                return 0;

            return Math.Min(100, Math.Max(0, percentual));
        }

        public string NormalizarTextoPercentual(string? texto)
        {
            decimal percentual = ConverterTextoParaPercentual(texto);
            return percentual.ToString("0");
        }

        public void AplicarPercentualTexto(Produto? produto, string? texto)
        {
            decimal percentual = ConverterTextoParaPercentual(texto);
            AplicarPercentual(produto, percentual);
        }

        public decimal ConverterTextoParaValor(string? texto)
        {
            texto ??= "0";

            texto = texto.Replace("R$", "").Trim();

            if (string.IsNullOrWhiteSpace(texto))
                return 0;

            if (!decimal.TryParse(
                    texto,
                    NumberStyles.Number,
                    new CultureInfo("pt-BR"),
                    out decimal valor))
            {
                return 0;
            }

            return Math.Max(0, valor);
        }

        public string NormalizarTextoValor(string? texto)
        {
            decimal valor = ConverterTextoParaValor(texto);
            return valor.ToString("0.00", new CultureInfo("pt-BR"));
        }

        public void AplicarValorTexto(Produto? produto, string? texto)
        {
            decimal valor = ConverterTextoParaValor(texto);
            AplicarValor(produto, valor);
        }

        public void AplicarPercentual(Produto? produto, decimal percentual)
        {
            if (produto == null)
                return;

            percentual = Math.Min(100, Math.Max(0, percentual));

            produto.Desconto = percentual;
            produto.DescontoValor = 0;
        }

        public void AplicarValor(Produto? produto, decimal valor)
        {
            if (produto == null)
                return;

            valor = Math.Max(0, valor);
            valor = Math.Min(produto.Preco, valor);

            produto.DescontoValor = valor;
            produto.Desconto = 0;
        }

        public void AplicarMesmoDesconto(Produto? origem, IEnumerable<Produto> produtos)
        {
            if (origem == null || produtos == null)
                return;

            foreach (var produto in produtos)
            {
                if (origem.Desconto > 0)
                {
                    AplicarPercentual(produto, origem.Desconto);
                }
                else if (origem.DescontoValor > 0)
                {
                    AplicarValor(produto, origem.DescontoValor);
                }
                else
                {
                    Zerar(produto);
                }
            }
        }

        public void Zerar(Produto? produto)
        {
            if (produto == null)
                return;

            produto.Desconto = 0;
            produto.DescontoValor = 0;
        }

        public void ZerarTodos(IEnumerable<Produto> produtos)
        {
            if (produtos == null)
                return;

            foreach (var produto in produtos)
            {
                Zerar(produto);
            }
        }
    }
}