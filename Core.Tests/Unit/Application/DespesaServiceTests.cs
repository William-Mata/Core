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

public sealed class DespesaServiceTests
{
    [Fact]
    public async Task DeveExigirUsuarioAutenticado_ParaCriarDespesa()
    {
        var service = CriarService(new DespesaRepositoryFake(), null);

        var ex = await Assert.ThrowsAsync<DomainException>(() => service.CriarAsync(CriarRequestPadrao()));

        Assert.Equal("usuario_nao_autenticado", ex.Message);
    }

    [Fact]
    public async Task DeveValidarDescricaoObrigatoria_AoCriarDespesa()
    {
        var service = CriarService(new DespesaRepositoryFake(), 1);

        var ex = await Assert.ThrowsAsync<DomainException>(() => service.CriarAsync(CriarRequestPadrao(descricao: "")));

        Assert.Equal("descricao_obrigatoria", ex.Message);
    }

    [Fact]
    public async Task DeveValidarPeriodoDaDespesa()
    {
        var service = CriarService(new DespesaRepositoryFake(), 1);

        var ex = await Assert.ThrowsAsync<DomainException>(() => service.CriarAsync(CriarRequestPadrao(dataLancamento: new DateOnly(2026, 3, 10), dataVencimento: new DateOnly(2026, 3, 1))));

        Assert.Equal("periodo_invalido", ex.Message);
    }

    [Fact]
    public async Task DeveImpedirEfetivacao_ComDadosInvalidos()
    {
        var repository = new DespesaRepositoryFake
        {
            Despesa = new Despesa { Id = 1, Descricao = "Despesa", DataLancamento = new DateOnly(2026, 3, 1), DataVencimento = new DateOnly(2026, 3, 2), TipoDespesa = "alimentacao", TipoPagamento = "pix", Recorrencia = Recorrencia.Unica, ValorTotal = 100m, ValorLiquido = 100m, Status = StatusDespesa.Pendente }
        };
        var service = CriarService(repository, 1);

        var ex = await Assert.ThrowsAsync<DomainException>(() => service.EfetivarAsync(1, new EfetivarDespesaRequest(new DateOnly(2026, 3, 5), "", 0m, 0m, 0m, 0m, 0m, null)));

        Assert.Equal("dados_invalidos", ex.Message);
    }

    [Fact]
    public async Task DeveRetornarErro_QuandoDespesaNaoForEncontrada()
    {
        var service = CriarService(new DespesaRepositoryFake(), 1);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => service.ObterAsync(99));

        Assert.Equal("despesa_nao_encontrada", ex.Message);
    }

    [Fact]
    public async Task DeveImpedirEfetivacao_QuandoDataEfetivacaoForMenorQueDataLancamento()
    {
        var repository = new DespesaRepositoryFake
        {
            Despesa = new Despesa
            {
                Id = 1,
                Descricao = "Despesa",
                DataLancamento = new DateOnly(2026, 3, 10),
                DataVencimento = new DateOnly(2026, 3, 15),
                TipoDespesa = "alimentacao",
                TipoPagamento = "pix",
                Recorrencia = Recorrencia.Unica,
                ValorTotal = 100m,
                ValorLiquido = 100m,
                Status = StatusDespesa.Pendente
            }
        };
        var service = CriarService(repository, 1);

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            service.EfetivarAsync(1, new EfetivarDespesaRequest(new DateOnly(2026, 3, 9), "pix", 100m, 0m, 0m, 0m, 0m, null)));

        Assert.Equal("periodo_invalido", ex.Message);
    }

    [Fact]
    public async Task DevePermitirEfetivacao_QuandoDataEfetivacaoForIgualDataLancamento()
    {
        var repository = new DespesaRepositoryFake
        {
            Despesa = new Despesa
            {
                Id = 1,
                Descricao = "Despesa",
                DataLancamento = new DateOnly(2026, 3, 10),
                DataVencimento = new DateOnly(2026, 3, 15),
                TipoDespesa = "alimentacao",
                TipoPagamento = "pix",
                Recorrencia = Recorrencia.Unica,
                ValorTotal = 100m,
                ValorLiquido = 100m,
                Status = StatusDespesa.Pendente
            }
        };
        var service = CriarService(repository, 1);

        var result = await service.EfetivarAsync(1, new EfetivarDespesaRequest(new DateOnly(2026, 3, 10), "pix", 100m, 0m, 0m, 0m, 0m, null));

        Assert.Equal("efetivada", result.Status);
    }

    [Fact]
    public async Task DevePublicarMensagemDeRecorrencia_AoCriarDespesaRecorrente()
    {
        var repository = new DespesaRepositoryFake();
        var publisher = new RecorrenciaPublisherFake();
        var service = CriarService(repository, new AreaRepoFake(), publisher, 1);

        await service.CriarAsync(CriarRequestPadrao(recorrencia: Recorrencia.Mensal, quantidadeRecorrencia: 2));

        Assert.Single(repository.DespesasCriadas);
        Assert.NotNull(publisher.DespesaMessage);
        Assert.Equal(2, publisher.DespesaMessage!.QuantidadeRecorrencia);
    }

    [Fact]
    public async Task DevePublicarMensagemComAlvo100_AoCriarDespesaComRecorrenciaFixa()
    {
        var repository = new DespesaRepositoryFake();
        var publisher = new RecorrenciaPublisherFake();
        var service = CriarService(repository, new AreaRepoFake(), publisher, 1);

        await service.CriarAsync(CriarRequestPadrao(recorrencia: Recorrencia.Mensal, quantidadeRecorrencia: null, recorrenciaFixa: true));

        Assert.Single(repository.DespesasCriadas);
        Assert.NotNull(publisher.DespesaMessage);
        Assert.True(publisher.DespesaMessage!.RecorrenciaFixa);
        Assert.Equal(100, publisher.DespesaMessage!.QuantidadeRecorrencia);
    }

    [Fact]
    public async Task DeveRejeitarRecorrenciaFixaComTipoUnico()
    {
        var service = CriarService(new DespesaRepositoryFake(), 1);

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            service.CriarAsync(CriarRequestPadrao(recorrencia: Recorrencia.Unica, recorrenciaFixa: true)));

        Assert.Equal("recorrencia_fixa_invalida", ex.Message);
    }

    [Fact]
    public async Task DeveValidarRelacaoEntreAreaESubArea_AoCriarDespesa()
    {
        var areaRepository = new AreaRepoFake
        {
            SubAreas = [new SubArea { Id = 2, AreaId = 99, Nome = "SubArea" }]
        };
        var service = CriarService(new DespesaRepositoryFake(), areaRepository, 1);

        var ex = await Assert.ThrowsAsync<DomainException>(() => service.CriarAsync(CriarRequestPadrao(areasRateio: [new DespesaAreaRateioRequest(1, 2, 100m)])));

        Assert.Equal("relacao_area_subarea_invalida", ex.Message);
    }

    [Fact]
    public async Task DeveRejeitarRateioDeAmigosQuandoSomaForDiferenteDoValorTotal_AoCriarDespesa()
    {
        var service = CriarService(new DespesaRepositoryFake(), 1);

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            service.CriarAsync(CriarRequestPadrao(amigos: [new AmigoRateioRequest("Amigo 1", 40m), new AmigoRateioRequest("Amigo 2", 30m)])));

        Assert.Equal("rateio_amigos_invalido", ex.Message);
    }

    [Fact]
    public async Task DeveRejeitarRateioDeAreaQuandoSomaForDiferenteDoValorTotal_AoCriarDespesa()
    {
        var areaRepository = CriarAreaRepoValida(TipoAreaFinanceira.Despesa);
        var service = CriarService(new DespesaRepositoryFake(), areaRepository, 1);

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            service.CriarAsync(CriarRequestPadrao(areasRateio: [new DespesaAreaRateioRequest(1, 2, 40m)])));

        Assert.Equal("rateio_area_invalido", ex.Message);
    }

    [Fact]
    public async Task DevePermitirCriacao_QuandoRateiosSomamValorTotal()
    {
        var areaRepository = CriarAreaRepoValida(TipoAreaFinanceira.Despesa);
        var service = CriarService(new DespesaRepositoryFake(), areaRepository, 1);

        var result = await service.CriarAsync(
            CriarRequestPadrao(
                amigos: [new AmigoRateioRequest("Amigo 1", 60m), new AmigoRateioRequest("Amigo 2", 40m)],
                areasRateio: [new DespesaAreaRateioRequest(1, 2, 100m)]));

        Assert.Equal("Despesa", result.Descricao);
        Assert.Equal(100m, result.ValorTotal);
        Assert.Equal(2, result.AmigosRateio.Count);
        Assert.Single(result.AreasSubAreasRateio);
    }

    [Fact]
    public async Task DeveRetornarErro_QuandoPagamentoForCartaoESemQuantidadeParcelas()
    {
        var service = CriarService(new DespesaRepositoryFake(), 1);

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            service.CriarAsync(CriarRequestPadrao(tipoPagamento: "cartaoCredito", quantidadeRecorrencia: null, quantidadeParcelas: null)));

        Assert.Equal("quantidade_parcelas_invalida", ex.Message);
    }

    [Fact]
    public async Task DeveTratarCartaoComoParcelamentoMensal_AoCriarDespesa()
    {
        var repository = new DespesaRepositoryFake();
        var publisher = new RecorrenciaPublisherFake();
        var service = CriarService(repository, new AreaRepoFake(), publisher, 1);

        await service.CriarAsync(CriarRequestPadrao(tipoPagamento: "cartaoCredito", quantidadeRecorrencia: null, quantidadeParcelas: 3, recorrenciaFixa: true));

        var despesa = Assert.Single(repository.DespesasCriadas);
        Assert.Equal(Recorrencia.Mensal, despesa.Recorrencia);
        Assert.False(despesa.RecorrenciaFixa);
        Assert.Equal(3, despesa.QuantidadeRecorrencia);

        Assert.NotNull(publisher.DespesaMessage);
        Assert.Equal(Recorrencia.Mensal, publisher.DespesaMessage!.Recorrencia);
        Assert.False(publisher.DespesaMessage.RecorrenciaFixa);
        Assert.Equal(3, publisher.DespesaMessage.QuantidadeRecorrencia);
    }

    [Fact]
    public async Task DeveRejeitarQuantidadeRecorrenciaMaiorQue100_QuandoNaoFixa()
    {
        var service = CriarService(new DespesaRepositoryFake(), 1);

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            service.CriarAsync(CriarRequestPadrao(recorrencia: Recorrencia.Mensal, quantidadeRecorrencia: 101, recorrenciaFixa: false)));

        Assert.Equal("quantidade_recorrencia_invalida", ex.Message);
    }

    private static CriarDespesaRequest CriarRequestPadrao(
        string descricao = "Despesa",
        DateOnly? dataLancamento = null,
        DateOnly? dataVencimento = null,
        IReadOnlyCollection<DespesaAreaRateioRequest>? areasRateio = null,
        IReadOnlyCollection<AmigoRateioRequest>? amigos = null,
        string tipoPagamento = "pix",
        Recorrencia recorrencia = Recorrencia.Unica,
        int? quantidadeRecorrencia = null,
        int? quantidadeParcelas = null,
        bool recorrenciaFixa = false) =>
        new(
            descricao,
            null,
            dataLancamento ?? new DateOnly(2026, 3, 1),
            dataVencimento ?? new DateOnly(2026, 3, 2),
            "alimentacao",
            tipoPagamento,
            recorrencia,
            100m,
            0m,
            0m,
            0m,
            0m,
            null,
            amigos,
            areasRateio,
            quantidadeRecorrencia,
            quantidadeParcelas,
            recorrenciaFixa);

    private static DespesaService CriarService(IDespesaRepository repository, int? usuarioId) =>
        CriarService(repository, new AreaRepoFake(), new RecorrenciaPublisherFake(), usuarioId);

    private static DespesaService CriarService(IDespesaRepository repository, IAreaRepository areaRepository, int? usuarioId) =>
        CriarService(repository, areaRepository, new RecorrenciaPublisherFake(), usuarioId);

    private static DespesaService CriarService(IDespesaRepository repository, IAreaRepository areaRepository, IRecorrenciaBackgroundPublisher publisher, int? usuarioId) =>
        new(repository, areaRepository, new UsuarioAutenticadoProviderFake(usuarioId), new HistoricoTransacaoFinanceiraService(new HistoricoRepositoryFake()), new DocumentoStorageServiceFake(), publisher);

    private sealed class DespesaRepositoryFake : IDespesaRepository
    {
        public Despesa? Despesa { get; set; }
        public List<Despesa> DespesasCriadas { get; } = [];

        public Task<List<Despesa>> ListarAsync(CancellationToken cancellationToken = default) => Task.FromResult(new List<Despesa>());
        public Task<List<Despesa>> ObterPorIdsAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken = default) => Task.FromResult(new List<Despesa>());
        public Task<Despesa?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default) => Task.FromResult(Despesa);
        public Task<Despesa> CriarAsync(Despesa despesa, CancellationToken cancellationToken = default)
        {
            DespesasCriadas.Add(despesa);
            return Task.FromResult(despesa);
        }
        public Task<Despesa> AtualizarAsync(Despesa despesa, CancellationToken cancellationToken = default) => Task.FromResult(despesa);
    }

    private sealed class UsuarioAutenticadoProviderFake(int? usuarioId) : IUsuarioAutenticadoProvider
    {
        public int? ObterUsuarioId() => usuarioId;
    }

    private sealed class HistoricoRepositoryFake : IHistoricoTransacaoFinanceiraRepository
    {
        public Task<HistoricoTransacaoFinanceira> CriarAsync(HistoricoTransacaoFinanceira historico, CancellationToken cancellationToken = default) =>
            Task.FromResult(historico);

        public Task<HistoricoTransacaoFinanceira?> ObterUltimoPorTransacaoAsync(TipoTransacaoFinanceira tipoTransacao, long transacaoId, CancellationToken cancellationToken = default) =>
            Task.FromResult<HistoricoTransacaoFinanceira?>(null);
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

    private sealed class RecorrenciaPublisherFake : IRecorrenciaBackgroundPublisher
    {
        public DespesaRecorrenciaBackgroundMessage? DespesaMessage { get; private set; }

        public Task PublicarDespesaAsync(DespesaRecorrenciaBackgroundMessage message, CancellationToken cancellationToken = default)
        {
            DespesaMessage = message;
            return Task.CompletedTask;
        }

        public Task PublicarReceitaAsync(ReceitaRecorrenciaBackgroundMessage message, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class DocumentoStorageServiceFake : IDocumentoStorageService
    {
        public Task<IReadOnlyCollection<DocumentoDto>> SalvarAsync(IReadOnlyCollection<DocumentoRequest> documentos, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<DocumentoDto>>(
                documentos.Select(x => new DocumentoDto(x.NomeArquivo, $@"C:\temp\{x.NomeArquivo}", x.ContentType, 1)).ToArray());
    }
}
