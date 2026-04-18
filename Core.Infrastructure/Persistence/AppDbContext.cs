using System;
using System.Linq;
using Core.Domain.Common;
using Core.Domain.Entities;
using Core.Domain.Entities.Administracao;
using Core.Domain.Entities.Financeiro;
using Core.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Core.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Modulo> Modulos => Set<Modulo>();
    public DbSet<Tela> Telas => Set<Tela>();
    public DbSet<Funcionalidade> Funcionalidades => Set<Funcionalidade>();
    public DbSet<UsuarioModulo> UsuariosModulos => Set<UsuarioModulo>();
    public DbSet<UsuarioTela> UsuariosTelas => Set<UsuarioTela>();
    public DbSet<UsuarioFuncionalidade> UsuariosFuncionalidades => Set<UsuarioFuncionalidade>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<TentativaLoginInvalida> TentativasLoginInvalidas => Set<TentativaLoginInvalida>();
    public DbSet<ContaBancaria> ContasBancarias => Set<ContaBancaria>();
    public DbSet<ContaBancariaExtrato> ContasBancariasExtrato => Set<ContaBancariaExtrato>();
    public DbSet<ContaBancariaLog> ContasBancariasLogs => Set<ContaBancariaLog>();
    public DbSet<Cartao> Cartoes => Set<Cartao>();
    public DbSet<CartaoLog> CartoesLogs => Set<CartaoLog>();
    public DbSet<FaturaCartao> FaturasCartoes => Set<FaturaCartao>();
    public DbSet<Despesa> Despesas => Set<Despesa>();
    public DbSet<DespesaAmigoRateio> DespesasAmigosRateio => Set<DespesaAmigoRateio>();
    public DbSet<DespesaAreaRateio> DespesasAreasRateio => Set<DespesaAreaRateio>();
    public DbSet<DespesaLog> DespesasLogs => Set<DespesaLog>();
    public DbSet<HistoricoTransacaoFinanceira> HistoricosTransacoesFinanceiras => Set<HistoricoTransacaoFinanceira>();
    public DbSet<ConviteAmizade> ConvitesAmizade => Set<ConviteAmizade>();
    public DbSet<Amizade> Amizades => Set<Amizade>();
    public DbSet<Reembolso> Reembolsos => Set<Reembolso>();
    public DbSet<ReembolsoDespesa> ReembolsosDespesas => Set<ReembolsoDespesa>();
    public DbSet<Receita> Receitas => Set<Receita>();
    public DbSet<ReceitaAmigoRateio> ReceitasAmigosRateio => Set<ReceitaAmigoRateio>();
    public DbSet<ReceitaAreaRateio> ReceitasAreasRateio => Set<ReceitaAreaRateio>();
    public DbSet<ReceitaLog> ReceitasLogs => Set<ReceitaLog>();
    public DbSet<Area> Areas => Set<Area>();
    public DbSet<SubArea> SubAreas => Set<SubArea>();
    public DbSet<Documento> Documentos => Set<Documento>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        NormalizarDataHoraParaBrasil();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        NormalizarDataHoraParaBrasil();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var seedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<Usuario>().ToTable("Usuario");
        modelBuilder.Entity<Usuario>().HasKey(x => x.Id);
        modelBuilder.Entity<Usuario>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<Usuario>().Property(x => x.DataNascimento).HasColumnType("date");
        modelBuilder.Entity<Usuario>().HasData(new
        {
            Id = 1,
            DataHoraCadastro = seedDate,
            UsuarioCadastroId = 1,
            DataNascimento = (DateOnly?)null,
            Nome = "Usuario",
            Email = "admin@core.com",
            SenhaHash = "PBKDF2$100000$DqVvtU2jQnWQTuqbL+H8aQ==$zvCjIqD8J/r93o4azALW2k8vIjoWtM5ikW7PKfY2PA8=",
            Ativo = true,
            PrimeiroAcesso = true,
            PerfilId = 1
        });
        modelBuilder.Entity<Usuario>()
            .HasMany(x => x.Modulos)
            .WithOne()
            .HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Usuario>()
            .HasMany(x => x.Telas)
            .WithOne()
            .HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Usuario>()
            .HasMany(x => x.Funcionalidades)
            .WithOne()
            .HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Modulo>().ToTable("Modulo");
        modelBuilder.Entity<Modulo>().HasKey(x => x.Id);
        modelBuilder.Entity<Modulo>().HasIndex(x => x.Nome).IsUnique();
        modelBuilder.Entity<Modulo>().HasData(
            new { Id = 2, DataHoraCadastro = seedDate, UsuarioCadastroId = 1, Nome = "financeiro", Status = true },
            new { Id = 3, DataHoraCadastro = seedDate, UsuarioCadastroId = 1, Nome = "administracao", Status = true });

        modelBuilder.Entity<Tela>().ToTable("Tela");
        modelBuilder.Entity<Tela>().HasKey(x => x.Id);
        modelBuilder.Entity<Tela>().HasIndex(x => new { x.ModuloId, x.Nome }).IsUnique();
        modelBuilder.Entity<Tela>().HasOne(x => x.Modulo).WithMany().HasForeignKey(x => x.ModuloId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Tela>().HasData(
            new { Id = 2, DataHoraCadastro = seedDate, UsuarioCadastroId = 1, ModuloId = 2, Nome = "Despesas", Status = true },
            new { Id = 3, DataHoraCadastro = seedDate, UsuarioCadastroId = 1, ModuloId = 2, Nome = "Receitas", Status = true },
            new { Id = 4, DataHoraCadastro = seedDate, UsuarioCadastroId = 1, ModuloId = 3, Nome = "Usuarios", Status = true });

        modelBuilder.Entity<Funcionalidade>().ToTable("Funcionalidade");
        modelBuilder.Entity<Funcionalidade>().HasKey(x => x.Id);
        modelBuilder.Entity<Funcionalidade>().HasIndex(x => new { x.TelaId, x.Nome }).IsUnique();
        modelBuilder.Entity<Funcionalidade>().HasOne(x => x.Tela).WithMany().HasForeignKey(x => x.TelaId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Funcionalidade>().HasData(
            new { Id = 2, DataHoraCadastro = seedDate, UsuarioCadastroId = 1, TelaId = 2, Nome = "editar", Status = true },
            new { Id = 3, DataHoraCadastro = seedDate, UsuarioCadastroId = 1, TelaId = 2, Nome = "visualizar", Status = true },
            new { Id = 4, DataHoraCadastro = seedDate, UsuarioCadastroId = 1, TelaId = 4, Nome = "criar", Status = true });

        modelBuilder.Entity<UsuarioModulo>().ToTable("UsuarioModulo");
        modelBuilder.Entity<UsuarioModulo>().HasKey(x => x.Id);
        modelBuilder.Entity<UsuarioModulo>().HasIndex(x => new { x.UsuarioId, x.ModuloId }).IsUnique();
        modelBuilder.Entity<UsuarioModulo>().HasOne(x => x.Modulo).WithMany().HasForeignKey(x => x.ModuloId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<UsuarioModulo>().HasData(
            new { Id = 1, DataHoraCadastro = seedDate, UsuarioCadastroId = 1, UsuarioId = 1, ModuloId = 2, Status = true },
            new { Id = 2, DataHoraCadastro = seedDate, UsuarioCadastroId = 1, UsuarioId = 1, ModuloId = 3, Status = true });

        modelBuilder.Entity<UsuarioTela>().ToTable("UsuarioTela");
        modelBuilder.Entity<UsuarioTela>().HasKey(x => x.Id);
        modelBuilder.Entity<UsuarioTela>().HasIndex(x => new { x.UsuarioId, x.TelaId }).IsUnique();
        modelBuilder.Entity<UsuarioTela>().HasOne(x => x.Tela).WithMany().HasForeignKey(x => x.TelaId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<UsuarioTela>().HasData(
            new { Id = 1, DataHoraCadastro = seedDate, UsuarioCadastroId = 1, UsuarioId = 1, TelaId = 2, Status = true },
            new { Id = 2, DataHoraCadastro = seedDate, UsuarioCadastroId = 1, UsuarioId = 1, TelaId = 3, Status = true },
            new { Id = 3, DataHoraCadastro = seedDate, UsuarioCadastroId = 1, UsuarioId = 1, TelaId = 4, Status = true });

        modelBuilder.Entity<UsuarioFuncionalidade>().ToTable("UsuarioFuncionalidade");
        modelBuilder.Entity<UsuarioFuncionalidade>().HasKey(x => x.Id);
        modelBuilder.Entity<UsuarioFuncionalidade>().HasIndex(x => new { x.UsuarioId, x.FuncionalidadeId }).IsUnique();
        modelBuilder.Entity<UsuarioFuncionalidade>().HasOne(x => x.Funcionalidade).WithMany().HasForeignKey(x => x.FuncionalidadeId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<UsuarioFuncionalidade>().HasData(
            new { Id = 1, DataHoraCadastro = seedDate, UsuarioCadastroId = 1, UsuarioId = 1, FuncionalidadeId = 2, Status = true },
            new { Id = 2, DataHoraCadastro = seedDate, UsuarioCadastroId = 1, UsuarioId = 1, FuncionalidadeId = 3, Status = true },
            new { Id = 3, DataHoraCadastro = seedDate, UsuarioCadastroId = 1, UsuarioId = 1, FuncionalidadeId = 4, Status = true });

        modelBuilder.Entity<TentativaLoginInvalida>().ToTable("TentativaLoginInvalida");
        modelBuilder.Entity<TentativaLoginInvalida>().HasKey(x => x.Id);
        modelBuilder.Entity<TentativaLoginInvalida>().HasIndex(x => x.Email).IsUnique();

        modelBuilder.Entity<RefreshToken>().ToTable("RefreshToken");
        modelBuilder.Entity<RefreshToken>().HasKey(x => x.Id);
        modelBuilder.Entity<RefreshToken>().HasIndex(x => x.Token).IsUnique();
        modelBuilder.Entity<RefreshToken>().Property(x => x.Token).HasMaxLength(128);
        modelBuilder.Entity<RefreshToken>().HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ContaBancaria>().ToTable("ContaBancaria");
        modelBuilder.Entity<ContaBancaria>().HasKey(x => x.Id);
        modelBuilder.Entity<ContaBancariaExtrato>().ToTable("ContaBancariaExtrato");
        modelBuilder.Entity<ContaBancariaLog>().ToTable("ContaBancariaLog");
        modelBuilder.Entity<ContaBancaria>().Property(x => x.Status).HasConversion<string>();
        modelBuilder.Entity<ContaBancariaLog>().Property(x => x.Acao).HasConversion<string>();
        modelBuilder.Entity<ContaBancaria>().HasMany(x => x.Extrato).WithOne().HasForeignKey(x => x.ContaBancariaId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ContaBancaria>().HasMany(x => x.Logs).WithOne().HasForeignKey(x => x.ContaBancariaId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Cartao>().ToTable("Cartao");
        modelBuilder.Entity<Cartao>().HasKey(x => x.Id);
        modelBuilder.Entity<CartaoLog>().ToTable("CartaoLog");
        modelBuilder.Entity<Cartao>().Property(x => x.Status).HasConversion<string>();
        modelBuilder.Entity<Cartao>().Property(x => x.Tipo).HasConversion<string>();
        modelBuilder.Entity<CartaoLog>().Property(x => x.Acao).HasConversion<string>();
        modelBuilder.Entity<Cartao>().HasMany(x => x.Logs).WithOne().HasForeignKey(x => x.CartaoId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FaturaCartao>().ToTable("FaturaCartao");
        modelBuilder.Entity<FaturaCartao>().HasKey(x => x.Id);
        modelBuilder.Entity<FaturaCartao>().Property(x => x.Status).HasConversion<string>();
        modelBuilder.Entity<FaturaCartao>().HasIndex(x => new { x.UsuarioCadastroId, x.CartaoId, x.Competencia }).IsUnique();
        modelBuilder.Entity<FaturaCartao>().HasOne<Cartao>().WithMany().HasForeignKey(x => x.CartaoId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Despesa>().ToTable("Despesa");
        modelBuilder.Entity<Despesa>().HasKey(x => x.Id);
        modelBuilder.Entity<DespesaAmigoRateio>().ToTable("DespesaAmigoRateio");
        modelBuilder.Entity<DespesaAreaRateio>().ToTable("DespesaAreaRateio");
        modelBuilder.Entity<DespesaLog>().ToTable("DespesaLog");
        modelBuilder.Entity<Despesa>().Property(x => x.Status).HasConversion<string>();
        modelBuilder.Entity<Despesa>().Property(x => x.Recorrencia).HasConversion<string>();
        modelBuilder.Entity<Despesa>().Property(x => x.RecorrenciaFixa).HasDefaultValue(false);
        modelBuilder.Entity<Despesa>().Property(x => x.ValorTotalRateioAmigos).HasPrecision(18, 2);
        modelBuilder.Entity<Despesa>().Property(x => x.TipoRateioAmigos).HasConversion<string>().HasMaxLength(20);
        modelBuilder.Entity<Despesa>().Property(x => x.TipoDespesa)
            .HasConversion(
                value => EnumToCamelCase(value),
                value => ParseEnum<TipoDespesa>(value))
            .HasMaxLength(20);
        modelBuilder.Entity<Despesa>().Property(x => x.TipoPagamento)
            .HasConversion(
                value => EnumToCamelCase(value),
                value => ParseEnum<TipoPagamento>(value))
            .HasMaxLength(20);
        modelBuilder.Entity<DespesaLog>().Property(x => x.Acao).HasConversion<string>();
        modelBuilder.Entity<Despesa>().HasOne<Despesa>().WithMany().HasForeignKey(x => x.DespesaOrigemId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Despesa>().HasOne<Despesa>().WithMany().HasForeignKey(x => x.DespesaRecorrenciaOrigemId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Despesa>().HasIndex(x => x.DespesaRecorrenciaOrigemId);
        modelBuilder.Entity<Despesa>().HasOne<ContaBancaria>().WithMany().HasForeignKey(x => x.ContaBancariaId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Despesa>().HasOne<Receita>().WithMany().HasForeignKey(x => x.ReceitaTransferenciaId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Despesa>().HasOne<Cartao>().WithMany().HasForeignKey(x => x.CartaoId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Despesa>().HasOne<FaturaCartao>().WithMany().HasForeignKey(x => x.FaturaCartaoId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Despesa>().HasMany(x => x.AmigosRateio).WithOne().HasForeignKey(x => x.DespesaId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Despesa>().HasMany(x => x.AreasRateio).WithOne().HasForeignKey(x => x.DespesaId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Despesa>().HasMany(x => x.Documentos).WithOne().HasForeignKey(x => x.DespesaId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Despesa>().HasMany(x => x.Logs).WithOne().HasForeignKey(x => x.DespesaId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<DespesaAreaRateio>().HasOne(x => x.Area).WithMany().HasForeignKey(x => x.AreaId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DespesaAreaRateio>().HasOne(x => x.SubArea).WithMany().HasForeignKey(x => x.SubAreaId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HistoricoTransacaoFinanceira>().ToTable("HistoricoTransacaoFinanceira");
        modelBuilder.Entity<HistoricoTransacaoFinanceira>().HasKey(x => x.Id);
        modelBuilder.Entity<HistoricoTransacaoFinanceira>().Property(x => x.TipoTransacao).HasConversion<string>();
        modelBuilder.Entity<HistoricoTransacaoFinanceira>().Property(x => x.TipoOperacao).HasConversion<string>();
        modelBuilder.Entity<HistoricoTransacaoFinanceira>().Property(x => x.TipoConta).HasConversion<string>();
        modelBuilder.Entity<HistoricoTransacaoFinanceira>().Property(x => x.TipoPagamento).HasConversion<string>();
        modelBuilder.Entity<HistoricoTransacaoFinanceira>().Property(x => x.TipoRecebimento).HasConversion<string>();
        modelBuilder.Entity<HistoricoTransacaoFinanceira>().Property(x => x.Observacao).HasMaxLength(500);
        modelBuilder.Entity<HistoricoTransacaoFinanceira>().Property(x => x.OcultarDoHistorico).HasDefaultValue(false);
        modelBuilder.Entity<HistoricoTransacaoFinanceira>().HasIndex(x => new { x.TipoTransacao, x.TransacaoId, x.DataHoraCadastro });

        modelBuilder.Entity<ConviteAmizade>().ToTable("ConviteAmizade");
        modelBuilder.Entity<ConviteAmizade>().HasKey(x => x.Id);
        modelBuilder.Entity<ConviteAmizade>().Property(x => x.Status).HasConversion<string>();
        modelBuilder.Entity<ConviteAmizade>().HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioCadastroId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ConviteAmizade>().HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioOrigemId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ConviteAmizade>().HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioDestinoId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ConviteAmizade>().HasIndex(x => new { x.UsuarioOrigemId, x.UsuarioDestinoId, x.Status });
        modelBuilder.Entity<ConviteAmizade>().HasIndex(x => new { x.UsuarioDestinoId, x.Status, x.DataHoraCadastro });

        modelBuilder.Entity<Amizade>().ToTable("Amizade");
        modelBuilder.Entity<Amizade>().HasKey(x => x.Id);
        modelBuilder.Entity<Amizade>().HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioCadastroId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Amizade>().HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioAId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Amizade>().HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioBId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Amizade>().HasIndex(x => new { x.UsuarioAId, x.UsuarioBId }).IsUnique();
        modelBuilder.Entity<Amizade>().HasIndex(x => x.UsuarioAId);
        modelBuilder.Entity<Amizade>().HasIndex(x => x.UsuarioBId);

        modelBuilder.Entity<Reembolso>().ToTable("Reembolso");
        modelBuilder.Entity<Reembolso>().HasKey(x => x.Id);
        modelBuilder.Entity<Reembolso>().Property(x => x.Status).HasConversion<string>();
        modelBuilder.Entity<Reembolso>().HasOne<Cartao>().WithMany().HasForeignKey(x => x.CartaoId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Reembolso>().HasOne<FaturaCartao>().WithMany().HasForeignKey(x => x.FaturaCartaoId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Reembolso>().HasMany(x => x.Documentos).WithOne().HasForeignKey(x => x.ReembolsoId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Reembolso>().HasMany(x => x.Despesas).WithOne().HasForeignKey(x => x.ReembolsoId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ReembolsoDespesa>().ToTable("ReembolsoDespesa");
        modelBuilder.Entity<ReembolsoDespesa>().HasKey(x => x.Id);
        modelBuilder.Entity<ReembolsoDespesa>().HasIndex(x => x.DespesaId).IsUnique();
        modelBuilder.Entity<ReembolsoDespesa>().HasOne(x => x.Despesa).WithMany().HasForeignKey(x => x.DespesaId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Receita>().ToTable("Receita");
        modelBuilder.Entity<Receita>().HasKey(x => x.Id);
        modelBuilder.Entity<ReceitaAmigoRateio>().ToTable("ReceitaAmigoRateio");
        modelBuilder.Entity<ReceitaAreaRateio>().ToTable("ReceitaAreaRateio");
        modelBuilder.Entity<ReceitaLog>().ToTable("ReceitaLog");
        modelBuilder.Entity<Receita>().Property(x => x.Status).HasConversion<string>();
        modelBuilder.Entity<Receita>().Property(x => x.Recorrencia).HasConversion<string>();
        modelBuilder.Entity<Receita>().Property(x => x.RecorrenciaFixa).HasDefaultValue(false);
        modelBuilder.Entity<Receita>().Property(x => x.ValorTotalRateioAmigos).HasPrecision(18, 2);
        modelBuilder.Entity<Receita>().Property(x => x.TipoRateioAmigos).HasConversion<string>().HasMaxLength(20);
        modelBuilder.Entity<Receita>().Property(x => x.TipoReceita)
            .HasConversion(
                value => EnumToCamelCase(value),
                value => ParseEnum<TipoReceita>(value))
            .HasMaxLength(20);
        modelBuilder.Entity<Receita>().Property(x => x.TipoRecebimento)
            .HasConversion(
                value => EnumToCamelCase(value),
                value => ParseEnum<TipoRecebimento>(value))
            .HasMaxLength(20);
        modelBuilder.Entity<ReceitaLog>().Property(x => x.Acao).HasConversion<string>();
        modelBuilder.Entity<Receita>().HasOne<Receita>().WithMany().HasForeignKey(x => x.ReceitaOrigemId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Receita>().HasOne<ContaBancaria>().WithMany().HasForeignKey(x => x.ContaBancariaId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Receita>().HasOne<Despesa>().WithMany().HasForeignKey(x => x.DespesaTransferenciaId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Receita>().HasOne<Cartao>().WithMany().HasForeignKey(x => x.CartaoId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Receita>().HasOne<FaturaCartao>().WithMany().HasForeignKey(x => x.FaturaCartaoId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Receita>().HasMany(x => x.AmigosRateio).WithOne().HasForeignKey(x => x.ReceitaId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Receita>().HasMany(x => x.AreasRateio).WithOne().HasForeignKey(x => x.ReceitaId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Receita>().HasMany(x => x.Documentos).WithOne().HasForeignKey(x => x.ReceitaId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Receita>().HasMany(x => x.Logs).WithOne().HasForeignKey(x => x.ReceitaId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ReceitaAreaRateio>().HasOne(x => x.Area).WithMany().HasForeignKey(x => x.AreaId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ReceitaAreaRateio>().HasOne(x => x.SubArea).WithMany().HasForeignKey(x => x.SubAreaId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Area>().ToTable("Area");
        modelBuilder.Entity<Area>().HasKey(x => x.Id);
        modelBuilder.Entity<Area>().Property(x => x.Tipo).HasConversion<string>();

        modelBuilder.Entity<SubArea>().ToTable("SubArea");
        modelBuilder.Entity<SubArea>().HasKey(x => x.Id);
        modelBuilder.Entity<SubArea>().HasOne(x => x.Area).WithMany(x => x.SubAreas).HasForeignKey(x => x.AreaId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Documento>().ToTable("Documento");
        modelBuilder.Entity<Documento>().HasKey(x => x.Id);
        modelBuilder.Entity<Documento>().HasIndex(x => x.DespesaId);
        modelBuilder.Entity<Documento>().HasIndex(x => x.ReceitaId);
        modelBuilder.Entity<Documento>().HasIndex(x => x.ReembolsoId);
    }

    private static string ToCamelCase(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        return char.ToLowerInvariant(value[0]) + value.Substring(1);
    }

    private static string EnumToCamelCase<TEnum>(TEnum value) where TEnum : struct, Enum =>
        ToCamelCase(value.ToString());

    private static TEnum ParseEnum<TEnum>(string value) where TEnum : struct, Enum =>
        Enum.Parse<TEnum>(value, true);

    private static TEnum? ParseNullableEnum<TEnum>(string? value) where TEnum : struct, Enum =>
        string.IsNullOrWhiteSpace(value) ? null : Enum.Parse<TEnum>(value, true);

    private void NormalizarDataHoraParaBrasil()
    {
        foreach (var entry in ChangeTracker.Entries().Where(x => x.State is EntityState.Added or EntityState.Modified))
        {
            foreach (var property in entry.Properties.Where(DeveNormalizarDataHora))
            {
                if (property.CurrentValue is DateTime dataHora)
                    property.CurrentValue = DataHoraBrasil.Converter(dataHora);
            }
        }
    }

    private static bool DeveNormalizarDataHora(PropertyEntry property)
    {
        var tipo = property.Metadata.ClrType;
        if (tipo != typeof(DateTime) && tipo != typeof(DateTime?))
            return false;

        return !property.Metadata.Name.EndsWith("Utc", StringComparison.OrdinalIgnoreCase);
    }
}

