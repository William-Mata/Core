using Core.Application.Services.Financeiro;
using Core.Domain.Entities.Financeiro;
using Core.Domain.Enums.Financeiro;
using Core.Domain.Exceptions;
using Core.Domain.Interfaces;
using Core.Domain.Interfaces.Financeiro;

namespace Core.Tests.Unit.Application.Financeiro;

public sealed class AreaSubAreaFinanceiroServiceTests
{
    [Fact]
    public async Task DeveListarAreasSemFiltro_QuandoTipoNaoInformado()
    {
        var repo = new AreaRepoFake
        {
            Areas = [CriarAreaDespesa(), CriarAreaReceita()]
        };
        var service = new AreaSubAreaFinanceiroService(repo, new UsuarioAutenticadoProviderFake(1));

        var resultado = await service.ListarAreasComSubAreasAsync();

        Assert.Equal(2, resultado.Count);
        Assert.Null(repo.UltimoTipoRecebido);
    }

    [Fact]
    public async Task DeveFiltrarAreasPorTipoDespesa()
    {
        var repo = new AreaRepoFake
        {
            Areas = [CriarAreaDespesa()]
        };
        var service = new AreaSubAreaFinanceiroService(repo, new UsuarioAutenticadoProviderFake(1));

        var resultado = await service.ListarAreasComSubAreasAsync("despesa");

        var area = Assert.Single(resultado);
        Assert.Equal("despesa", area.Tipo);
        Assert.Equal(TipoAreaFinanceira.Despesa, repo.UltimoTipoRecebido);
    }

    [Fact]
    public async Task DeveAceitarTipoCaseInsensitive()
    {
        var repo = new AreaRepoFake
        {
            Areas = [CriarAreaReceita()]
        };
        var service = new AreaSubAreaFinanceiroService(repo, new UsuarioAutenticadoProviderFake(1));

        var resultado = await service.ListarAreasComSubAreasAsync("ReCeItA");

        var area = Assert.Single(resultado);
        Assert.Equal("receita", area.Tipo);
        Assert.Equal(TipoAreaFinanceira.Receita, repo.UltimoTipoRecebido);
    }

    [Fact]
    public async Task DeveRetornarErro_QuandoTipoForInvalido()
    {
        var service = new AreaSubAreaFinanceiroService(new AreaRepoFake(), new UsuarioAutenticadoProviderFake(1));

        var ex = await Assert.ThrowsAsync<DomainException>(() => service.ListarAreasComSubAreasAsync("foo"));

        Assert.Equal("tipo_area_invalido", ex.Message);
    }

    [Fact]
    public async Task DeveListarSomaRateioPorAreaESubArea()
    {
        var repo = new AreaRepoFake
        {
            Areas = [CriarAreaDespesa()],
            SomasRateio =
            [
                new AreaSubAreaRateioSoma(1, 1, 25m),
                new AreaSubAreaRateioSoma(1, 2, 75m)
            ]
        };
        var service = new AreaSubAreaFinanceiroService(repo, new UsuarioAutenticadoProviderFake(9));

        var resultado = await service.ListarAreasComSubAreasESomaRateioAsync("despesa");

        var area = Assert.Single(resultado);
        Assert.Equal(100m, area.ValorTotalRateio);
        Assert.Equal(9, repo.UltimoUsuarioIdRecebido);
        Assert.Equal(TipoAreaFinanceira.Despesa, repo.UltimoTipoRecebidoNoRateio);
        Assert.Equal(2, area.SubAreas.Count);
        Assert.Equal(25m, area.SubAreas.First(x => x.Id == 1).ValorTotalRateio);
        Assert.Equal(75m, area.SubAreas.First(x => x.Id == 2).ValorTotalRateio);
    }

    [Fact]
    public async Task DevePreencherZero_QuandoSubAreaNaoPossuiRateio()
    {
        var repo = new AreaRepoFake
        {
            Areas = [CriarAreaDespesa()],
            SomasRateio = [new AreaSubAreaRateioSoma(1, 1, 10m)]
        };
        var service = new AreaSubAreaFinanceiroService(repo, new UsuarioAutenticadoProviderFake(5));

        var resultado = await service.ListarAreasComSubAreasESomaRateioAsync("despesa");

        var area = Assert.Single(resultado);
        Assert.Equal(10m, area.ValorTotalRateio);
        Assert.Equal(0m, area.SubAreas.First(x => x.Id == 2).ValorTotalRateio);
    }

    [Fact]
    public async Task DeveRetornarErro_QuandoUsuarioNaoAutenticado_ParaSomaRateio()
    {
        var service = new AreaSubAreaFinanceiroService(new AreaRepoFake(), new UsuarioAutenticadoProviderFake(null));

        var ex = await Assert.ThrowsAsync<DomainException>(() => service.ListarAreasComSubAreasESomaRateioAsync());

        Assert.Equal("usuario_nao_autenticado", ex.Message);
    }

    private static Area CriarAreaDespesa() =>
        new()
        {
            Id = 1,
            Nome = "Alimentacao",
            Tipo = TipoAreaFinanceira.Despesa,
            SubAreas =
            [
                new SubArea { Id = 2, AreaId = 1, Nome = "Supermercado" },
                new SubArea { Id = 1, AreaId = 1, Nome = "Almoco" }
            ]
        };

    private static Area CriarAreaReceita() =>
        new()
        {
            Id = 10,
            Nome = "Salario",
            Tipo = TipoAreaFinanceira.Receita,
            SubAreas = [new SubArea { Id = 11, AreaId = 10, Nome = "Holerite" }]
        };

    private sealed class AreaRepoFake : IAreaRepository
    {
        public List<Area> Areas { get; set; } = [];
        public List<AreaSubAreaRateioSoma> SomasRateio { get; set; } = [];
        public TipoAreaFinanceira? UltimoTipoRecebido { get; private set; }
        public TipoAreaFinanceira? UltimoTipoRecebidoNoRateio { get; private set; }
        public int UltimoUsuarioIdRecebido { get; private set; }

        public Task<List<Area>> ListarComSubAreasAsync(TipoAreaFinanceira? tipo = null, CancellationToken cancellationToken = default)
        {
            UltimoTipoRecebido = tipo;
            return Task.FromResult(Areas);
        }

        public Task<List<SubArea>> ObterSubAreasPorIdsAsync(IReadOnlyCollection<long> subAreasIds, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<SubArea>());

        public Task<List<AreaSubAreaRateioSoma>> ListarSomaRateioPorAreaSubAreaAsync(int usuarioId, TipoAreaFinanceira? tipo = null, CancellationToken cancellationToken = default)
        {
            UltimoUsuarioIdRecebido = usuarioId;
            UltimoTipoRecebidoNoRateio = tipo;
            return Task.FromResult(SomasRateio);
        }
    }

    private sealed class UsuarioAutenticadoProviderFake(int? usuarioId) : IUsuarioAutenticadoProvider
    {
        public int? ObterUsuarioId() => usuarioId;
    }
}
