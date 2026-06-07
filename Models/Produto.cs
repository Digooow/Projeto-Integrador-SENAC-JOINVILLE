using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Projeto_Integrador_SENAC.Models
{
    public class Produto : INotifyPropertyChanged
    {
        private string nome = "";
        private int quantidade;
        private decimal preco;
        private decimal desconto;
        private decimal descontoValor;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Nome
        {
            get => nome;
            set
            {
                if (nome == value)
                    return;

                nome = value;
                Notificar();
            }
        }

        public int Quantidade
        {
            get => quantidade;
            set
            {
                value = Math.Max(0, value);

                if (quantidade == value)
                    return;

                quantidade = value;
                Notificar();
                NotificarDependenciasDeCalculo();
            }
        }

        public decimal Preco
        {
            get => preco;
            set
            {
                value = Math.Max(0, value);

                if (preco == value)
                    return;

                preco = value;

                if (descontoValor > preco)
                    descontoValor = preco;

                Notificar();
                Notificar(nameof(DescontoValor));
                NotificarDependenciasDeCalculo();
            }
        }

        public decimal Desconto
        {
            get => desconto;
            set
            {
                value = Math.Min(100, Math.Max(0, value));

                if (desconto == value)
                    return;

                desconto = value;
                descontoValor = 0;

                Notificar();
                Notificar(nameof(DescontoValor));
                NotificarDependenciasDeCalculo();
            }
        }

        public decimal DescontoValor
        {
            get => descontoValor;
            set
            {
                value = Math.Max(0, value);
                value = Math.Min(Preco, value);

                if (descontoValor == value)
                    return;

                descontoValor = value;
                desconto = 0;

                Notificar();
                Notificar(nameof(Desconto));
                NotificarDependenciasDeCalculo();
            }
        }

        public decimal PrecoPromocional => CalcularPrecoPromocional();

        public decimal Total => Quantidade * PrecoPromocional;

        private decimal CalcularPrecoPromocional()
        {
            decimal valor = Preco;

            if (Desconto > 0)
                valor -= Preco * Desconto / 100;
            else if (DescontoValor > 0)
                valor -= DescontoValor;

            return Math.Max(0, valor);
        }

        private void NotificarDependenciasDeCalculo()
        {
            Notificar(nameof(PrecoPromocional));
            Notificar(nameof(Total));
        }

        private void Notificar([CallerMemberName] string? nome = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nome));
        }
    }
}