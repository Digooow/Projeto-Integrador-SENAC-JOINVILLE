using System.Collections.ObjectModel;
using System.Windows.Input;
using Projeto_Integrador_SENAC.Models;
using Projeto_Integrador_SENAC.Services;

namespace Projeto_Integrador_SENAC.ViewModels
{
    public class AlunoViewModel : BaseViewModel
    {
        private readonly IAlunoService _alunoService;

        public ObservableCollection<Aluno> Alunos { get; set; }
        private Aluno? _alunoSelecionado;
        public Aluno? AlunoSelecionado
        {
            get => _alunoSelecionado;
            set { _alunoSelecionado = value; OnPropertyChanged(); }
        }

        private string _nome = string.Empty;
        public string Nome
        {
            get => _nome;
            set { _nome = value; OnPropertyChanged(); }
        }

        private string _email = string.Empty;
        public string Email
        {
            get => _email;
            set { _email = value; OnPropertyChanged(); }
        }

        private string _matricula = string.Empty;
        public string Matricula
        {
            get => _matricula;
            set { _matricula = value; OnPropertyChanged(); }
        }

        public ICommand SalvarCommand { get; }
        public ICommand CarregarAlunosCommand { get; }
        public ICommand ExcluirCommand { get; }

        public AlunoViewModel(IAlunoService alunoService)
        {
            _alunoService = alunoService;
            Alunos = new ObservableCollection<Aluno>();

            SalvarCommand = new RelayCommand(_ => SalvarAluno(), _ => PodeSalvar());
            CarregarAlunosCommand = new RelayCommand(_ => CarregarAlunos());
            ExcluirCommand = new RelayCommand(_ => ExcluirAluno(), _ => AlunoSelecionado != null);

            CarregarAlunos();
        }

        private void CarregarAlunos()
        {
            var lista = _alunoService.GetAll();
            Alunos.Clear();
            foreach (var aluno in lista)
                Alunos.Add(aluno);
        }

        private void SalvarAluno()
        {
            var aluno = new Aluno
            {
                Nome = Nome,
                Email = Email,
                Matricula = Matricula
            };
            _alunoService.Add(aluno);
            LimparCampos();
            CarregarAlunos();
        }

        private void ExcluirAluno()
        {
            if (AlunoSelecionado != null)
            {
                _alunoService.Delete(AlunoSelecionado.Id);
                CarregarAlunos();
            }
        }

        private void LimparCampos()
        {
            Nome = string.Empty;
            Email = string.Empty;
            Matricula = string.Empty;
        }

        private bool PodeSalvar() => !string.IsNullOrWhiteSpace(Nome) && !string.IsNullOrWhiteSpace(Email);
    }
}