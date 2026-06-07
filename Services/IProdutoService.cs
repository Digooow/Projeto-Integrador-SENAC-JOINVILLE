
using System;
using System.Collections.ObjectModel;
using Projeto_Integrador_SENAC.Models;

namespace Projeto_Integrador_SENAC.Services
{
    public interface IProdutoService
    {
        event Action? DadosAlterados;

        ObservableCollection<Produto> ObterTodos();

        void Adicionar(Produto? produto);

        void Remover(Produto? produto);

        void Atualizar();
    }
}
