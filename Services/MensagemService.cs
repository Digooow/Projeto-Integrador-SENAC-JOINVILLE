
using System.Net;
using System.Diagnostics;
using Projeto_Integrador_SENAC.Models;
using System.Linq;
using System.Collections.Generic;

namespace Projeto_Integrador_SENAC.Services
{
    public class MensagemService
    {
        public void EnviarPromocao(List<Produto> produtos)
        {
            var lista = produtos
                .Where(p => p.Desconto > 0 && p.Quantidade > 0)
                .ToList();

            if (!lista.Any()) return;

            string mensagem = "Promoções da Cantina:\n\n";

            foreach (var p in lista)
            {
                mensagem += $"{p.Nome}\n";
                mensagem += $"De: R${p.Preco}\n";
                mensagem += $"Por: R${p.PrecoPromocional}\n\n";
            }

            string url = "https://wa.me/?text=" + WebUtility.UrlEncode(mensagem);

            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
    }
}
