using System.Text.Json;
using Core.Application.DTOs.Financeiro;
using Core.Application.Services.Financeiro;
using Core.Domain.Entities.Financeiro;
using Core.Domain.Enums;
using Core.Domain.Exceptions;
using Core.Domain.Interfaces;
using Core.Domain.Interfaces.Financeiro;

namespace Core.Tests.Unit.Application;

public sealed class ReembolsoServiceTests
{
    [Fact]
    public async Task DeveExigirUsuarioAutenticado_ParaCriarReembolso()
    {
        var service = CriarService(new ReembolsoRepositoryFake(), new DespesaRepositoryFake(), null);

        var ex = await Assert.ThrowsAsync<DomainException>(() => service.CriarAsync(CriarRequestPadrao()));

        Assert.Equal("usuario_nao_autenticado", ex.Message);
    }

    [Fact]
    public async Task DeveExigirPeloMenosUmaDespesaVinculada()
    {
        var service = CriarService(new ReembolsoRepositoryFake(), new DespesaRepositoryFake(), 1);

        var ex = await Assert.ThrowsAsync<DomainException>(() => service.CriarAsync(CriarRequestPadrao(despesasVinculadas: [])));

        Assert.Equal("despesas_vinculadas_obrigatorias", ex.Message);
    }

    [Fact]
    public async Task DeveImpedirVinculoDeDespesaJaUsadaEmOutroReembolso()
    {
        var service = new ReembolsoService(
            new ReembolsoRepositoryFake { ExisteDespesaVinculada = true },
            new DespesaRepositoryFake
            {
                Despesas =
                [
                    new Despesa { Id = 1, Descricao = "Combustivel", ValorTotal = 100m }
                ]
            },
            new UsuarioAutenticadoProviderFake(1),
            new HistoricoTransacaoFinanceiraService(new HistoricoRepositoryFake()));

        var ex = await Assert.ThrowsAsync<DomainException>(() => service.CriarAsync(CriarRequestPadrao()));

        Assert.Equal("despesa_vinculada_outro_reembolso", ex.Message);
    }

    [Fact]
    public async Task DeveCriarReembolsoComValorTotalSomadoPelasDespesas()
    {
        var repository = new ReembolsoRepositoryFake();
        var service = CriarService(
            repository,
            new DespesaRepositoryFake
            {
                Despesas =
                [
                    new Despesa { Id = 1, Descricao = "Combustivel", ValorTotal = 100m },
                    new Despesa { Id = 3, Descricao = "Pedagio", ValorTotal = 74.9m }
                ]
            },
            1);

        var response = await service.CriarAsync(CriarRequestPadrao());

        Assert.Equal(174.9m, response.ValorTotal);
        Assert.Equal("AGUARDANDO", response.Status);
        Assert.Equal([1L, 3L], response.DespesasVinculadas);
        Assert.NotNull(repository.ReembolsoCriado);
        Assert.Equal(174.9m, repository.ReembolsoCriado!.ValorTotal);
    }

    [Fact]
    public async Task DeveImpedirReembolsoPagoComDataEfetivacaoMenorQueDataSolicitacao()
    {
        var service = CriarService(
            new ReembolsoRepositoryFake(),
            new DespesaRepositoryFake
            {
                Despesas =
                [
                    new Despesa { Id = 1, Descricao = "Combustivel", ValorTotal = 100m }
                ]
            },
            1);

        var ex = await Assert.ThrowsAsync<DomainException>(() => service.CriarAsync(CriarRequestPadrao(status: "PAGO", dataEfetivacao: new DateOnly(2026, 3, 17))));

        Assert.Equal("periodo_invalido", ex.Message);
    }

    [Fact]
    public async Task DevePermitirReembolsoPagoComDataEfetivacaoIgualDataSolicitacao()
    {
        var service = CriarService(
            new ReembolsoRepositoryFake(),
            new DespesaRepositoryFake
            {
                Despesas =
                [
                    new Despesa { Id = 1, Descricao = "Combustivel", ValorTotal = 100m }
                ]
            },
            1);

        var response = await service.CriarAsync(CriarRequestPadrao(status: "PAGO", dataEfetivacao: new DateOnly(2026, 3, 18)));

        Assert.Equal("PAGO", response.Status);
        Assert.Equal(new DateOnly(2026, 3, 18), response.DataEfetivacao);
    }

    [Fact]
    public async Task DeveEfetivarReembolso_QuandoDadosForemValidos()
    {
        var repository = new ReembolsoRepositoryFake
        {
            Reembolso = new Reembolso
            {
                Id = 10,
                Descricao = "Reembolso",
                Solicitante = "Joao",
                DataSolicitacao = new DateOnly(2026, 3, 18),
                ValorTotal = 150m,
                Status = StatusReembolso.Aprovado
            }
        };
        var service = CriarService(repository, new DespesaRepositoryFake(), 1);

        var response = await service.EfetivarAsync(10, new EfetivarReembolsoRequest(new DateOnly(2026, 3, 18), contaBancariaId: 1));

        Assert.Equal("PAGO", response.Status);
        Assert.Equal(new DateOnly(2026, 3, 18), response.DataEfetivacao);
    }

    [Fact]
    public async Task DeveImpedirEfetivacao_QuandoDataEfetivacaoForMenorQueDataSolicitacao()
    {
        var repository = new ReembolsoRepositoryFake
        {
            Reembolso = new Reembolso
            {
                Id = 10,
                Descricao = "Reembolso",
                Solicitante = "Joao",
                DataSolicitacao = new DateOnly(2026, 3, 18),
                ValorTotal = 150m,
                Status = StatusReembolso.Aprovado
            }
        };
        var service = CriarService(repository, new DespesaRepositoryFake(), 1);

        var ex = await Assert.ThrowsAsync<DomainException>(() => service.EfetivarAsync(10, new EfetivarReembolsoRequest(new DateOnly(2026, 3, 17), contaBancariaId: 1)));

        Assert.Equal("periodo_invalido", ex.Message);
    }

    [Fact]
    public async Task DeveEstornarReembolsoPago()
    {
        var repository = new ReembolsoRepositoryFake
        {
            Reembolso = new Reembolso
            {
                Id = 10,
                Descricao = "Reembolso",
                Solicitante = "Joao",
                DataSolicitacao = new DateOnly(2026, 3, 18),
                DataEfetivacao = new DateOnly(2026, 3, 20),
                ValorTotal = 150m,
                Status = StatusReembolso.Pago
            }
        };
        var service = CriarService(repository, new DespesaRepositoryFake(), 1);

        var response = await service.EstornarAsync(10);

        Assert.Equal("AGUARDANDO", response.Status);
        Assert.Null(response.DataEfetivacao);
    }

    private static SalvarReembolsoRequest CriarRequestPadrao(
        IReadOnlyCollection<JsonElement>? despesasVinculadas = null,
        string? status = "AGUARDANDO",
        DateOnly? dataEfetivacao = null) =>
        new(
            "Viagem comercial - semana 2",
            "Joao Silva",
            new DateOnly(2026, 3, 18),
            dataEfetivacao,
            despesasVinculadas ?? [CriarJsonNumero(1), CriarJsonNumero(3)],
            999m,
            status);

    private static JsonElement CriarJsonNumero(long valor)
    {
        using var document = JsonDocument.Parse(valor.ToString());
        return document.RootElement.Clone();
    }

    private static ReembolsoService CriarService(IReembolsoRepository repository, IDespesaRepository despesaRepository, int? usuarioId) =>
        new(repository, despesaRepository, new UsuarioAutenticadoProviderFake(usuarioId), new HistoricoTransacaoFinanceiraService(new HistoricoRepositoryFake()));

    private sealed class UsuarioAutenticadoProviderFake(int? usuarioId) : IUsuarioAutenticadoProvider
    {
        public int? ObterUsuarioId() => usuarioId;
    }

    private sealed class DespesaRepositoryFake : IDespesaRepository
    {
        public List<Despesa> Despesas { get; set; } = [];

        public Task<List<Despesa>> ListarAsync(CancellationToken cancellationToken = default) => Task.FromResult(Despesas);
        public Task<List<Despesa>> ObterPorIdsAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken = default) =>
            Task.FromResult(Despesas.Where(x => ids.Contains(x.Id)).ToList());
        public Task<Despesa?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Despesa?>(Despesas.FirstOrDefault(x => x.Id == id));
        public Task<Despesa> CriarAsync(Despesa despesa, CancellationToken cancellationToken = default) => Task.FromResult(despesa);
        public Task<Despesa> AtualizarAsync(Despesa despesa, CancellationToken cancellationToken = default) => Task.FromResult(despesa);
    }

    private sealed class ReembolsoRepositoryFake : IReembolsoRepository
    {
        public bool ExisteDespesaVinculada { get; set; }
        public Reembolso? ReembolsoCriado { get; private set; }
        public Reembolso? Reembolso { get; set; }

        public Task<List<Reembolso>> ListarAsync(string? filtroId, string? descricao, DateOnly? dataInicio, DateOnly? dataFim, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<Reembolso>());

        public Task<Reembolso?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Reembolso);

        public Task<Reembolso> CriarAsync(Reembolso reembolso, CancellationToken cancellationToken = default)
        {
            reembolso.Id = 1;
            ReembolsoCriado = reembolso;
            return Task.FromResult(reembolso);
        }

        public Task<Reembolso> AtualizarAsync(Reembolso reembolso, CancellationToken cancellationToken = default) =>
            Task.FromResult(reembolso);

        public Task ExcluirAsync(Reembolso reembolso, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<bool> ExisteDespesaVinculadaEmOutroReembolsoAsync(IReadOnlyCollection<long> despesasIds, long? reembolsoIgnoradoId, CancellationToken cancellationToken = default) =>
            Task.FromResult(ExisteDespesaVinculada);
    }

    private sealed class HistoricoRepositoryFake : IHistoricoTransacaoFinanceiraRepository
    {
        public Task<HistoricoTransacaoFinanceira> CriarAsync(HistoricoTransacaoFinanceira historico, CancellationToken cancellationToken = default) =>
            Task.FromResult(historico);

        public Task<HistoricoTransacaoFinanceira?> ObterUltimoPorTransacaoAsync(TipoTransacaoFinanceira tipoTransacao, long transacaoId, CancellationToken cancellationToken = default) =>
            Task.FromResult<HistoricoTransacaoFinanceira?>(null);
    }
}
