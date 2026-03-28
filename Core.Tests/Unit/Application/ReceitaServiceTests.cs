using Core.Application.DTOs;
using Core.Application.Services.Financeiro;
using Core.Domain.Entities;
using Core.Domain.Enums;
using Core.Domain.Exceptions;
using Core.Domain.Interfaces;

namespace Core.Tests.Unit.Application;

public sealed class ReceitaServiceTests
{
    [Fact]
    public async Task DeveExigirUsuarioAutenticado_ParaCriarReceita()
    {
        var service = new ReceitaService(new ReceitaRepoFake(), new ContaRepoFake(), new AreaRepoFake(), new UsuarioAutenticadoProviderFake(null));

        var request = CriarRequestPadrao();
        var ex = await Assert.ThrowsAsync<DomainException>(() => service.CriarAsync(request));

        Assert.Equal("usuario_nao_autenticado", ex.Message);
    }

    [Fact]
    public async Task DeveExigirContaBancaria_QuandoTipoRecebimentoForPix()
    {
        var service = new ReceitaService(new ReceitaRepoFake(), new ContaRepoFake(), new AreaRepoFake(), new UsuarioAutenticadoProviderFake(99));

        var request = CriarRequestPadrao(tipoRecebimento: "pix", contaBancaria: null);
        var ex = await Assert.ThrowsAsync<DomainException>(() => service.CriarAsync(request));

        Assert.Equal("conta_bancaria_obrigatoria", ex.Message);
    }

    [Fact]
    public async Task DeveValidarContaBancariaInformada()
    {
        var service = new ReceitaService(new ReceitaRepoFake(), new ContaRepoFake(), new AreaRepoFake(), new UsuarioAutenticadoProviderFake(99));

        var request = CriarRequestPadrao(tipoRecebimento: "dinheiro", contaBancaria: "Conta Inexistente");
        var ex = await Assert.ThrowsAsync<DomainException>(() => service.CriarAsync(request));

        Assert.Equal("conta_bancaria_invalida", ex.Message);
    }

    [Fact]
    public async Task DeveValidarRelacaoEntreAreaESubArea()
    {
        var areaRepository = new AreaRepoFake
        {
            SubAreas = [new SubArea { Id = 2, AreaId = 99, Nome = "SubArea" }]
        };
        var service = new ReceitaService(new ReceitaRepoFake(), new ContaRepoFake(), areaRepository, new UsuarioAutenticadoProviderFake(99));

        var request = CriarRequestPadrao(areasRateio: [new ReceitaAreaRateioRequest(1, 2, 100m)]);
        var ex = await Assert.ThrowsAsync<DomainException>(() => service.CriarAsync(request));

        Assert.Equal("relacao_area_subarea_invalida", ex.Message);
    }

    [Fact]
    public async Task DeveRetornarErro_QuandoReceitaNaoForEncontradaAoEfetivar()
    {
        var service = new ReceitaService(new ReceitaRepoFake(), new ContaRepoFake(), new AreaRepoFake(), new UsuarioAutenticadoProviderFake(99));

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => service.EfetivarAsync(10, new EfetivarReceitaRequest(new DateOnly(2026, 3, 5), "dinheiro", null, 100m, 0m, 0m, 0m, 0m, null)));

        Assert.Equal("receita_nao_encontrada", ex.Message);
    }

    private static CriarReceitaRequest CriarRequestPadrao(
        string tipoRecebimento = "dinheiro",
        string? contaBancaria = null,
        IReadOnlyCollection<ReceitaAreaRateioRequest>? areasRateio = null) =>
        new(
            "Freelance",
            null,
            new DateOnly(2026, 3, 1),
            new DateOnly(2026, 3, 2),
            "freelance",
            tipoRecebimento,
            Recorrencia.Unica,
            1000m,
            0m,
            0m,
            0m,
            0m,
            [],
            new Dictionary<string, decimal>(),
            areasRateio ?? [],
            contaBancaria,
            null);

    private sealed class UsuarioAutenticadoProviderFake(int? usuarioId) : IUsuarioAutenticadoProvider
    {
        public int? ObterUsuarioId() => usuarioId;
    }

    private sealed class ReceitaRepoFake : IReceitaRepository
    {
        public Task<List<Receita>> ListarAsync(CancellationToken cancellationToken = default) => Task.FromResult(new List<Receita>());
        public Task<Receita?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default) => Task.FromResult<Receita?>(null);
        public Task<Receita> CriarAsync(Receita receita, CancellationToken cancellationToken = default) => Task.FromResult(receita);
        public Task<Receita> AtualizarAsync(Receita receita, CancellationToken cancellationToken = default) => Task.FromResult(receita);
    }

    private sealed class ContaRepoFake : IContaBancariaRepository
    {
        public List<ContaBancaria> Contas { get; set; } = [];

        public Task<List<ContaBancaria>> ListarAsync(CancellationToken cancellationToken = default) => Task.FromResult(Contas);
        public Task<ContaBancaria?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default) => Task.FromResult<ContaBancaria?>(null);
        public Task<ContaBancaria> CriarAsync(ContaBancaria conta, CancellationToken cancellationToken = default) => Task.FromResult(conta);
        public Task<ContaBancaria> AtualizarAsync(ContaBancaria conta, CancellationToken cancellationToken = default) => Task.FromResult(conta);
    }

    private sealed class AreaRepoFake : IAreaRepository
    {
        public List<SubArea> SubAreas { get; set; } = [];

        public Task<List<SubArea>> ObterSubAreasPorIdsAsync(IReadOnlyCollection<long> subAreasIds, CancellationToken cancellationToken = default) =>
            Task.FromResult(SubAreas.Where(x => subAreasIds.Contains(x.Id)).ToList());
    }
}
