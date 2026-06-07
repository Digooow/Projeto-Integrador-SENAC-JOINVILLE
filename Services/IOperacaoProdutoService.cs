using System.Collections.Generic;
using Projeto_Integrador_SENAC.Models;

namespace Projeto_Integrador_SENAC.Services
{
    public interface IOperacaoProdutoService
    {
        void AlterarQuantidade(Produto? produto, int valor);

        void AplicarDescontoPercentual(Produto? produto, string? descontoTexto);

        void AplicarDescontoPercentualTodos(IEnumerable<Produto> produtos, string? descontoTexto);

        void ZerarDesconto(Produto? produto);

        void ZerarDescontoTodos(IEnumerable<Produto> produtos);
    }
}