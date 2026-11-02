using Cantina.Domain.Exceptions;

namespace Cantina.Domain.Entities;

public class Categoria
{
    public int Id { get; set; }
    public string Nome { get; private set; } = string.Empty;
    public string IconeEmoji { get; private set; } = "🍽️";
    public bool PerecivelDiario { get; private set; }
    public int OrdemExibicao { get; private set; }

    public ICollection<Produto> Produtos { get; private set; } = new List<Produto>();

    protected Categoria()
    {
    }

    public Categoria(string nome, string iconeEmoji, bool perecivelDiario, int ordemExibicao = 0)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new DomainException("O nome da categoria é obrigatório.");

        Nome = nome.Trim();
        IconeEmoji = string.IsNullOrWhiteSpace(iconeEmoji) ? "🍽️" : iconeEmoji.Trim();
        PerecivelDiario = perecivelDiario;
        OrdemExibicao = ordemExibicao;
    }
}

