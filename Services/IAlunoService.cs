using System.Collections.Generic;
using Projeto_Integrador_SENAC.Models;

namespace Projeto_Integrador_SENAC.Services
{
    public interface IAlunoService
    {
        IEnumerable<Aluno> GetAll();
        Aluno? GetById(int id);
        void Add(Aluno aluno);
        void Update(Aluno aluno);
        void Delete(int id);
    }
}