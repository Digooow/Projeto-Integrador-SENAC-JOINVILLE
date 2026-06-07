using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using Projeto_Integrador_SENAC.Models;

namespace Projeto_Integrador_SENAC.Services
{
    public static class Storage
    {
        private static readonly string Caminho =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "produtos.json");

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        public static void Salvar(List<Produto> produtos)
        {
            var json = JsonSerializer.Serialize(produtos, JsonOptions);

            File.WriteAllText(Caminho, json, Encoding.UTF8);
        }

        public static List<Produto> Carregar()
        {
            if (!File.Exists(Caminho))
                return new List<Produto>();

            try
            {
                var json = File.ReadAllText(Caminho, Encoding.UTF8);

                if (string.IsNullOrWhiteSpace(json))
                    return new List<Produto>();

                return JsonSerializer.Deserialize<List<Produto>>(json, JsonOptions)
                       ?? new List<Produto>();
            }
            catch
            {
                return new List<Produto>();
            }
        }
    }
}