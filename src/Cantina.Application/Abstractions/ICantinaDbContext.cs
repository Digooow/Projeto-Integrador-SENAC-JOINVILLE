using Cantina.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cantina.Application.Abstractions;

public interface ICantinaDbContext
{
    DbSet<Categoria> Categorias { get; }
    DbSet<Produto> Produtos { get; }
    DbSet<Pedido> Pedidos { get; }
    DbSet<ItemPedido> ItensPedido { get; }
    DbSet<GrupoWhatsApp> GruposWhatsApp { get; }
    DbSet<Aluno> Alunos { get; }
    DbSet<MovimentacaoEstoque> MovimentacoesEstoque { get; }
    DbSet<NotificacaoWhatsAppLog> NotificacoesWhatsApp { get; }
    DbSet<FechamentoDiario> FechamentosDiarios { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

