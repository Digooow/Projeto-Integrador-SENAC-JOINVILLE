using System;
using System.Collections.ObjectModel;
using System.Linq;
using Projeto_Integrador_SENAC.Models;

namespace Projeto_Integrador_SENAC.Services
{
    public class ProdutoService : IProdutoService
    {
        private readonly ObservableCollection<Produto> _produtos;

        public event Action? DadosAlterados;

        public ProdutoService()
        {
            _produtos = new ObservableCollection<Produto>(Storage.Carregar());
        }

        public ObservableCollection<Produto> ObterTodos()
        {
            return _produtos;
        }

        public void Adicionar(Produto? produto)
        {
            if (produto == null)
                return;

            if (string.IsNullOrWhiteSpace(produto.Nome))
                throw new ArgumentException("Nome obrigatório", nameof(produto.Nome));

            _produtos.Add(produto);
            SalvarENotificar();
        }

        public void Remover(Produto? produto)
        {
            if (produto == null)
                return;

            bool removido = _produtos.Remove(produto);

            if (removido)
                SalvarENotificar();
        }


        public void Atualizar()
        {
            SalvarENotificar();
        }

        private void SalvarENotificar()
        {
            Storage.Salvar(_produtos.ToList());
            DadosAlterados?.Invoke();
        }
    }
}