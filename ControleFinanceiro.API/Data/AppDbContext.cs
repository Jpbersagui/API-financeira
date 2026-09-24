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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Conta>(entity =>
            {
                entity.ToTable("Contas");
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Nome).HasMaxLength(100).IsRequired();
                entity.Property(c => c.Tipo).HasConversion<string>().HasMaxLength(30).IsRequired();
            });

            modelBuilder.Entity<Transacao>(entity =>
            {
                entity.ToTable("Transacoes");

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
