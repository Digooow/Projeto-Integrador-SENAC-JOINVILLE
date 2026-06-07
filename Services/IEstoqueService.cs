
using Projeto_Integrador_SENAC.Models;

namespace Projeto_Integrador_SENAC.Services
{
    public interface IEstoqueService
    {
        void AlterarQuantidade(Produto? produto, int valor);
    }
}
