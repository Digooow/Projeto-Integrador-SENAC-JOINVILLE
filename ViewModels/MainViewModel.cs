using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using Projeto_Integrador_SENAC.Models;
using Projeto_Integrador_SENAC.Services;
using Projeto_Integrador_SENAC.Views;

namespace Projeto_Integrador_SENAC.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly IProdutoService _produtoService;
        private readonly IOperacaoProdutoService _operacaoProdutoService;
        private readonly DescontoService _descontoService;
        private readonly MensagemService _mensagemService;

        private Produto? _produtoSelecionado;
        private string _descontoTexto = "0";
        private string _descontoValorTexto = "0";
        private int _quantidadeAdicionar;

        public ObservableCollection<Produto> Produtos { get; }

        public Produto? ProdutoSelecionado
        {
            get => _produtoSelecionado;
            set
            {
                _produtoSelecionado = value;
                OnPropertyChanged(nameof(ProdutoSelecionado));

                if (_produtoSelecionado == null)
                {
                    DescontoTexto = "0";
                    DescontoValorTexto = "0";
                }
                else
                {
                    DescontoTexto = _produtoSelecionado.Desconto.ToString("0");

                    if (_produtoSelecionado.DescontoValor > 0)
                    {
                        DescontoValorTexto = (_produtoSelecionado.DescontoValor * 100m).ToString("0");
                    }
                    else
                    {
                        DescontoValorTexto = "0";
                    }
                }
            }
        }


        public string DescontoTexto
        {
            get => _descontoTexto;
            set
            {
                _descontoTexto = _descontoService.NormalizarTextoPercentual(value);
                OnPropertyChanged(nameof(DescontoTexto));
            }
        }

        public string DescontoValorTexto
        {
            get => _descontoValorTexto;
            set
            {
                _descontoValorTexto = value ?? "0";
                OnPropertyChanged(nameof(DescontoValorTexto));
            }
        }

        public int QuantidadeAdicionar
        {
            get => _quantidadeAdicionar;
            set
            {
                _quantidadeAdicionar = value;
                OnPropertyChanged(nameof(QuantidadeAdicionar));
            }
        }

        public ICommand MaisCommand { get; }
        public ICommand MenosCommand { get; }
        public ICommand Mais5Command { get; }
        public ICommand Mais10Command { get; }
        public ICommand AdicionarQuantidadeCommand { get; }
        public ICommand AplicarDescontoCommand { get; }
        public ICommand AplicarDescontoTodosCommand { get; }
        public ICommand ZerarDescontoCommand { get; }
        public ICommand ZerarDescontoTodosCommand { get; }
        public ICommand WhatsAppCommand { get; }
        public ICommand AbrirCadastroCommand { get; }

        public MainViewModel(IProdutoService service)
        {
            _produtoService = service;

            _descontoService = new DescontoService();
            _mensagemService = new MensagemService();

            _operacaoProdutoService = new OperacaoProdutoService(
                _produtoService,
                new EstoqueService(),
                _descontoService
            );

            Produtos = _produtoService.ObterTodos();

            MaisCommand = new RelayCommand(() => AlterarQuantidade(1));
            MenosCommand = new RelayCommand(() => AlterarQuantidade(-1));
            Mais5Command = new RelayCommand(() => AlterarQuantidade(5));
            Mais10Command = new RelayCommand(() => AlterarQuantidade(10));

            AdicionarQuantidadeCommand = new RelayCommand(AdicionarQuantidade);
            AplicarDescontoCommand = new RelayCommand(AplicarDesconto);
            AplicarDescontoTodosCommand = new RelayCommand(AplicarDescontoTodos);
            ZerarDescontoCommand = new RelayCommand(ZerarDesconto);
            ZerarDescontoTodosCommand = new RelayCommand(ZerarDescontoTodos);
            WhatsAppCommand = new RelayCommand(EnviarPromocoesWhatsApp);
            AbrirCadastroCommand = new RelayCommand(AbrirCadastro);

            _produtoService.DadosAlterados += AtualizarTelaComDispatcher;
        }

        private void AlterarQuantidade(int valor)
        {
            _operacaoProdutoService.AlterarQuantidade(ProdutoSelecionado, valor);
        }

        private void AdicionarQuantidade()
        {
            if (ProdutoSelecionado == null)
                return;

            _operacaoProdutoService.AlterarQuantidade(ProdutoSelecionado, QuantidadeAdicionar);
            QuantidadeAdicionar = 0;
        }

        private void AplicarDesconto()
        {
            if (ProdutoSelecionado == null)
                return;

            decimal descontoValor = _descontoService.ConverterTextoParaValor(DescontoValorTexto);

            if (descontoValor > 0)
            {
                _descontoService.AplicarValorTexto(ProdutoSelecionado, DescontoValorTexto);
                _produtoService.Atualizar();
            }
            else
            {
                _operacaoProdutoService.AplicarDescontoPercentual(ProdutoSelecionado, DescontoTexto);
            }
        }

        private void AplicarDescontoTodos()
        {
            decimal descontoValor = _descontoService.ConverterTextoParaValor(DescontoValorTexto);

            if (descontoValor > 0)
            {
                foreach (var produto in Produtos)
                {
                    _descontoService.AplicarValor(produto, descontoValor);
                }
                _produtoService.Atualizar();
            }
            else
            {
                _operacaoProdutoService.AplicarDescontoPercentualTodos(Produtos, DescontoTexto);
            }
        }

        private void ZerarDesconto()
        {
            if (ProdutoSelecionado == null)
                return;

            _operacaoProdutoService.ZerarDesconto(ProdutoSelecionado);

            DescontoTexto = "0";
            DescontoValorTexto = "0";
        }

        private void ZerarDescontoTodos()
        {
            _operacaoProdutoService.ZerarDescontoTodos(Produtos);

            DescontoTexto = "0";
            DescontoValorTexto = "0";
        }

        private void EnviarPromocoesWhatsApp()
        {
            _mensagemService.EnviarPromocao(Produtos.ToList());
        }

        private void AbrirCadastro()
        {
            var janela = new CadastroWindow(_produtoService);
            janela.ShowDialog();

            AtualizarTela();
        }

        private void AtualizarTela()
        {
            CollectionViewSource.GetDefaultView(Produtos)?.Refresh();
        }

        private void AtualizarTelaComDispatcher()
        {
            App.Current.Dispatcher.Invoke(AtualizarTela);
        }
    }
}
