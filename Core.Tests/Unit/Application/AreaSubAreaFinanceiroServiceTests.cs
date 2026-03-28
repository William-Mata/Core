using Core.Application.Services.Financeiro;
using Core.Domain.Entities.Financeiro;
using Core.Domain.Enums;
using Core.Domain.Exceptions;
using Core.Domain.Interfaces.Financeiro;

namespace Core.Tests.Unit.Application;

public sealed class AreaSubAreaFinanceiroServiceTests
{
    [Fact]
    public async Task DeveListarAreasSemFiltro_QuandoTipoNaoInformado()
    {
        var repo = new AreaRepoFake
        {
            Areas = [CriarAreaDespesa(), CriarAreaReceita()]
        };
        var service = new AreaSubAreaFinanceiroService(repo);

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
        var service = new AreaSubAreaFinanceiroService(repo);

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
        var service = new AreaSubAreaFinanceiroService(repo);

        var resultado = await service.ListarAreasComSubAreasAsync("ReCeItA");

        var area = Assert.Single(resultado);
        Assert.Equal("receita", area.Tipo);
        Assert.Equal(TipoAreaFinanceira.Receita, repo.UltimoTipoRecebido);
    }

    [Fact]
    public async Task DeveRetornarErro_QuandoTipoForInvalido()
    {
        var service = new AreaSubAreaFinanceiroService(new AreaRepoFake());

        var ex = await Assert.ThrowsAsync<DomainException>(() => service.ListarAreasComSubAreasAsync("foo"));

        Assert.Equal("tipo_area_invalido", ex.Message);
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
        public TipoAreaFinanceira? UltimoTipoRecebido { get; private set; }

        public Task<List<Area>> ListarComSubAreasAsync(TipoAreaFinanceira? tipo = null, CancellationToken cancellationToken = default)
        {
            UltimoTipoRecebido = tipo;
            return Task.FromResult(Areas);
        }

        public Task<List<SubArea>> ObterSubAreasPorIdsAsync(IReadOnlyCollection<long> subAreasIds, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<SubArea>());
    }
}
