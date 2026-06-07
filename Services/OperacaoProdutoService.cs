using System.Collections.Generic;
using Projeto_Integrador_SENAC.Models;

namespace Projeto_Integrador_SENAC.Services
{
    public class OperacaoProdutoService : IOperacaoProdutoService
    {
        private readonly IProdutoService _produtoService;
        private readonly IEstoqueService _estoqueService;
        private readonly DescontoService _descontoService;

        public OperacaoProdutoService(
            IProdutoService produtoService,
            IEstoqueService estoqueService,
            DescontoService descontoService)
        {
            _produtoService = produtoService;
            _estoqueService = estoqueService;
            _descontoService = descontoService;
        }

        public void AlterarQuantidade(Produto? produto, int valor)
        {
            if (produto == null || valor == 0)
                return;

            _estoqueService.AlterarQuantidade(produto, valor);
            _produtoService.Atualizar();
        }

        public void AplicarDescontoPercentual(Produto? produto, string? descontoTexto)
        {
            if (produto == null)
                return;

            _descontoService.AplicarPercentualTexto(produto, descontoTexto);
            _produtoService.Atualizar();
        }

        public void AplicarDescontoPercentualTodos(IEnumerable<Produto> produtos, string? descontoTexto)
        {
            if (produtos == null)
                return;

            decimal desconto = _descontoService.ConverterTextoParaPercentual(descontoTexto);

            foreach (var produto in produtos)
            {
                _descontoService.AplicarPercentual(produto, desconto);
            }

            _produtoService.Atualizar();
        }

        public void ZerarDesconto(Produto? produto)
        {
            if (produto == null)
                return;

            _descontoService.Zerar(produto);
            _produtoService.Atualizar();
        }

        public void ZerarDescontoTodos(IEnumerable<Produto> produtos)
        {
            if (produtos == null)
                return;

            _descontoService.ZerarTodos(produtos);
            _produtoService.Atualizar();
        }
    }
}