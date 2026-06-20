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

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // O valor de entrada é a string digitada pelo usuário (ex: "150").
            if (value is string stringValue && !string.IsNullOrEmpty(stringValue))
            {
                string apenasNumeros = Regex.Replace(stringValue, "[^0-9]", "");

                if (string.IsNullOrEmpty(apenasNumeros))
                    return "0,00";

                if (decimal.TryParse(apenasNumeros, out decimal valorEmCentavos))
                {
                    // Converte centavos para reais e formata com duas casas decimais.
                    return (valorEmCentavos / CentavosPorReal).ToString("F2", CulturaBrasil);
                }
            }

            return "0,00";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string texto = value?.ToString() ?? "0";

            // Remove símbolos e espaços comuns.
            texto = texto.Replace("R$", "").Replace(" ", "").Trim();

            if (string.IsNullOrWhiteSpace(texto))
                return "0";

            // Extrai apenas os dígitos.
            string apenasNumeros = Regex.Replace(texto, "[^0-9]", "");

            // Tenta converter para decimal e, se bem-sucedido, aplica a regra da maquininha.
            if (!string.IsNullOrEmpty(apenasNumeros) && decimal.TryParse(apenasNumeros, out decimal valorEmCentavos))
            {
                // Converte centavos para reais (divide por 100) e formata com duas casas.
                return (valorEmCentavos / CentavosPorReal).ToString("F2", CulturaBrasil);
            }

            return "0";
        }
    }
}