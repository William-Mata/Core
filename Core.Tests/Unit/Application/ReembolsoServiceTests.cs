using System.Text.Json;
using Core.Application.DTOs;
using Core.Application.Services.Financeiro;
using Core.Domain.Entities.Financeiro;
using Core.Domain.Exceptions;
using Core.Domain.Interfaces;
using Core.Domain.Interfaces.Financeiro;

namespace Core.Tests.Unit.Application;

public sealed class ReembolsoServiceTests
{
    [Fact]
    public async Task DeveExigirUsuarioAutenticado_ParaCriarReembolso()
    {
        var service = new ReembolsoService(new ReembolsoRepositoryFake(), new DespesaRepositoryFake(), new UsuarioAutenticadoProviderFake(null));

        var ex = await Assert.ThrowsAsync<DomainException>(() => service.CriarAsync(CriarRequestPadrao()));

        Assert.Equal("usuario_nao_autenticado", ex.Message);
    }

    [Fact]
    public async Task DeveExigirPeloMenosUmaDespesaVinculada()
    {
        var service = new ReembolsoService(new ReembolsoRepositoryFake(), new DespesaRepositoryFake(), new UsuarioAutenticadoProviderFake(1));

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
            new UsuarioAutenticadoProviderFake(1));

        var ex = await Assert.ThrowsAsync<DomainException>(() => service.CriarAsync(CriarRequestPadrao()));

        Assert.Equal("despesa_vinculada_outro_reembolso", ex.Message);
    }

    [Fact]
    public async Task DeveCriarReembolsoComValorTotalSomadoPelasDespesas()
    {
        var repository = new ReembolsoRepositoryFake();
        var service = new ReembolsoService(
            repository,
            new DespesaRepositoryFake
            {
                Despesas =
                [
                    new Despesa { Id = 1, Descricao = "Combustivel", ValorTotal = 100m },
                    new Despesa { Id = 3, Descricao = "Pedagio", ValorTotal = 74.9m }
                ]
            },
            new UsuarioAutenticadoProviderFake(1));

        var response = await service.CriarAsync(CriarRequestPadrao());

        Assert.Equal(174.9m, response.ValorTotal);
        Assert.Equal("AGUARDANDO", response.Status);
        Assert.Equal([1L, 3L], response.DespesasVinculadas);
        Assert.NotNull(repository.ReembolsoCriado);
        Assert.Equal(174.9m, repository.ReembolsoCriado!.ValorTotal);
    }

    private static SalvarReembolsoRequest CriarRequestPadrao(IReadOnlyCollection<JsonElement>? despesasVinculadas = null) =>
        new(
            "Viagem comercial - semana 2",
            "Joao Silva",
            new DateOnly(2026, 3, 18),
            despesasVinculadas ?? [CriarJsonNumero(1), CriarJsonNumero(3)],
            999m,
            "AGUARDANDO");

    private static JsonElement CriarJsonNumero(long valor)
    {
        using var document = JsonDocument.Parse(valor.ToString());
        return document.RootElement.Clone();
    }

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

        public Task<List<Reembolso>> ListarAsync(string? filtroId, string? descricao, DateOnly? dataInicio, DateOnly? dataFim, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<Reembolso>());

        public Task<Reembolso?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Reembolso?>(null);

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
}
