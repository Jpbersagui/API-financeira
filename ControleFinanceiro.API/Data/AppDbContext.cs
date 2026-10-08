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
        public DbSet<Previsao> Previsoes { get; set; }
        public DbSet<RevisaoPrevisao> RevisoesPrevisoes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Previsao>(e =>
            {
                e.ToTable("Previsoes", t =>
                {
                    t.HasCheckConstraint("CK_Previsoes_Valores", "[ValorPrevistoOriginal] > 0 AND ([ValorFinal] IS NULL OR [ValorFinal] >= 0)");
                    t.HasCheckConstraint("CK_Previsoes_Competencia", "([AnoCompetencia] IS NULL AND [MesCompetencia] IS NULL) OR ([AnoCompetencia] IS NOT NULL AND [MesCompetencia] IS NOT NULL AND [AnoCompetencia] BETWEEN 1 AND 9999 AND [MesCompetencia] BETWEEN 1 AND 12)");
                    t.HasCheckConstraint("CK_Previsoes_Estado", "([Estado] = 'Ativa' AND [EncerradaEm] IS NULL AND [CanceladaEm] IS NULL AND [MotivoEncerramento] IS NULL AND [MotivoCancelamento] IS NULL) OR ([Estado] = 'Encerrada' AND [EncerradaEm] IS NOT NULL AND [MotivoEncerramento] IS NOT NULL AND [CanceladaEm] IS NULL AND [MotivoCancelamento] IS NULL) OR ([Estado] = 'Cancelada' AND [CanceladaEm] IS NOT NULL AND [MotivoCancelamento] IS NOT NULL AND [EncerradaEm] IS NULL AND [MotivoEncerramento] IS NULL)");
                });
                e.Property(p => p.Descricao).HasMaxLength(200).IsRequired();
                e.Property(p => p.Categoria).HasMaxLength(100).IsRequired();
                e.Property(p => p.Observacoes).HasMaxLength(2000);
                e.Property(p => p.Tipo).HasConversion<string>().HasMaxLength(20);
                e.Property(p => p.Estado).HasConversion<string>().HasMaxLength(20);
                e.Property(p => p.ValorPrevistoOriginal).HasPrecision(18, 2);
                e.Property(p => p.ValorFinal).HasPrecision(18, 2);
                e.Property(p => p.MotivoEncerramento).HasMaxLength(500);
                e.Property(p => p.MotivoCancelamento).HasMaxLength(500);
                e.Property(p => p.Versao).IsRowVersion();
                e.HasOne(p => p.Conta).WithMany().HasForeignKey(p => p.ContaId).OnDelete(DeleteBehavior.Restrict);
                e.HasIndex(p => new { p.DataPrevista, p.Estado });
            });
            modelBuilder.Entity<RevisaoPrevisao>(e =>
            {
                e.Property(r => r.Acao).HasMaxLength(50).IsRequired();
                e.Property(r => r.Motivo).HasMaxLength(500).IsRequired();
                e.HasOne(r => r.Previsao).WithMany().HasForeignKey(r => r.PrevisaoId).OnDelete(DeleteBehavior.Restrict);
            });

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
                entity.HasOne(t => t.Previsao).WithMany(p => p.Transacoes).HasForeignKey(t => t.PrevisaoId).OnDelete(DeleteBehavior.Restrict);
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
