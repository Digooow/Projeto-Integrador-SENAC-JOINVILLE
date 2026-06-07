
using System;
using Projeto_Integrador_SENAC.Models;

namespace Projeto_Integrador_SENAC.Services
{
    public class EstoqueService : IEstoqueService
    {
        public void AlterarQuantidade(Produto? produto, int valor)
        {
            if (produto == null || valor == 0)
                return;

            produto.Quantidade = Math.Max(0, produto.Quantidade + valor);
        }
    }
}
