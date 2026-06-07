using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Data;

namespace Projeto_Integrador_SENAC.Converters
{
    public class TextMoneyConverter : IValueConverter
    {
        private static readonly CultureInfo CulturaBrasil = new CultureInfo("pt-BR");

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue && !string.IsNullOrEmpty(stringValue))
            {
                string apenasNumeros = Regex.Replace(stringValue, "[^0-9]", "");

                if (string.IsNullOrEmpty(apenasNumeros))
                    return "0,00";

                if (decimal.TryParse(apenasNumeros, out decimal valor))
                {
                    return (valor / 100m).ToString("F2", CulturaBrasil);
                }
            }

            return "0,00";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string texto = value?.ToString() ?? "0";

            texto = texto.Replace("R$", "").Trim();

            if (string.IsNullOrWhiteSpace(texto))
                return "0";

            texto = texto.Replace(" ", "");

            string apenasNumeros = Regex.Replace(texto, "[^0-9]", "");

            if (!string.IsNullOrEmpty(apenasNumeros) && apenasNumeros.Length > 0)
            {
                if (decimal.TryParse(apenasNumeros, out decimal valor))
                {
                    valor = valor / 100m;
                    return valor.ToString("F2", CulturaBrasil);
                }
            }

            return "0";
        }
    }
}
