using Cantina.Application.Abstractions;
using Cantina.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cantina.Infrastructure.Persistence;

public sealed class CantinaDbContext : DbContext, ICantinaDbContext
{
    public CantinaDbContext(DbContextOptions<CantinaDbContext> options) : base(options)
    {
    }

    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<ItemPedido> ItensPedido => Set<ItemPedido>();
    public DbSet<GrupoWhatsApp> GruposWhatsApp => Set<GrupoWhatsApp>();
    public DbSet<Aluno> Alunos => Set<Aluno>();
    public DbSet<MovimentacaoEstoque> MovimentacoesEstoque => Set<MovimentacaoEstoque>();
    public DbSet<NotificacaoWhatsAppLog> NotificacoesWhatsApp => Set<NotificacaoWhatsAppLog>();
    public DbSet<FechamentoDiario> FechamentosDiarios => Set<FechamentoDiario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Categoria>(b =>
        {
            b.ToTable("Categorias");
            b.HasKey(x => x.Id);
            b.Property(x => x.Nome).HasMaxLength(80).IsRequired();
            b.Property(x => x.IconeEmoji).HasMaxLength(16).IsRequired();
        });

        modelBuilder.Entity<Produto>(b =>
        {
            b.ToTable("Produtos");
            b.HasKey(x => x.Id);
            b.Property(x => x.AtualizadoEm).IsConcurrencyToken();
            b.Property(x => x.Nome).HasMaxLength(100).IsRequired();
            b.Property(x => x.Descricao).HasMaxLength(300);
            b.Property(x => x.IconeEmoji).HasMaxLength(16);
            b.Property(x => x.PrecoCusto).HasPrecision(10, 2);
            b.Property(x => x.PrecoVenda).HasPrecision(10, 2);
            b.Property(x => x.DescontoPercentual).HasPrecision(5, 2);
            b.Property(x => x.DescontoValor).HasPrecision(10, 2);

            b.HasOne(x => x.Categoria)
                .WithMany(c => c.Produtos)
                .HasForeignKey(x => x.CategoriaId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Pedido>(b =>
        {
            b.ToTable("Pedidos");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.CodigoRetirada).IsUnique();
            b.Property(x => x.CodigoRetirada).HasMaxLength(20).IsRequired();
            b.Property(x => x.NomeCliente).HasMaxLength(100).IsRequired();
            b.Property(x => x.TelefoneWhatsApp).HasMaxLength(25);
            b.Property(x => x.MatriculaAluno).HasMaxLength(30);
            b.Property(x => x.Observacoes).HasMaxLength(300);

            b.Property(x => x.SubtotalOriginal).HasPrecision(10, 2);
            b.Property(x => x.DescontoTotal).HasPrecision(10, 2);
            b.Property(x => x.ValorTotal).HasPrecision(10, 2);
            b.Property(x => x.ReceitaRecuperada).HasPrecision(10, 2);
            b.Property(x => x.ReceitaNormal).HasPrecision(10, 2);

            b.HasMany(x => x.Itens)
                .WithOne()
                .HasForeignKey(i => i.PedidoId)
                .OnDelete(DeleteBehavior.Cascade);

            b.Navigation(x => x.Itens)
                .HasField("_itens")
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<ItemPedido>(b =>
        {
            b.ToTable("ItensPedido");
            b.HasKey(x => x.Id);
            b.Property(x => x.NomeProduto).HasMaxLength(100).IsRequired();
            b.Property(x => x.PrecoUnitarioOriginal).HasPrecision(10, 2);
            b.Property(x => x.PrecoUnitarioFinal).HasPrecision(10, 2);
        });

        modelBuilder.Entity<GrupoWhatsApp>(b =>
        {
            b.ToTable("GruposWhatsApp");
            b.HasKey(x => x.Id);
            b.Property(x => x.Nome).HasMaxLength(80).IsRequired();
            b.Property(x => x.LinkConvite).HasMaxLength(250);
            b.Property(x => x.DestinoApiJid).HasMaxLength(100);
        });

        modelBuilder.Entity<Aluno>(b =>
        {
            b.ToTable("Alunos");
            b.HasKey(x => x.Id);
            b.Property(x => x.Nome).HasMaxLength(100).IsRequired();
            b.Property(x => x.Email).HasMaxLength(140).IsRequired();
            b.Property(x => x.TelefoneWhatsApp).HasMaxLength(25);
            b.Property(x => x.Matricula).HasMaxLength(30);
        });

        modelBuilder.Entity<MovimentacaoEstoque>(b =>
        {
            b.ToTable("MovimentacoesEstoque");
            b.HasKey(x => x.Id);
            b.Property(x => x.NomeProduto).HasMaxLength(100).IsRequired();
            b.Property(x => x.Motivo).HasMaxLength(250);
        });

        modelBuilder.Entity<NotificacaoWhatsAppLog>(b =>
        {
            b.ToTable("NotificacoesWhatsApp");
            b.HasKey(x => x.Id);
            b.Property(x => x.Destinatario).HasMaxLength(100).IsRequired();
            b.Property(x => x.LinkFallbackWaMe).HasMaxLength(2000);
            b.Property(x => x.DetalheStatus).HasMaxLength(300);
        });

        modelBuilder.Entity<FechamentoDiario>(b =>
        {
            b.ToTable("FechamentosDiarios");
            b.HasKey(x => x.Id);
            b.Property(x => x.ReceitaNormal).HasPrecision(10, 2);
            b.Property(x => x.ReceitaRecuperada).HasPrecision(10, 2);
            b.Property(x => x.DescontosConcedidos).HasPrecision(10, 2);
            b.Property(x => x.Observacoes).HasMaxLength(300);
        });
    }
}

