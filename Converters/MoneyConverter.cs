using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Data;

namespace Projeto_Integrador_SENAC.Converters
{
    /// <summary>
    /// Conversor monetário projetado para simular o comportamento de uma maquininha de cartão.
    /// O usuário digita os centavos como números inteiros e o sistema converte para reais.
    /// Exemplo: "1" -> R$ 0,01 | "100" -> R$ 1,00 | "1234" -> R$ 12,34
    /// </summary>
    public class MoneyConverter : IValueConverter
    {
        // Constante que deixa EXPLÍCITA a regra de negócio: 1 real = 100 centavos.
        private const int CentavosPorReal = 100;

        private static readonly CultureInfo CulturaBrasil = new CultureInfo("pt-BR");

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Formata o valor decimal (em reais) para exibição no padrão brasileiro.
            if (value is decimal decimalValue)
            {
                return decimalValue.ToString("F2", CulturaBrasil);
            }

            // Se o binding vier nulo, retorna vazio ou "0,00" conforme sua regra.
            // (Aqui você decide se prefere manter "0,00" ou Binding.DoNothing)
            return "0,00";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string textoDigitado = value?.ToString() ?? "0";

            // Remove qualquer caractere que não seja número (permite digitar sem vírgula/ponto).
            string apenasNumeros = Regex.Replace(textoDigitado, "[^0-9]", "");

            if (string.IsNullOrWhiteSpace(apenasNumeros))
                return 0m;

            if (decimal.TryParse(apenasNumeros, out decimal valorEmCentavos))
            {
                // REGRA DE NEGÓCIO (Maquininha):
                // O número digitado representa CENTAVOS, portanto dividimos por 100.
                return valorEmCentavos / CentavosPorReal;
            }

            return 0m;
        }
    }
}