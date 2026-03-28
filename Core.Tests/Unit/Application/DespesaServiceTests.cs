using Core.Application.DTOs;
using Core.Application.Services.Financeiro;
using Core.Domain.Entities;
using Core.Domain.Enums;
using Core.Domain.Exceptions;
using Core.Domain.Interfaces;

namespace Core.Tests.Unit.Application;

public sealed class DespesaServiceTests
{
    [Fact]
    public async Task DeveExigirUsuarioAutenticado_ParaCriarDespesa()
    {
        var service = new DespesaService(new DespesaRepositoryFake(), new UsuarioAutenticadoProviderFake(null));

        var ex = await Assert.ThrowsAsync<DomainException>(() => service.CriarAsync(CriarRequestPadrao()));

        Assert.Equal("usuario_nao_autenticado", ex.Message);
    }

    [Fact]
    public async Task DeveValidarDescricaoObrigatoria_AoCriarDespesa()
    {
        var service = new DespesaService(new DespesaRepositoryFake(), new UsuarioAutenticadoProviderFake(1));

        var ex = await Assert.ThrowsAsync<DomainException>(() => service.CriarAsync(CriarRequestPadrao(descricao: "")));

        Assert.Equal("descricao_obrigatoria", ex.Message);
    }

    [Fact]
    public async Task DeveValidarPeriodoDaDespesa()
    {
        var service = new DespesaService(new DespesaRepositoryFake(), new UsuarioAutenticadoProviderFake(1));

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
        var service = new DespesaService(repository, new UsuarioAutenticadoProviderFake(1));

        var ex = await Assert.ThrowsAsync<DomainException>(() => service.EfetivarAsync(1, new EfetivarDespesaRequest(new DateOnly(2026, 3, 5), "", 0m, 0m, 0m, 0m, 0m, null)));

        Assert.Equal("dados_invalidos", ex.Message);
    }

    [Fact]
    public async Task DeveRetornarErro_QuandoDespesaNaoForEncontrada()
    {
        var service = new DespesaService(new DespesaRepositoryFake(), new UsuarioAutenticadoProviderFake(1));

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => service.ObterAsync(99));

        Assert.Equal("despesa_nao_encontrada", ex.Message);
    }

    private static CriarDespesaRequest CriarRequestPadrao(
        string descricao = "Despesa",
        DateOnly? dataLancamento = null,
        DateOnly? dataVencimento = null) =>
        new(
            descricao,
            null,
            dataLancamento ?? new DateOnly(2026, 3, 1),
            dataVencimento ?? new DateOnly(2026, 3, 2),
            "alimentacao",
            "pix",
            Recorrencia.Unica,
            100m,
            0m,
            0m,
            0m,
            0m,
            [],
            [],
            null);

    private sealed class DespesaRepositoryFake : IDespesaRepository
    {
        public Despesa? Despesa { get; set; }

        public Task<List<Despesa>> ListarAsync(CancellationToken cancellationToken = default) => Task.FromResult(new List<Despesa>());
        public Task<List<Despesa>> ObterPorIdsAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken = default) => Task.FromResult(new List<Despesa>());
        public Task<Despesa?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default) => Task.FromResult(Despesa);
        public Task<Despesa> CriarAsync(Despesa despesa, CancellationToken cancellationToken = default) => Task.FromResult(despesa);
        public Task<Despesa> AtualizarAsync(Despesa despesa, CancellationToken cancellationToken = default) => Task.FromResult(despesa);
    }

    private sealed class UsuarioAutenticadoProviderFake(int? usuarioId) : IUsuarioAutenticadoProvider
    {
        public int? ObterUsuarioId() => usuarioId;
    }
}
