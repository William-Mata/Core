using Core.Application.DTOs.Financeiro;
using Core.Application.Services.Financeiro;
using Core.Domain.Entities.Financeiro;
using Core.Domain.Enums;
using Core.Domain.Interfaces;
using Core.Domain.Interfaces.Financeiro;

namespace Core.Tests.Unit.Application;

public sealed class FaturaCartaoServiceTests
{
    [Fact]
    public async Task DeveFecharFaturaAutomaticamente_AjustandoVencimentoEFechamentoParaDiaUtil()
    {
        var dataBase = DateTime.Now.AddMonths(-1);
        var competencia = dataBase.ToString("yyyy-MM");
        var ultimoDiaMes = DateTime.DaysInMonth(dataBase.Year, dataBase.Month);
        var dataVencimentoBase = Enumerable.Range(1, ultimoDiaMes)
            .Select(dia => new DateOnly(dataBase.Year, dataBase.Month, dia))
            .First(data => data.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday);
        var dataVencimentoEsperada = AjustarParaProximoDiaUtil(dataVencimentoBase);
        var dataFechamentoEsperada = AjustarParaDiaUtilAnteriorOuIgual(dataVencimentoEsperada.AddDays(-7));

        var repository = new FaturaCartaoRepositoryFake
        {
            Faturas =
            [
                new FaturaCartao
                {
                    Id = 10,
                    CartaoId = 3,
                    Competencia = competencia,
                    Status = StatusFaturaCartao.Aberta,
                    ValorTotal = 0m,
                    UsuarioCadastroId = 5
                }
            ]
        };
        var cartaoRepository = new CartaoRepositoryFake
        {
            Cartao = new Cartao
            {
                Id = 3,
                Tipo = TipoCartao.Credito,
                DiaVencimento = dataVencimentoBase
            }
        };
        var service = CriarService(repository, cartaoRepository, 5);

        var resultado = await service.ListarAsync(new ListarFaturasCartaoRequest(null, competencia));

        var fatura = Assert.Single(resultado);
        Assert.Equal("fechada", fatura.Status);
        Assert.Equal(dataFechamentoEsperada, fatura.DataFechamento);
        Assert.DoesNotContain(fatura.DataFechamento!.Value.DayOfWeek, [DayOfWeek.Saturday, DayOfWeek.Sunday]);
    }

    [Fact]
    public async Task NaoDeveFecharFaturaAutomaticamente_QuandoAindaNaoChegarDataDeFechamento()
    {
        var dataBaseFutura = DateTime.Now.AddYears(5);
        var competencia = dataBaseFutura.ToString("yyyy-MM");
        var dataVencimento = new DateOnly(dataBaseFutura.Year, dataBaseFutura.Month, 28);

        var repository = new FaturaCartaoRepositoryFake
        {
            Faturas =
            [
                new FaturaCartao
                {
                    Id = 11,
                    CartaoId = 7,
                    Competencia = competencia,
                    Status = StatusFaturaCartao.Aberta,
                    ValorTotal = 0m,
                    UsuarioCadastroId = 5
                }
            ]
        };
        var cartaoRepository = new CartaoRepositoryFake
        {
            Cartao = new Cartao
            {
                Id = 7,
                Tipo = TipoCartao.Credito,
                DiaVencimento = dataVencimento
            }
        };
        var service = CriarService(repository, cartaoRepository, 5);

        var resultado = await service.ListarAsync(new ListarFaturasCartaoRequest(null, competencia));

        var fatura = Assert.Single(resultado);
        Assert.Equal("aberta", fatura.Status);
        Assert.Null(fatura.DataFechamento);
    }

    private static FaturaCartaoService CriarService(
        IFaturaCartaoRepository faturaRepository,
        ICartaoRepository cartaoRepository,
        int usuarioId) =>
        new(
            faturaRepository,
            cartaoRepository,
            new DespesaRepositoryFake(),
            new ReceitaRepositoryFake(),
            new ReembolsoRepositoryFake(),
            new UsuarioAutenticadoProviderFake(usuarioId));

    private sealed class FaturaCartaoRepositoryFake : IFaturaCartaoRepository
    {
        public List<FaturaCartao> Faturas { get; set; } = [];

        public Task<List<FaturaCartao>> ListarPorUsuarioAsync(int usuarioCadastroId, long? cartaoId, string? competencia, CancellationToken cancellationToken = default)
        {
            var query = Faturas.AsEnumerable();
            if (cartaoId.HasValue)
                query = query.Where(x => x.CartaoId == cartaoId.Value);
            if (!string.IsNullOrWhiteSpace(competencia))
                query = query.Where(x => x.Competencia == competencia);
            return Task.FromResult(query.ToList());
        }

        public Task<FaturaCartao?> ObterPorIdAsync(long id, int usuarioCadastroId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Faturas.FirstOrDefault(x => x.Id == id));

        public Task<FaturaCartao?> ObterPorCartaoCompetenciaAsync(long cartaoId, int usuarioCadastroId, string competencia, CancellationToken cancellationToken = default) =>
            Task.FromResult(Faturas.FirstOrDefault(x => x.CartaoId == cartaoId && x.Competencia == competencia));

        public Task<FaturaCartao> CriarAsync(FaturaCartao fatura, CancellationToken cancellationToken = default)
        {
            Faturas.Add(fatura);
            return Task.FromResult(fatura);
        }

        public Task<FaturaCartao> AtualizarAsync(FaturaCartao fatura, CancellationToken cancellationToken = default) =>
            Task.FromResult(fatura);
    }

    private sealed class CartaoRepositoryFake : ICartaoRepository
    {
        public Cartao? Cartao { get; set; }

        public Task<List<Cartao>> ListarAsync(CancellationToken cancellationToken = default) => Task.FromResult(new List<Cartao>());

        public Task<List<Cartao>> ListarAsync(int usuarioCadastroId, CancellationToken cancellationToken = default) => ListarAsync(cancellationToken);

        public Task<Cartao?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default) => Task.FromResult(Cartao);

        public Task<Cartao?> ObterPorIdAsync(long id, int usuarioCadastroId, CancellationToken cancellationToken = default) => ObterPorIdAsync(id, cancellationToken);

        public Task<Cartao> CriarAsync(Cartao cartao, CancellationToken cancellationToken = default) => Task.FromResult(cartao);

        public Task<Cartao> AtualizarAsync(Cartao cartao, CancellationToken cancellationToken = default) => Task.FromResult(cartao);
    }

    private sealed class DespesaRepositoryFake : IDespesaRepository
    {
        public Task<List<Despesa>> ListarAsync(CancellationToken cancellationToken = default) => Task.FromResult(new List<Despesa>());

        public Task<List<Despesa>> ListarAsync(string? filtroId, string? descricao, string? competencia, DateOnly? dataInicio, DateOnly? dataFim, CancellationToken cancellationToken = default) => Task.FromResult(new List<Despesa>());

        public Task<List<Despesa>> ListarPorUsuarioAsync(int usuarioCadastroId, string? filtroId, string? descricao, string? competencia, DateOnly? dataInicio, DateOnly? dataFim, CancellationToken cancellationToken = default) => Task.FromResult(new List<Despesa>());

        public Task<List<Despesa>> ListarPendentesAprovacaoPorUsuarioAsync(int usuarioCadastroId, CancellationToken cancellationToken = default) => Task.FromResult(new List<Despesa>());

        public Task<List<Despesa>> ListarEspelhosPorOrigemAsync(long despesaOrigemId, CancellationToken cancellationToken = default) => Task.FromResult(new List<Despesa>());

        public Task<List<Despesa>> ObterPorIdsAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken = default) => Task.FromResult(new List<Despesa>());

        public Task<List<Despesa>> ObterPorIdsAsync(IReadOnlyCollection<long> ids, int usuarioCadastroId, CancellationToken cancellationToken = default) => Task.FromResult(new List<Despesa>());

        public Task<Despesa?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default) => Task.FromResult<Despesa?>(null);

        public Task<Despesa?> ObterPorIdAsync(long id, int usuarioCadastroId, CancellationToken cancellationToken = default) => Task.FromResult<Despesa?>(null);

        public Task<Despesa> CriarAsync(Despesa despesa, CancellationToken cancellationToken = default) => Task.FromResult(despesa);

        public Task<Despesa> AtualizarAsync(Despesa despesa, CancellationToken cancellationToken = default) => Task.FromResult(despesa);
    }

    private sealed class ReceitaRepositoryFake : IReceitaRepository
    {
        public Task<List<Receita>> ListarAsync(CancellationToken cancellationToken = default) => Task.FromResult(new List<Receita>());

        public Task<List<Receita>> ListarAsync(string? filtroId, string? descricao, string? competencia, DateOnly? dataInicio, DateOnly? dataFim, CancellationToken cancellationToken = default) => Task.FromResult(new List<Receita>());

        public Task<List<Receita>> ListarPorUsuarioAsync(int usuarioCadastroId, string? filtroId, string? descricao, string? competencia, DateOnly? dataInicio, DateOnly? dataFim, CancellationToken cancellationToken = default) => Task.FromResult(new List<Receita>());

        public Task<List<Receita>> ListarPendentesAprovacaoPorUsuarioAsync(int usuarioCadastroId, CancellationToken cancellationToken = default) => Task.FromResult(new List<Receita>());

        public Task<List<Receita>> ListarEspelhosPorOrigemAsync(long receitaOrigemId, CancellationToken cancellationToken = default) => Task.FromResult(new List<Receita>());

        public Task<Receita?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default) => Task.FromResult<Receita?>(null);

        public Task<Receita?> ObterPorIdAsync(long id, int usuarioCadastroId, CancellationToken cancellationToken = default) => Task.FromResult<Receita?>(null);

        public Task<Receita> CriarAsync(Receita receita, CancellationToken cancellationToken = default) => Task.FromResult(receita);

        public Task<Receita> AtualizarAsync(Receita receita, CancellationToken cancellationToken = default) => Task.FromResult(receita);
    }

    private sealed class ReembolsoRepositoryFake : IReembolsoRepository
    {
        public Task<List<Reembolso>> ListarAsync(string? filtroId, string? descricao, string? competencia, DateOnly? dataInicio, DateOnly? dataFim, CancellationToken cancellationToken = default) => Task.FromResult(new List<Reembolso>());

        public Task<List<Reembolso>> ListarAsync(int usuarioCadastroId, string? filtroId, string? descricao, string? competencia, DateOnly? dataInicio, DateOnly? dataFim, CancellationToken cancellationToken = default) => Task.FromResult(new List<Reembolso>());

        public Task<Reembolso?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default) => Task.FromResult<Reembolso?>(null);

        public Task<Reembolso?> ObterPorIdAsync(long id, int usuarioCadastroId, CancellationToken cancellationToken = default) => Task.FromResult<Reembolso?>(null);

        public Task<Reembolso> CriarAsync(Reembolso reembolso, CancellationToken cancellationToken = default) => Task.FromResult(reembolso);

        public Task<Reembolso> AtualizarAsync(Reembolso reembolso, CancellationToken cancellationToken = default) => Task.FromResult(reembolso);

        public Task ExcluirAsync(Reembolso reembolso, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<bool> ExisteDespesaVinculadaEmOutroReembolsoAsync(IReadOnlyCollection<long> despesasIds, long? reembolsoIgnoradoId, CancellationToken cancellationToken = default) => Task.FromResult(false);

        public Task<bool> ExisteDespesaVinculadaEmOutroReembolsoAsync(int usuarioCadastroId, IReadOnlyCollection<long> despesasIds, long? reembolsoIgnoradoId, CancellationToken cancellationToken = default) => Task.FromResult(false);
    }

    private sealed class UsuarioAutenticadoProviderFake(int? usuarioId) : IUsuarioAutenticadoProvider
    {
        public int? ObterUsuarioId() => usuarioId;
    }

    private static DateOnly AjustarParaProximoDiaUtil(DateOnly data)
    {
        while (data.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            data = data.AddDays(1);

        return data;
    }

    private static DateOnly AjustarParaDiaUtilAnteriorOuIgual(DateOnly data)
    {
        while (data.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            data = data.AddDays(-1);

        return data;
    }
}
