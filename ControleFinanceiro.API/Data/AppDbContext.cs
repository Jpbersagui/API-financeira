using ControleFinanceiro.API.Models;
using ControleFinanceiro.API.Enums;
using Microsoft.EntityFrameworkCore;

namespace ControleFinanceiro.API.Data
{
    /// <summary>
    /// Contexto do banco de dados para o sistema de Controle Financeiro.
    /// </summary>
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Transacao> Transacoes { get; set; }
        public DbSet<Salario> Salarios { get; set; }
        public DbSet<Conta> Contas { get; set; }
        public DbSet<RevisaoTransacao> RevisoesTransacoes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Conta>(entity =>
            {
                entity.ToTable("Contas", t => t.HasCheckConstraint("CK_Contas_Abertura", "([DataAbertura] IS NULL AND [ValorAbertura] IS NULL) OR ([DataAbertura] IS NOT NULL AND [ValorAbertura] IS NOT NULL)"));
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Nome).HasMaxLength(100).IsRequired();
                entity.Property(c => c.Tipo).HasConversion<string>().HasMaxLength(30).IsRequired();
                entity.Property(c => c.ValorAbertura).HasPrecision(18, 2);
                entity.Property(c => c.Versao).IsRowVersion();
            });

            modelBuilder.Entity<RevisaoTransacao>(entity =>
            {
                entity.ToTable("RevisoesTransacoes");
                entity.Property(r => r.Motivo).HasMaxLength(500).IsRequired();
                entity.HasOne(r => r.Transacao).WithMany().HasForeignKey(r => r.TransacaoId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Transacao>(entity =>
            {
                entity.ToTable("Transacoes", t => t.HasCheckConstraint("CK_Transacoes_Confirmacao", "[Estado] <> 'Confirmada' OR ([ContaId] IS NOT NULL AND [DataEfetivacao] IS NOT NULL AND [ConfirmadaEm] IS NOT NULL AND [Valor] > 0 AND [MetodoPagamento] <> 'CartaoCredito' AND ([OrigemRegistro] IS NULL OR [OrigemRegistro] <> 'CreditoLegado') AND [ClassificacaoPendente] IS NULL AND [NumeroParcelas] = 1 AND [ParcelaAtual] IS NULL AND [TransacaoOrigemId] IS NULL)"));
                entity.Property(t => t.Estado).HasConversion<string>().HasMaxLength(30).HasDefaultValue(EstadoTransacao.NaoReconciliada);
                entity.Property(t => t.OrigemRegistro).HasConversion<string>().HasMaxLength(30);
                entity.Property(t => t.ClassificacaoPendente).HasConversion<string>().HasMaxLength(30);
                entity.Property(t => t.MotivoDesconsideracao).HasMaxLength(500);
                entity.Property(t => t.Versao).IsRowVersion();
                entity.HasIndex(t => new { t.ContaId, t.Estado, t.DataEfetivacao, t.Id });

                entity.HasKey(t => t.Id);
                entity.HasOne(t => t.Conta).WithMany().HasForeignKey(t => t.ContaId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Precisão do campo decimal
                entity.Property(t => t.Valor)
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();

                entity.Property(t => t.Titulo)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(t => t.Categoria)
                    .HasMaxLength(100)
                    .IsRequired();

                // Armazenar enums como string para melhor legibilidade no banco
                entity.Property(t => t.Tipo)
                    .HasConversion<string>()
                    .HasMaxLength(20)
                    .IsRequired();

                entity.Property(t => t.MetodoPagamento)
                    .HasConversion<string>()
                    .HasMaxLength(30)
                    .IsRequired();

                // Self-referencing relationship para parcelamento
                entity.HasOne(t => t.TransacaoOrigem)
                    .WithMany(t => t.Parcelas)
                    .HasForeignKey(t => t.TransacaoOrigemId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired(false);

                // Índices para consultas frequentes
                entity.HasIndex(t => t.Data);
                entity.HasIndex(t => t.MetodoPagamento);
                entity.HasIndex(t => t.Tipo);
                entity.HasIndex(t => t.Categoria);
            });

            modelBuilder.Entity<Salario>(entity =>
            {
                entity.ToTable("Salarios");

                entity.HasKey(s => s.Id);

                entity.Property(s => s.Valor)
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();

                entity.HasIndex(s => s.Ativo);
            });
        }
    }
}
