using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using System.ComponentModel.DataAnnotations.Schema;

namespace Projeto_Integrador_SENAC.Models
{
    [Table("Produtos")]  // nome da tabela no banco
    public class Produto : INotifyPropertyChanged
    {
        private int id;
        private string nome = "";
        private int quantidade;
        private decimal preco;
        private decimal desconto;
        private decimal descontoValor;

        public event PropertyChangedEventHandler? PropertyChanged;

        // Chave primária (obrigatória para EF)
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id
        {
            get => id;
            set
            {
                if (id == value) return;
                id = value;
                Notificar();
            }
        }

        [Required]
        [MaxLength(100)]
        public string Nome
        {
            get => nome;
            set
            {
                if (nome == value) return;
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
                if (quantidade == value) return;
                quantidade = value;
                Notificar();
                NotificarDependenciasDeCalculo();
            }
        }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Preco
        {
            get => preco;
            set
            {
                value = Math.Max(0, value);
                if (preco == value) return;
                preco = value;
                if (descontoValor > preco) descontoValor = preco;
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
                if (desconto == value) return;
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
                if (descontoValor == value) return;
                descontoValor = value;
                desconto = 0;
                Notificar();
                Notificar(nameof(Desconto));
                NotificarDependenciasDeCalculo();
            }
        }

        [NotMapped]  // Não cria coluna no banco
        public decimal PrecoPromocional => CalcularPrecoPromocional();

        [NotMapped]  // Não cria coluna no banco
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