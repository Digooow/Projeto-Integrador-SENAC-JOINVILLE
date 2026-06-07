
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Data;

namespace Projeto_Integrador_SENAC.Converters
{
    public class MoneyConverter : IValueConverter
    {
        private static readonly CultureInfo CulturaBrasil = new CultureInfo("pt-BR");

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal decimalValue)
            {
                return decimalValue.ToString("F2", CulturaBrasil);
            }

            return "0,00";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string texto = value?.ToString() ?? "0";

            texto = Regex.Replace(texto, "[^0-9]", "");

            if (string.IsNullOrWhiteSpace(texto))
                return 0m;

            if (decimal.TryParse(texto, out decimal valor))
            {
                return valor / 100m;
            }

            return 0m;
        }
    }
}
