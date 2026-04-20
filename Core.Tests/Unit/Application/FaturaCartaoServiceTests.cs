using Core.Application.DTOs.Financeiro;
using Core.Application.Services.Financeiro;
using Core.Domain.Common;
using Core.Domain.Entities.Financeiro;
using Core.Domain.Enums;
using Core.Domain.Exceptions;
using Core.Domain.Interfaces;
using Core.Domain.Interfaces.Financeiro;

namespace Core.Tests.Unit.Application;

public sealed class FaturaCartaoServiceTests
{
    [Fact]
    public async Task DeveMarcarFaturaComoVencida_QuandoDataAtualForMaiorQueVencimento()
    {
        var dataBase = DataHoraBrasil.Agora().AddMonths(-1);
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
            Cartoes =
            [
                new Cartao
                {
                    Id = 3,
                    Tipo = TipoCartao.Credito,
                    DiaVencimento = dataVencimentoBase
                }
            ]
        };
        var service = CriarService(repository, cartaoRepository, 5);

        var resultado = await service.ListarAsync(new ListarFaturasCartaoRequest(null, competencia));

        var fatura = Assert.Single(resultado);
        Assert.Equal("vencida", fatura.Status);
        Assert.Equal(dataVencimentoEsperada, fatura.DataVencimento);
        Assert.Equal(dataFechamentoEsperada, fatura.DataFechamento);
        Assert.NotEqual(DayOfWeek.Saturday, fatura.DataFechamento!.Value.DayOfWeek);
        Assert.NotEqual(DayOfWeek.Sunday, fatura.DataFechamento.Value.DayOfWeek);
    }

    [Fact]
    public async Task NaoDeveFecharFaturaAutomaticamente_QuandoAindaNaoChegarDataDeFechamento()
    {
        var dataBaseFutura = DataHoraBrasil.Agora().AddYears(5);
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
            Cartoes =
            [
                new Cartao
                {
                    Id = 7,
                    Tipo = TipoCartao.Credito,
                    DiaVencimento = dataVencimento
                }
            ]
        };
        var service = CriarService(repository, cartaoRepository, 5);

        var resultado = await service.ListarAsync(new ListarFaturasCartaoRequest(null, competencia));

        var fatura = Assert.Single(resultado);
        Assert.Equal("aberta", fatura.Status);
        Assert.Null(fatura.DataFechamento);
    }

    [Fact]
    public async Task DeveCriarFaturaComDataVencimentoAjustadaParaProximoDiaUtil()
    {
        var dataBaseFutura = DataHoraBrasil.Agora().AddYears(5);
        var competencia = dataBaseFutura.ToString("yyyy-MM");
        var ultimoDiaMes = DateTime.DaysInMonth(dataBaseFutura.Year, dataBaseFutura.Month);
        var dataVencimentoBase = Enumerable.Range(1, ultimoDiaMes)
            .Select(dia => new DateOnly(dataBaseFutura.Year, dataBaseFutura.Month, dia))
            .First(data => data.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday);
        var dataVencimentoEsperada = AjustarParaProximoDiaUtil(dataVencimentoBase);

        var repository = new FaturaCartaoRepositoryFake();
        var cartaoRepository = new CartaoRepositoryFake
        {
            Cartoes =
            [
                new Cartao
                {
                    Id = 9,
                    Tipo = TipoCartao.Credito,
                    DiaVencimento = dataVencimentoBase
                }
            ]
        };
        var service = CriarService(repository, cartaoRepository, 5);

        var faturaId = await service.ResolverFaturaIdParaTransacaoCartaoAsync(9, competencia, 5);

        Assert.NotNull(faturaId);
        var faturaCriada = Assert.Single(repository.Faturas);
        Assert.Equal(faturaId.Value, faturaCriada.Id);
        Assert.Equal(dataVencimentoEsperada, faturaCriada.DataVencimento);
        Assert.NotEqual(DayOfWeek.Saturday, faturaCriada.DataVencimento!.Value.DayOfWeek);
        Assert.NotEqual(DayOfWeek.Sunday, faturaCriada.DataVencimento.Value.DayOfWeek);
    }

    [Fact]
    public async Task DeveEfetivarFatura_GerandoDespesaPagamentoEDevolvendoLimite()
    {
        var competencia = DataHoraBrasil.Agora().ToString("yyyy-MM");
        var faturas = new FaturaCartaoRepositoryFake
        {
            Faturas =
            [
                new FaturaCartao
                {
                    Id = 1,
                    CartaoId = 20,
                    Competencia = competencia,
                    DataVencimento = DataHoraBrasil.Hoje().AddDays(5),
                    Status = StatusFaturaCartao.Aberta,
                    ValorTotal = 1200m,
                    UsuarioCadastroId = 5
                }
            ]
        };
        var cartoes = new CartaoRepositoryFake
        {
            Cartoes =
            [
                new Cartao
                {
                    Id = 20,
                    Tipo = TipoCartao.Credito,
                    SaldoDisponivel = 300m
                }
            ]
        };
        var contas = new ContaBancariaRepositoryFake
        {
            Contas =
            [
                new ContaBancaria
                {
                    Id = 10,
                    UsuarioCadastroId = 5,
                    SaldoAtual = 2000m
                }
            ]
        };
        var despesas = new DespesaRepositoryFake
        {
            Despesas =
            [
                new Despesa
                {
                    Id = 100,
                    UsuarioCadastroId = 5,
                    Descricao = "Compra no cartao",
                    Competencia = competencia,
                    DataLancamento = DataHoraBrasil.Agora(),
                    TipoPagamento = TipoPagamento.CartaoCredito,
                    ValorTotal = 1200m,
                    ValorLiquido = 1200m,
                    ValorEfetivacao = 1200m,
                    Status = StatusDespesa.Efetivada,
                    FaturaCartaoId = 1,
                    CartaoId = 20
                }
            ]
        };
        var historicos = new HistoricoTransacaoFinanceiraRepositoryFake(contas, cartoes);
        var service = CriarService(faturas, cartoes, 5, contas, despesas, historicos);
        var request = new EfetivarFaturaCartaoRequest(
            DataEfetivacao: DataHoraBrasil.Agora(),
            ContaBancariaId: 10,
            ValorTotal: 1200m,
            ValorEfetivacao: 1000m);

        var resultado = await service.EfetivarAsync(1, request);

        Assert.Equal("efetivada", resultado.Status);
        Assert.Equal(DateOnly.FromDateTime(request.DataEfetivacao), resultado.DataEfetivacao);
        var faturaPersistida = Assert.Single(faturas.Faturas);
        Assert.Equal(StatusFaturaCartao.Efetivada, faturaPersistida.Status);
        Assert.True(faturaPersistida.DespesaPagamentoId.HasValue);

        var despesaPagamento = Assert.Single(despesas.Despesas.Where(x => !x.CartaoId.HasValue && x.ContaBancariaId == 10));
        Assert.Equal(StatusDespesa.Efetivada, despesaPagamento.Status);
        Assert.Equal(1000m, despesaPagamento.ValorTotal);
        Assert.Equal(1000m, despesaPagamento.ValorEfetivacao);
        Assert.Equal(TipoPagamento.Transferencia, despesaPagamento.TipoPagamento);
        Assert.Equal(10, despesaPagamento.ContaBancariaId);

        Assert.Equal(1000m, contas.Contas.Single().SaldoAtual);
        Assert.Equal(1300m, cartoes.Cartoes.Single().SaldoDisponivel);
    }

    [Fact]
    public async Task DeveEstornarFatura_DesfazendoEfeitosEPermitindoOcultarTransacao()
    {
        var competencia = DataHoraBrasil.Agora().ToString("yyyy-MM");
        var faturas = new FaturaCartaoRepositoryFake
        {
            Faturas =
            [
                new FaturaCartao
                {
                    Id = 2,
                    CartaoId = 30,
                    Competencia = competencia,
                    DataVencimento = DataHoraBrasil.Hoje().AddDays(3),
                    Status = StatusFaturaCartao.Aberta,
                    ValorTotal = 900m,
                    UsuarioCadastroId = 5
                }
            ]
        };
        var cartoes = new CartaoRepositoryFake
        {
            Cartoes =
            [
                new Cartao
                {
                    Id = 30,
                    Tipo = TipoCartao.Credito,
                    SaldoDisponivel = 200m
                }
            ]
        };
        var contas = new ContaBancariaRepositoryFake
        {
            Contas =
            [
                new ContaBancaria
                {
                    Id = 11,
                    UsuarioCadastroId = 5,
                    SaldoAtual = 1500m
                }
            ]
        };
        var despesas = new DespesaRepositoryFake
        {
            Despesas =
            [
                new Despesa
                {
                    Id = 101,
                    UsuarioCadastroId = 5,
                    Descricao = "Compra no cartao",
                    Competencia = competencia,
                    DataLancamento = DataHoraBrasil.Agora(),
                    TipoPagamento = TipoPagamento.CartaoCredito,
                    ValorTotal = 900m,
                    ValorLiquido = 900m,
                    ValorEfetivacao = 900m,
                    Status = StatusDespesa.Efetivada,
                    FaturaCartaoId = 2,
                    CartaoId = 30
                }
            ]
        };
        var historicos = new HistoricoTransacaoFinanceiraRepositoryFake(contas, cartoes);
        var service = CriarService(faturas, cartoes, 5, contas, despesas, historicos);

        await service.EfetivarAsync(2, new EfetivarFaturaCartaoRequest(
            DataEfetivacao: DataHoraBrasil.Agora(),
            ContaBancariaId: 11,
            ValorTotal: 900m,
            ValorEfetivacao: 700m));

        var retornoEstorno = await service.EstornarAsync(2, new EstornarFaturaCartaoRequest(
            DataEstorno: DataHoraBrasil.Hoje(),
            OcultarDoHistorico: true));

        Assert.Equal("estornada", retornoEstorno.Status);
        var faturaPersistida = Assert.Single(faturas.Faturas);
        Assert.Equal(StatusFaturaCartao.Estornada, faturaPersistida.Status);
        Assert.Null(faturaPersistida.DataEfetivacao);
        Assert.Equal(DataHoraBrasil.Hoje(), faturaPersistida.DataEstorno);

        var despesaPagamento = Assert.Single(despesas.Despesas.Where(x => !x.CartaoId.HasValue && x.ContaBancariaId == 11));
        Assert.Equal(StatusDespesa.Pendente, despesaPagamento.Status);
        Assert.Null(despesaPagamento.DataEfetivacao);
        Assert.Null(despesaPagamento.ValorEfetivacao);

        Assert.Equal(1500m, contas.Contas.Single().SaldoAtual);
        Assert.Equal(200m, cartoes.Cartoes.Single().SaldoDisponivel);
        Assert.Contains(historicos.Historicos, x => x.OcultarDoHistorico);
    }

    [Fact]
    public async Task NaoDevePermitirEfetivacao_QuandoStatusNaoForPermitido()
    {
        var competencia = DataHoraBrasil.Agora().ToString("yyyy-MM");
        var repository = new FaturaCartaoRepositoryFake
        {
            Faturas =
            [
                new FaturaCartao
                {
                    Id = 1,
                    CartaoId = 3,
                    Competencia = competencia,
                    Status = StatusFaturaCartao.Efetivada,
                    ValorTotal = 100m,
                    UsuarioCadastroId = 5
                }
            ]
        };
        var cartaoRepository = new CartaoRepositoryFake { Cartoes = [new Cartao { Id = 3, Tipo = TipoCartao.Credito }] };
        var contaRepository = new ContaBancariaRepositoryFake { Contas = [new ContaBancaria { Id = 1, UsuarioCadastroId = 5, SaldoAtual = 1000m }] };
        var service = CriarService(repository, cartaoRepository, 5, contaRepository);

        await Assert.ThrowsAsync<DomainException>(() => service.EfetivarAsync(1, new EfetivarFaturaCartaoRequest(
            DataEfetivacao: DataHoraBrasil.Agora(),
            ContaBancariaId: 1,
            ValorTotal: 100m,
            ValorEfetivacao: 100m)));
    }

    [Fact]
    public async Task NaoDevePermitirEfetivacao_QuandoExistirTransacaoDaFaturaNaoEfetivada()
    {
        var competencia = DataHoraBrasil.Agora().ToString("yyyy-MM");
        var repository = new FaturaCartaoRepositoryFake
        {
            Faturas =
            [
                new FaturaCartao
                {
                    Id = 7,
                    CartaoId = 3,
                    Competencia = competencia,
                    Status = StatusFaturaCartao.Fechada,
                    ValorTotal = 100m,
                    UsuarioCadastroId = 5
                }
            ]
        };
        var cartoes = new CartaoRepositoryFake { Cartoes = [new Cartao { Id = 3, Tipo = TipoCartao.Credito, SaldoDisponivel = 100m }] };
        var contas = new ContaBancariaRepositoryFake { Contas = [new ContaBancaria { Id = 1, UsuarioCadastroId = 5, SaldoAtual = 1000m }] };
        var despesas = new DespesaRepositoryFake
        {
            Despesas =
            [
                new Despesa
                {
                    Id = 110,
                    UsuarioCadastroId = 5,
                    Descricao = "Compra pendente no cartao",
                    Competencia = competencia,
                    DataLancamento = DataHoraBrasil.Agora(),
                    TipoPagamento = TipoPagamento.CartaoCredito,
                    ValorTotal = 100m,
                    ValorLiquido = 100m,
                    Status = StatusDespesa.Pendente,
                    FaturaCartaoId = 7,
                    CartaoId = 3
                }
            ]
        };
        var historicos = new HistoricoTransacaoFinanceiraRepositoryFake(contas, cartoes);
        var service = CriarService(repository, cartoes, 5, contas, despesas, historicos);

        var ex = await Assert.ThrowsAsync<DomainException>(() => service.EfetivarAsync(7, new EfetivarFaturaCartaoRequest(
            DataEfetivacao: DataHoraBrasil.Agora(),
            ContaBancariaId: 1,
            ValorTotal: 100m,
            ValorEfetivacao: 100m)));

        Assert.Equal("fatura_transacoes_pendentes", ex.Message);
    }

    [Fact]
    public async Task DevePermitirEfetivacao_QuandoStatusForVencida()
    {
        var competencia = DataHoraBrasil.Agora().ToString("yyyy-MM");
        var repository = new FaturaCartaoRepositoryFake
        {
            Faturas =
            [
                new FaturaCartao
                {
                    Id = 5,
                    CartaoId = 3,
                    Competencia = competencia,
                    Status = StatusFaturaCartao.Vencida,
                    ValorTotal = 100m,
                    UsuarioCadastroId = 5
                }
            ]
        };
        var cartoes = new CartaoRepositoryFake { Cartoes = [new Cartao { Id = 3, Tipo = TipoCartao.Credito, SaldoDisponivel = 0m }] };
        var contas = new ContaBancariaRepositoryFake { Contas = [new ContaBancaria { Id = 1, UsuarioCadastroId = 5, SaldoAtual = 1000m }] };
        var despesas = new DespesaRepositoryFake
        {
            Despesas =
            [
                new Despesa
                {
                    Id = 102,
                    UsuarioCadastroId = 5,
                    Descricao = "Compra no cartao",
                    Competencia = competencia,
                    DataLancamento = DataHoraBrasil.Agora(),
                    TipoPagamento = TipoPagamento.CartaoCredito,
                    ValorTotal = 100m,
                    ValorLiquido = 100m,
                    ValorEfetivacao = 100m,
                    Status = StatusDespesa.Efetivada,
                    FaturaCartaoId = 5,
                    CartaoId = 3
                }
            ]
        };
        var historicos = new HistoricoTransacaoFinanceiraRepositoryFake(contas, cartoes);
        var service = CriarService(repository, cartoes, 5, contas, despesas, historicos);

        var resultado = await service.EfetivarAsync(5, new EfetivarFaturaCartaoRequest(
            DataEfetivacao: DataHoraBrasil.Agora(),
            ContaBancariaId: 1,
            ValorTotal: 100m,
            ValorEfetivacao: 100m));

        Assert.Equal("efetivada", resultado.Status);
    }

    [Fact]
    public async Task NaoDeveGerarDespesa_QuandoValorTotalDaFaturaForZero()
    {
        var competencia = DataHoraBrasil.Agora().ToString("yyyy-MM");
        var repository = new FaturaCartaoRepositoryFake
        {
            Faturas =
            [
                new FaturaCartao
                {
                    Id = 6,
                    CartaoId = 40,
                    Competencia = competencia,
                    Status = StatusFaturaCartao.Fechada,
                    ValorTotal = 0m,
                    UsuarioCadastroId = 5
                }
            ]
        };
        var cartoes = new CartaoRepositoryFake { Cartoes = [new Cartao { Id = 40, Tipo = TipoCartao.Credito, SaldoDisponivel = 100m }] };
        var contas = new ContaBancariaRepositoryFake();
        var despesas = new DespesaRepositoryFake();
        var historicos = new HistoricoTransacaoFinanceiraRepositoryFake(contas, cartoes);
        var service = CriarService(repository, cartoes, 5, contas, despesas, historicos);

        var resultado = await service.EfetivarAsync(6, new EfetivarFaturaCartaoRequest(
            DataEfetivacao: DataHoraBrasil.Agora(),
            ContaBancariaId: 0,
            ValorTotal: 0m,
            ValorEfetivacao: 0m));

        Assert.Equal("efetivada", resultado.Status);
        Assert.Empty(despesas.Despesas);
        Assert.Equal(100m, cartoes.Cartoes.Single().SaldoDisponivel);
    }

    [Fact]
    public async Task DeveEstornarFaturaEfetivada_QuandoSolicitadoPorEstornoDeTransacaoVinculada()
    {
        var competencia = DataHoraBrasil.Agora().ToString("yyyy-MM");
        var faturas = new FaturaCartaoRepositoryFake
        {
            Faturas =
            [
                new FaturaCartao
                {
                    Id = 8,
                    CartaoId = 50,
                    Competencia = competencia,
                    Status = StatusFaturaCartao.Efetivada,
                    ValorTotal = 500m,
                    UsuarioCadastroId = 5,
                    DataEfetivacao = DataHoraBrasil.Hoje(),
                    DespesaPagamentoId = 300
                }
            ]
        };
        var cartoes = new CartaoRepositoryFake
        {
            Cartoes =
            [
                new Cartao
                {
                    Id = 50,
                    Tipo = TipoCartao.Credito,
                    SaldoDisponivel = 900m
                }
            ]
        };
        var contas = new ContaBancariaRepositoryFake
        {
            Contas =
            [
                new ContaBancaria
                {
                    Id = 15,
                    UsuarioCadastroId = 5,
                    SaldoAtual = 700m
                }
            ]
        };
        var despesas = new DespesaRepositoryFake
        {
            Despesas =
            [
                new Despesa
                {
                    Id = 300,
                    UsuarioCadastroId = 5,
                    Descricao = "Pagamento de fatura cartao",
                    Competencia = competencia,
                    DataLancamento = DataHoraBrasil.Agora(),
                    TipoPagamento = TipoPagamento.Transferencia,
                    ValorTotal = 500m,
                    ValorLiquido = 500m,
                    ValorEfetivacao = 500m,
                    Status = StatusDespesa.Efetivada,
                    ContaBancariaId = 15
                }
            ]
        };
        var historicos = new HistoricoTransacaoFinanceiraRepositoryFake(contas, cartoes);
        var service = CriarService(faturas, cartoes, 5, contas, despesas, historicos);

        await service.GarantirFaturaEstornadaParaEstornoTransacaoAsync(8, DataHoraBrasil.Hoje(), true, "estorno de transacao");

        var fatura = Assert.Single(faturas.Faturas);
        Assert.Equal(StatusFaturaCartao.Estornada, fatura.Status);
        Assert.Null(fatura.DataEfetivacao);
        var despesaPagamento = Assert.Single(despesas.Despesas);
        Assert.Equal(StatusDespesa.Pendente, despesaPagamento.Status);
    }

    private static FaturaCartaoService CriarService(
        IFaturaCartaoRepository faturaRepository,
        ICartaoRepository cartaoRepository,
        int usuarioId,
        IContaBancariaRepository? contaRepository = null,
        IDespesaRepository? despesaRepository = null,
        IHistoricoTransacaoFinanceiraRepository? historicoRepository = null) =>
        new(
            faturaRepository,
            cartaoRepository,
            contaRepository ?? new ContaBancariaRepositoryFake(),
            despesaRepository ?? new DespesaRepositoryFake(),
            new ReceitaRepositoryFake(),
            new ReembolsoRepositoryFake(),
            new UsuarioAutenticadoProviderFake(usuarioId),
            new HistoricoTransacaoFinanceiraService(historicoRepository ?? new HistoricoTransacaoFinanceiraRepositoryFake(new ContaBancariaRepositoryFake(), new CartaoRepositoryFake())));

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
            Task.FromResult(Faturas.FirstOrDefault(x => x.Id == id && x.UsuarioCadastroId == usuarioCadastroId));

        public Task<FaturaCartao?> ObterPorCartaoCompetenciaAsync(long cartaoId, int usuarioCadastroId, string competencia, CancellationToken cancellationToken = default) =>
            Task.FromResult(Faturas.FirstOrDefault(x => x.CartaoId == cartaoId && x.UsuarioCadastroId == usuarioCadastroId && x.Competencia == competencia));

        public Task<FaturaCartao> CriarAsync(FaturaCartao fatura, CancellationToken cancellationToken = default)
        {
            if (fatura.Id <= 0)
                fatura.Id = Faturas.Count == 0 ? 1 : Faturas.Max(x => x.Id) + 1;

            Faturas.Add(fatura);
            return Task.FromResult(fatura);
        }

        public Task<FaturaCartao> AtualizarAsync(FaturaCartao fatura, CancellationToken cancellationToken = default)
        {
            var idx = Faturas.FindIndex(x => x.Id == fatura.Id);
            if (idx >= 0)
                Faturas[idx] = fatura;
            return Task.FromResult(fatura);
        }
    }

    private sealed class CartaoRepositoryFake : ICartaoRepository
    {
        public List<Cartao> Cartoes { get; set; } = [];

        public Task<List<Cartao>> ListarAsync(CancellationToken cancellationToken = default) => Task.FromResult(Cartoes.ToList());

        public Task<List<Cartao>> ListarAsync(int usuarioCadastroId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Cartoes.Where(x => x.UsuarioCadastroId == 0 || x.UsuarioCadastroId == usuarioCadastroId).ToList());

        public Task<Cartao?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Cartoes.FirstOrDefault(x => x.Id == id));

        public Task<Cartao?> ObterPorIdAsync(long id, int usuarioCadastroId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Cartoes.FirstOrDefault(x => x.Id == id && (x.UsuarioCadastroId == 0 || x.UsuarioCadastroId == usuarioCadastroId)));

        public Task<Cartao> CriarAsync(Cartao cartao, CancellationToken cancellationToken = default)
        {
            if (cartao.Id <= 0)
                cartao.Id = Cartoes.Count == 0 ? 1 : Cartoes.Max(x => x.Id) + 1;
            Cartoes.Add(cartao);
            return Task.FromResult(cartao);
        }

        public Task<Cartao> AtualizarAsync(Cartao cartao, CancellationToken cancellationToken = default)
        {
            var idx = Cartoes.FindIndex(x => x.Id == cartao.Id);
            if (idx >= 0)
                Cartoes[idx] = cartao;
            return Task.FromResult(cartao);
        }
    }

    private sealed class ContaBancariaRepositoryFake : IContaBancariaRepository
    {
        public List<ContaBancaria> Contas { get; set; } = [];

        public Task<List<ContaBancaria>> ListarAsync(CancellationToken cancellationToken = default) => Task.FromResult(Contas.ToList());

        public Task<List<ContaBancaria>> ListarAsync(int usuarioCadastroId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Contas.Where(x => x.UsuarioCadastroId == usuarioCadastroId).ToList());

        public Task<ContaBancaria?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Contas.FirstOrDefault(x => x.Id == id));

        public Task<ContaBancaria?> ObterPorIdAsync(long id, int usuarioCadastroId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Contas.FirstOrDefault(x => x.Id == id && x.UsuarioCadastroId == usuarioCadastroId));

        public Task<ContaBancaria> CriarAsync(ContaBancaria conta, CancellationToken cancellationToken = default)
        {
            if (conta.Id <= 0)
                conta.Id = Contas.Count == 0 ? 1 : Contas.Max(x => x.Id) + 1;
            Contas.Add(conta);
            return Task.FromResult(conta);
        }

        public Task<ContaBancaria> AtualizarAsync(ContaBancaria conta, CancellationToken cancellationToken = default)
        {
            var idx = Contas.FindIndex(x => x.Id == conta.Id);
            if (idx >= 0)
                Contas[idx] = conta;
            return Task.FromResult(conta);
        }
    }

    private sealed class DespesaRepositoryFake : IDespesaRepository
    {
        public List<Despesa> Despesas { get; set; } = [];

        public Task<List<Despesa>> ListarAsync(CancellationToken cancellationToken = default) => Task.FromResult(Despesas.ToList());

        public Task<List<Despesa>> ListarAsync(string? filtroId, string? descricao, string? competencia, DateOnly? dataInicio, DateOnly? dataFim, CancellationToken cancellationToken = default) => Task.FromResult(Despesas.ToList());

        public Task<List<Despesa>> ListarPorUsuarioAsync(int usuarioCadastroId, string? filtroId, string? descricao, string? competencia, DateOnly? dataInicio, DateOnly? dataFim, CancellationToken cancellationToken = default) =>
            Task.FromResult(Despesas.Where(x => x.UsuarioCadastroId == usuarioCadastroId).ToList());

        public Task<List<Despesa>> ListarPendentesAprovacaoPorUsuarioAsync(int usuarioCadastroId, CancellationToken cancellationToken = default) => Task.FromResult(new List<Despesa>());

        public Task<List<Despesa>> ListarEspelhosPorOrigemAsync(long despesaOrigemId, CancellationToken cancellationToken = default) => Task.FromResult(new List<Despesa>());

        public Task<List<Despesa>> ObterPorIdsAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken = default) =>
            Task.FromResult(Despesas.Where(x => ids.Contains(x.Id)).ToList());

        public Task<List<Despesa>> ObterPorIdsAsync(IReadOnlyCollection<long> ids, int usuarioCadastroId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Despesas.Where(x => ids.Contains(x.Id) && x.UsuarioCadastroId == usuarioCadastroId).ToList());

        public Task<Despesa?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Despesas.FirstOrDefault(x => x.Id == id));

        public Task<Despesa?> ObterPorIdAsync(long id, int usuarioCadastroId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Despesas.FirstOrDefault(x => x.Id == id && x.UsuarioCadastroId == usuarioCadastroId));

        public Task<Despesa> CriarAsync(Despesa despesa, CancellationToken cancellationToken = default)
        {
            if (despesa.Id <= 0)
                despesa.Id = Despesas.Count == 0 ? 1 : Despesas.Max(x => x.Id) + 1;
            Despesas.Add(despesa);
            return Task.FromResult(despesa);
        }

        public Task<Despesa> AtualizarAsync(Despesa despesa, CancellationToken cancellationToken = default)
        {
            var idx = Despesas.FindIndex(x => x.Id == despesa.Id);
            if (idx >= 0)
                Despesas[idx] = despesa;
            return Task.FromResult(despesa);
        }
    }

    private sealed class HistoricoTransacaoFinanceiraRepositoryFake(
        ContaBancariaRepositoryFake contaRepository,
        CartaoRepositoryFake cartaoRepository) : IHistoricoTransacaoFinanceiraRepository
    {
        private long _proximoId = 1;
        public List<HistoricoTransacaoFinanceira> Historicos { get; } = [];

        public Task<HistoricoTransacaoFinanceira> CriarAsync(HistoricoTransacaoFinanceira historico, CancellationToken cancellationToken = default)
        {
            var valorImpacto = ResolverValorImpacto(historico.TipoTransacao, historico.TipoOperacao, historico.ValorTransacao);
            if (historico.ContaBancariaId.HasValue)
            {
                var conta = contaRepository.Contas.FirstOrDefault(x => x.Id == historico.ContaBancariaId.Value);
                if (conta is not null)
                {
                    historico.ValorAntesTransacao = conta.SaldoAtual;
                    conta.SaldoAtual += valorImpacto;
                    historico.ValorDepoisTransacao = conta.SaldoAtual;
                }
            }
            else if (historico.CartaoId.HasValue)
            {
                var cartao = cartaoRepository.Cartoes.FirstOrDefault(x => x.Id == historico.CartaoId.Value);
                if (cartao is not null)
                {
                    historico.ValorAntesTransacao = cartao.SaldoDisponivel;
                    cartao.SaldoDisponivel += valorImpacto;
                    historico.ValorDepoisTransacao = cartao.SaldoDisponivel;
                }
            }

            historico.Id = _proximoId++;
            historico.ValorTransacao = valorImpacto;
            Historicos.Add(historico);
            return Task.FromResult(historico);
        }

        public Task<HistoricoTransacaoFinanceira?> ObterUltimoPorTransacaoAsync(TipoTransacaoFinanceira tipoTransacao, long transacaoId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Historicos
                .Where(x => x.TipoTransacao == tipoTransacao && x.TransacaoId == transacaoId)
                .OrderByDescending(x => x.Id)
                .FirstOrDefault());

        public Task MarcarOcultoPorTransacaoAsync(TipoTransacaoFinanceira tipoTransacao, long transacaoId, CancellationToken cancellationToken = default)
        {
            foreach (var historico in Historicos.Where(x => x.TipoTransacao == tipoTransacao && x.TransacaoId == transacaoId))
                historico.OcultarDoHistorico = true;
            return Task.CompletedTask;
        }

        public Task<List<HistoricoTransacaoFinanceira>> ListarPorUsuarioAsync(int usuarioOperacaoId, int quantidadeRegistros, OrdemRegistrosHistoricoTransacaoFinanceira ordemRegistros, CancellationToken cancellationToken = default) =>
            Task.FromResult(Historicos.Where(x => x.UsuarioOperacaoId == usuarioOperacaoId).ToList());

        public Task<List<HistoricoTransacaoFinanceira>> ListarPorUsuarioResumoAsync(int usuarioOperacaoId, int? ano, CancellationToken cancellationToken = default) =>
            Task.FromResult(Historicos.Where(x => x.UsuarioOperacaoId == usuarioOperacaoId).ToList());

        public Task<List<HistoricoTransacaoFinanceira>> ListarPorContaBancariaCompetenciaAsync(long contaBancariaId, int usuarioOperacaoId, string? competencia, CancellationToken cancellationToken = default) =>
            Task.FromResult(Historicos.Where(x => x.UsuarioOperacaoId == usuarioOperacaoId && x.ContaBancariaId == contaBancariaId).ToList());

        public Task<List<HistoricoTransacaoFinanceira>> ListarPorCartaoCompetenciaAsync(long cartaoId, int usuarioOperacaoId, string? competencia, CancellationToken cancellationToken = default) =>
            Task.FromResult(Historicos.Where(x => x.UsuarioOperacaoId == usuarioOperacaoId && x.CartaoId == cartaoId).ToList());

        private static decimal ResolverValorImpacto(TipoTransacaoFinanceira tipoTransacao, TipoOperacaoTransacaoFinanceira tipoOperacao, decimal valorTransacao)
        {
            var valorAbsoluto = Math.Abs(valorTransacao);
            return (tipoTransacao, tipoOperacao) switch
            {
                (TipoTransacaoFinanceira.Despesa, TipoOperacaoTransacaoFinanceira.Efetivacao) => -valorAbsoluto,
                (TipoTransacaoFinanceira.Despesa, TipoOperacaoTransacaoFinanceira.Estorno) => valorAbsoluto,
                (TipoTransacaoFinanceira.Receita, TipoOperacaoTransacaoFinanceira.Efetivacao) => valorAbsoluto,
                (TipoTransacaoFinanceira.Receita, TipoOperacaoTransacaoFinanceira.Estorno) => -valorAbsoluto,
                (TipoTransacaoFinanceira.Reembolso, TipoOperacaoTransacaoFinanceira.Efetivacao) => valorAbsoluto,
                (TipoTransacaoFinanceira.Reembolso, TipoOperacaoTransacaoFinanceira.Estorno) => -valorAbsoluto,
                _ => valorTransacao
            };
        }
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
