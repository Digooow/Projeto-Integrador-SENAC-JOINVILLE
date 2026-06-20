using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Data;

namespace Projeto_Integrador_SENAC.Converters
{
    /// <summary>
    /// Conversor para campos de texto que representam valores monetários.
    /// Comportamento similar a uma maquininha de cartão: o usuário digita números inteiros
    /// que são interpretados como centavos.
    /// Exemplo: entrada "1" -> saída "0,01" | "100" -> "1,00" | "1234" -> "12,34"
    /// </summary>
    public class TextMoneyConverter : IValueConverter
    {
        private const int CentavosPorReal = 100;
        private static readonly CultureInfo CulturaBrasil = new CultureInfo("pt-BR");
        private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(100);

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue && !string.IsNullOrEmpty(stringValue))
            {
                string apenasNumeros = Regex.Replace(
                    stringValue,
                    "[^0-9]",
                    "",
                    RegexOptions.None,
                    RegexTimeout
                );

                if (string.IsNullOrEmpty(apenasNumeros))
                    return "0,00";

                if (decimal.TryParse(apenasNumeros, out decimal valorEmCentavos))
                {
                    return (valorEmCentavos / CentavosPorReal).ToString("F2", CulturaBrasil);
                }
            }

            return "0,00";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string texto = value?.ToString() ?? "0";

            texto = texto.Replace("R$", "").Replace(" ", "").Trim();

            if (string.IsNullOrWhiteSpace(texto))
                return "0";

            string apenasNumeros = Regex.Replace(
                texto,
                "[^0-9]",
                "",
                RegexOptions.None,
                RegexTimeout
            );

            if (!string.IsNullOrEmpty(apenasNumeros) && decimal.TryParse(apenasNumeros, out decimal valorEmCentavos))
            {
                return (valorEmCentavos / CentavosPorReal).ToString("F2", CulturaBrasil);
            }

            return "0";
        }
    }
}