using Core.Application.DTOs;
using Core.Application.Services.Financeiro;
using Core.Domain.Entities;
using Core.Domain.Entities.Financeiro;
using Core.Domain.Enums;
using Core.Domain.Exceptions;
using Core.Domain.Interfaces;
using Core.Domain.Interfaces.Financeiro;

namespace Core.Tests.Unit.Application;

public sealed class ContaBancariaServiceTests
{
    [Fact]
    public async Task DeveListarContasFiltrandoPeloUsuarioAutenticado()
    {
        var repository = new ContaBancariaRepositoryFake();
        var service = new ContaBancariaService(repository, new HistoricoRepositoryFake(), new UsuarioAutenticadoProviderFake(7));

        await service.ListarAsync();

        Assert.Equal(7, repository.UltimoUsuarioIdConsulta);
    }

    [Fact]
    public async Task DeveExigirUsuarioAutenticado_ParaCriarConta()
    {
        var service = new ContaBancariaService(new ContaBancariaRepositoryFake(), new HistoricoRepositoryFake(), new UsuarioAutenticadoProviderFake(null));

        var ex = await Assert.ThrowsAsync<DomainException>(() => service.CriarAsync(new CriarContaBancariaRequest("Conta", "Banco", "0001", "123", 100m, new DateOnly(2026, 1, 1))));

        Assert.Equal("usuario_nao_autenticado", ex.Message);
    }

    [Fact]
    public async Task DeveValidarCamposObrigatorios_AoCriarConta()
    {
        var service = new ContaBancariaService(new ContaBancariaRepositoryFake(), new HistoricoRepositoryFake(), new UsuarioAutenticadoProviderFake(1));

        var ex = await Assert.ThrowsAsync<DomainException>(() => service.CriarAsync(new CriarContaBancariaRequest("", "Banco", "0001", "123", 100m, new DateOnly(2026, 1, 1))));

        Assert.Equal("campo_obrigatorio", ex.Message);
    }

    [Fact]
    public async Task DeveValidarSaldoInicial_AoCriarConta()
    {
        var service = new ContaBancariaService(new ContaBancariaRepositoryFake(), new HistoricoRepositoryFake(), new UsuarioAutenticadoProviderFake(1));

        var ex = await Assert.ThrowsAsync<DomainException>(() => service.CriarAsync(new CriarContaBancariaRequest("Conta", "Banco", "0001", "123", 0m, new DateOnly(2026, 1, 1))));

        Assert.Equal("saldo_inicial_invalido", ex.Message);
    }

    [Fact]
    public async Task DeveImpedirInativacao_QuandoExistiremPendencias()
    {
        var repository = new ContaBancariaRepositoryFake
        {
            Conta = new ContaBancaria { Id = 1, Descricao = "Conta", Banco = "Banco", Agencia = "0001", Numero = "123", SaldoInicial = 100m, SaldoAtual = 100m, DataAbertura = new DateOnly(2026, 1, 1), Status = StatusContaBancaria.Ativa }
        };
        var service = new ContaBancariaService(repository, new HistoricoRepositoryFake(), new UsuarioAutenticadoProviderFake(1));

        var ex = await Assert.ThrowsAsync<DomainException>(() => service.InativarAsync(1, new AlternarStatusContaBancariaRequest(1)));

        Assert.Equal("conta_com_pendencias", ex.Message);
    }

    [Fact]
    public async Task DeveRetornarErro_QuandoContaNaoForEncontrada()
    {
        var service = new ContaBancariaService(new ContaBancariaRepositoryFake(), new HistoricoRepositoryFake(), new UsuarioAutenticadoProviderFake(1));

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => service.ObterAsync(99));

        Assert.Equal("conta_bancaria_nao_encontrada", ex.Message);
    }

    [Fact]
    public async Task DeveListarLancamentosVinculadosPorCompetencia()
    {
        var repository = new ContaBancariaRepositoryFake
        {
            Conta = new ContaBancaria { Id = 1, Descricao = "Conta", Banco = "Banco", Agencia = "0001", Numero = "123", SaldoInicial = 100m, SaldoAtual = 100m, DataAbertura = new DateOnly(2026, 1, 1), Status = StatusContaBancaria.Ativa }
        };
        var historicoRepository = new HistoricoRepositoryFake
        {
            Historicos = [new HistoricoTransacaoFinanceira { Id = 10, TransacaoId = 99, DataTransacao = new DateOnly(2026, 4, 12), Descricao = "Efetivacao", TipoTransacao = TipoTransacaoFinanceira.Despesa, TipoOperacao = TipoOperacaoTransacaoFinanceira.Efetivacao, ValorAntesTransacao = 100m, ValorTransacao = 50m, ValorDepoisTransacao = 50m }]
        };
        var service = new ContaBancariaService(repository, historicoRepository, new UsuarioAutenticadoProviderFake(7));

        var resultado = await service.ListarLancamentosAsync(1, "04/2026");

        var item = Assert.Single(resultado);
        Assert.Equal(10, item.Id);
        Assert.Equal(1, historicoRepository.UltimaContaBancariaIdConsulta);
        Assert.Equal(7, historicoRepository.UltimoUsuarioIdConsulta);
        Assert.Equal("04/2026", historicoRepository.UltimaCompetenciaConsulta);
    }

    private sealed class ContaBancariaRepositoryFake : IContaBancariaRepository
    {
        public ContaBancaria? Conta { get; set; }
        public int? UltimoUsuarioIdConsulta { get; private set; }

        public Task<List<ContaBancaria>> ListarAsync(CancellationToken cancellationToken = default) => Task.FromResult(new List<ContaBancaria>());
        public Task<List<ContaBancaria>> ListarAsync(int usuarioCadastroId, CancellationToken cancellationToken = default)
        {
            UltimoUsuarioIdConsulta = usuarioCadastroId;
            return ListarAsync(cancellationToken);
        }
        public Task<ContaBancaria?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default) => Task.FromResult(Conta);
        public Task<ContaBancaria?> ObterPorIdAsync(long id, int usuarioCadastroId, CancellationToken cancellationToken = default)
        {
            UltimoUsuarioIdConsulta = usuarioCadastroId;
            return ObterPorIdAsync(id, cancellationToken);
        }
        public Task<ContaBancaria> CriarAsync(ContaBancaria conta, CancellationToken cancellationToken = default) => Task.FromResult(conta);
        public Task<ContaBancaria> AtualizarAsync(ContaBancaria conta, CancellationToken cancellationToken = default) => Task.FromResult(conta);
    }

    private sealed class UsuarioAutenticadoProviderFake(int? usuarioId) : IUsuarioAutenticadoProvider
    {
        public int? ObterUsuarioId() => usuarioId;
    }

    private sealed class HistoricoRepositoryFake : IHistoricoTransacaoFinanceiraRepository
    {
        public List<HistoricoTransacaoFinanceira> Historicos { get; set; } = [];
        public long? UltimaContaBancariaIdConsulta { get; private set; }
        public int? UltimoUsuarioIdConsulta { get; private set; }
        public string? UltimaCompetenciaConsulta { get; private set; }

        public Task<HistoricoTransacaoFinanceira> CriarAsync(HistoricoTransacaoFinanceira historico, CancellationToken cancellationToken = default) =>
            Task.FromResult(historico);

        public Task<HistoricoTransacaoFinanceira?> ObterUltimoPorTransacaoAsync(TipoTransacaoFinanceira tipoTransacao, long transacaoId, CancellationToken cancellationToken = default) =>
            Task.FromResult<HistoricoTransacaoFinanceira?>(null);

        public Task<List<HistoricoTransacaoFinanceira>> ListarPorUsuarioAsync(int usuarioOperacaoId, int quantidadeRegistros, OrdemRegistrosHistoricoTransacaoFinanceira ordemRegistros, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<HistoricoTransacaoFinanceira>());

        public Task<List<HistoricoTransacaoFinanceira>> ListarPorContaBancariaCompetenciaAsync(long contaBancariaId, int usuarioOperacaoId, string? competencia, CancellationToken cancellationToken = default)
        {
            UltimaContaBancariaIdConsulta = contaBancariaId;
            UltimoUsuarioIdConsulta = usuarioOperacaoId;
            UltimaCompetenciaConsulta = competencia;
            return Task.FromResult(Historicos);
        }

        public Task<List<HistoricoTransacaoFinanceira>> ListarPorCartaoCompetenciaAsync(long cartaoId, int usuarioOperacaoId, string? competencia, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<HistoricoTransacaoFinanceira>());
    }
}
