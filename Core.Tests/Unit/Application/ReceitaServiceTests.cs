using Core.Application.DTOs.Financeiro;
using Core.Application.Contracts.Financeiro;
using Core.Application.Services.Financeiro;
using Core.Domain.Entities;
using Core.Domain.Entities.Financeiro;
using Core.Domain.Enums;
using Core.Domain.Exceptions;
using Core.Domain.Interfaces;
using Core.Domain.Interfaces.Financeiro;

namespace Core.Tests.Unit.Application;

public sealed class ReceitaServiceTests
{
    [Fact]
    public async Task DeveExigirUsuarioAutenticado_ParaCriarReceita()
    {
        var service = CriarService(new ReceitaRepoFake(), new ContaRepoFake(), new AreaRepoFake(), null);

        var request = CriarRequestPadrao();
        var ex = await Assert.ThrowsAsync<DomainException>(() => service.CriarAsync(request));

        Assert.Equal("usuario_nao_autenticado", ex.Message);
    }

    [Fact]
    public async Task DeveExigirContaBancaria_QuandoTipoRecebimentoForPix()
    {
        var service = CriarService(new ReceitaRepoFake(), new ContaRepoFake(), new AreaRepoFake(), 99);

        var request = CriarRequestPadrao(tipoRecebimento: "pix", contaBancaria: null);
        var ex = await Assert.ThrowsAsync<DomainException>(() => service.CriarAsync(request));

        Assert.Equal("conta_bancaria_obrigatoria", ex.Message);
    }

    [Fact]
    public async Task DeveValidarContaBancariaInformada()
    {
        var service = CriarService(new ReceitaRepoFake(), new ContaRepoFake(), new AreaRepoFake(), 99);

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
        var service = CriarService(new ReceitaRepoFake(), new ContaRepoFake(), areaRepository, 99);

        var request = CriarRequestPadrao(areasRateio: [new ReceitaAreaRateioRequest(1, 2, 100m)]);
        var ex = await Assert.ThrowsAsync<DomainException>(() => service.CriarAsync(request));

        Assert.Equal("relacao_area_subarea_invalida", ex.Message);
    }

    [Fact]
    public async Task DeveRejeitarRateioDeAmigosQuandoSomaForDiferenteDoValorTotal()
    {
        var service = CriarService(new ReceitaRepoFake(), new ContaRepoFake(), new AreaRepoFake(), 99);

        var request = CriarRequestPadrao(amigos: [new AmigoRateioRequest("Amigo 1", 600m), new AmigoRateioRequest("Amigo 2", 300m)]);
        var ex = await Assert.ThrowsAsync<DomainException>(() => service.CriarAsync(request));

        Assert.Equal("rateio_amigos_invalido", ex.Message);
    }

    [Fact]
    public async Task DeveRejeitarRateioDeAreaQuandoSomaForDiferenteDoValorTotal()
    {
        var areaRepository = CriarAreaRepoValida(TipoAreaFinanceira.Receita);
        var service = CriarService(new ReceitaRepoFake(), new ContaRepoFake(), areaRepository, 99);

        var request = CriarRequestPadrao(areasRateio: [new ReceitaAreaRateioRequest(1, 2, 600m)]);
        var ex = await Assert.ThrowsAsync<DomainException>(() => service.CriarAsync(request));

        Assert.Equal("rateio_area_invalido", ex.Message);
    }

    [Fact]
    public async Task DevePermitirCriacao_QuandoRateiosSomamValorTotal()
    {
        var areaRepository = CriarAreaRepoValida(TipoAreaFinanceira.Receita);
        var service = CriarService(new ReceitaRepoFake(), new ContaRepoFake(), areaRepository, 99);

        var result = await service.CriarAsync(
            CriarRequestPadrao(
                amigos: [new AmigoRateioRequest("Amigo 1", 400m), new AmigoRateioRequest("Amigo 2", 600m)],
                areasRateio: [new ReceitaAreaRateioRequest(1, 2, 1000m)]));

        Assert.Equal("Freelance", result.Descricao);
        Assert.Equal(1000m, result.ValorTotal);
        Assert.Equal(2, result.Amigos.Count);
        Assert.Single(result.AreasRateio);
    }

    [Fact]
    public async Task DeveRetornarErro_QuandoReceitaNaoForEncontradaAoEfetivar()
    {
        var service = CriarService(new ReceitaRepoFake(), new ContaRepoFake(), new AreaRepoFake(), 99);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => service.EfetivarAsync(10, new EfetivarReceitaRequest(new DateOnly(2026, 3, 5), "dinheiro", null, 100m, 0m, 0m, 0m, 0m, null)));

        Assert.Equal("receita_nao_encontrada", ex.Message);
    }

    [Fact]
    public async Task DeveImpedirEfetivacao_QuandoDataEfetivacaoForMenorQueDataLancamento()
    {
        var service = CriarService(
            new ReceitaRepoFake
            {
                Receita = new Receita
                {
                    Id = 1,
                    Descricao = "Receita",
                    DataLancamento = new DateOnly(2026, 3, 10),
                    DataVencimento = new DateOnly(2026, 3, 15),
                    TipoReceita = "freelance",
                    TipoRecebimento = "dinheiro",
                    Recorrencia = Recorrencia.Unica,
                    ValorTotal = 100m,
                    ValorLiquido = 100m,
                    Status = StatusReceita.Pendente
                }
            },
            new ContaRepoFake(),
            new AreaRepoFake(),
            99);

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            service.EfetivarAsync(1, new EfetivarReceitaRequest(new DateOnly(2026, 3, 9), "dinheiro", null, 100m, 0m, 0m, 0m, 0m, null)));

        Assert.Equal("periodo_invalido", ex.Message);
    }

    [Fact]
    public async Task DevePermitirEfetivacao_QuandoDataEfetivacaoForIgualDataLancamento()
    {
        var service = CriarService(
            new ReceitaRepoFake
            {
                Receita = new Receita
                {
                    Id = 1,
                    Descricao = "Receita",
                    DataLancamento = new DateOnly(2026, 3, 10),
                    DataVencimento = new DateOnly(2026, 3, 15),
                    TipoReceita = "freelance",
                    TipoRecebimento = "dinheiro",
                    Recorrencia = Recorrencia.Unica,
                    ValorTotal = 100m,
                    ValorLiquido = 100m,
                    Status = StatusReceita.Pendente
                }
            },
            new ContaRepoFake(),
            new AreaRepoFake(),
            99);

        var result = await service.EfetivarAsync(1, new EfetivarReceitaRequest(new DateOnly(2026, 3, 10), "dinheiro", null, 100m, 0m, 0m, 0m, 0m, null));

        Assert.Equal("efetivada", result.Status);
    }

    [Fact]
    public async Task DevePublicarMensagemDeRecorrencia_AoCriarReceitaRecorrente()
    {
        var repository = new ReceitaRepoFake();
        var publisher = new RecorrenciaPublisherFake();
        var service = CriarService(repository, new ContaRepoFake(), new AreaRepoFake(), publisher, 99);

        await service.CriarAsync(CriarRequestPadrao(recorrencia: Recorrencia.Semanal, quantidadeRecorrencia: 2));

        Assert.Single(repository.ReceitasCriadas);
        Assert.NotNull(publisher.ReceitaMessage);
        Assert.Equal(2, publisher.ReceitaMessage!.QuantidadeRecorrencia);
    }

    [Fact]
    public async Task DevePublicarMensagemComAlvo100_AoCriarReceitaComRecorrenciaFixa()
    {
        var repository = new ReceitaRepoFake();
        var publisher = new RecorrenciaPublisherFake();
        var service = CriarService(repository, new ContaRepoFake(), new AreaRepoFake(), publisher, 99);

        await service.CriarAsync(CriarRequestPadrao(recorrencia: Recorrencia.Mensal, quantidadeRecorrencia: null, recorrenciaFixa: true));

        Assert.Single(repository.ReceitasCriadas);
        Assert.NotNull(publisher.ReceitaMessage);
        Assert.True(publisher.ReceitaMessage!.RecorrenciaFixa);
        Assert.Equal(100, publisher.ReceitaMessage!.QuantidadeRecorrencia);
    }

    [Fact]
    public async Task DeveRejeitarRecorrenciaFixaComTipoUnico()
    {
        var service = CriarService(new ReceitaRepoFake(), new ContaRepoFake(), new AreaRepoFake(), 99);

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            service.CriarAsync(CriarRequestPadrao(recorrencia: Recorrencia.Unica, recorrenciaFixa: true)));

        Assert.Equal("recorrencia_fixa_invalida", ex.Message);
    }

    [Fact]
    public async Task DeveRejeitarQuantidadeRecorrenciaMaiorQue100_QuandoNaoFixa()
    {
        var service = CriarService(new ReceitaRepoFake(), new ContaRepoFake(), new AreaRepoFake(), 99);

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            service.CriarAsync(CriarRequestPadrao(recorrencia: Recorrencia.Mensal, quantidadeRecorrencia: 101, recorrenciaFixa: false)));

        Assert.Equal("quantidade_recorrencia_invalida", ex.Message);
    }

    private static CriarReceitaRequest CriarRequestPadrao(
        string tipoRecebimento = "dinheiro",
        string? contaBancaria = null,
        IReadOnlyCollection<ReceitaAreaRateioRequest>? areasRateio = null,
        IReadOnlyCollection<AmigoRateioRequest>? amigos = null,
        Recorrencia recorrencia = Recorrencia.Unica,
        int? quantidadeRecorrencia = null,
        bool recorrenciaFixa = false) =>
        new(
            "Freelance",
            null,
            new DateOnly(2026, 3, 1),
            new DateOnly(2026, 3, 2),
            "freelance",
            tipoRecebimento,
            recorrencia,
            1000m,
            0m,
            0m,
            0m,
            0m,
            [],
            new Dictionary<string, decimal>(),
            areasRateio ?? [],
            contaBancaria,
            null,
            quantidadeRecorrencia,
            amigos,
            recorrenciaFixa);

    private static ReceitaService CriarService(IReceitaRepository receitaRepository, IContaBancariaRepository contaRepository, IAreaRepository areaRepository, int? usuarioId) =>
        CriarService(receitaRepository, contaRepository, areaRepository, new RecorrenciaPublisherFake(), usuarioId);

    private static ReceitaService CriarService(IReceitaRepository receitaRepository, IContaBancariaRepository contaRepository, IAreaRepository areaRepository, IRecorrenciaBackgroundPublisher publisher, int? usuarioId) =>
        new(receitaRepository, contaRepository, areaRepository, new UsuarioAutenticadoProviderFake(usuarioId), new HistoricoTransacaoFinanceiraService(new HistoricoRepositoryFake()), publisher);

    private sealed class UsuarioAutenticadoProviderFake(int? usuarioId) : IUsuarioAutenticadoProvider
    {
        public int? ObterUsuarioId() => usuarioId;
    }

    private sealed class ReceitaRepoFake : IReceitaRepository
    {
        public Receita? Receita { get; set; }
        public List<Receita> ReceitasCriadas { get; } = [];

        public Task<List<Receita>> ListarAsync(CancellationToken cancellationToken = default) => Task.FromResult(new List<Receita>());
        public Task<Receita?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default) => Task.FromResult(Receita);
        public Task<Receita> CriarAsync(Receita receita, CancellationToken cancellationToken = default)
        {
            ReceitasCriadas.Add(receita);
            return Task.FromResult(receita);
        }
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
        public List<Area> Areas { get; set; } = [];
        public List<SubArea> SubAreas { get; set; } = [];

        public Task<List<Area>> ListarComSubAreasAsync(TipoAreaFinanceira? tipo = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(Areas);

        public Task<List<SubArea>> ObterSubAreasPorIdsAsync(IReadOnlyCollection<long> subAreasIds, CancellationToken cancellationToken = default) =>
            Task.FromResult(SubAreas.Where(x => subAreasIds.Contains(x.Id)).ToList());
    }

    private static AreaRepoFake CriarAreaRepoValida(TipoAreaFinanceira tipoArea)
    {
        var area = new Area { Id = 1, Nome = "Area", Tipo = tipoArea };
        return new AreaRepoFake
        {
            SubAreas =
            [
                new SubArea { Id = 2, AreaId = area.Id, Area = area, Nome = "SubArea" }
            ]
        };
    }

    private sealed class HistoricoRepositoryFake : IHistoricoTransacaoFinanceiraRepository
    {
        public Task<HistoricoTransacaoFinanceira> CriarAsync(HistoricoTransacaoFinanceira historico, CancellationToken cancellationToken = default) =>
            Task.FromResult(historico);

        public Task<HistoricoTransacaoFinanceira?> ObterUltimoPorTransacaoAsync(TipoTransacaoFinanceira tipoTransacao, long transacaoId, CancellationToken cancellationToken = default) =>
            Task.FromResult<HistoricoTransacaoFinanceira?>(null);
    }

    private sealed class RecorrenciaPublisherFake : IRecorrenciaBackgroundPublisher
    {
        public ReceitaRecorrenciaBackgroundMessage? ReceitaMessage { get; private set; }

        public Task PublicarDespesaAsync(DespesaRecorrenciaBackgroundMessage message, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task PublicarReceitaAsync(ReceitaRecorrenciaBackgroundMessage message, CancellationToken cancellationToken = default)
        {
            ReceitaMessage = message;
            return Task.CompletedTask;
        }
    }
}
