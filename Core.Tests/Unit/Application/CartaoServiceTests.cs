using Core.Application.DTOs;
using Core.Application.Services.Financeiro;
using Core.Domain.Entities;
using Core.Domain.Entities.Financeiro;
using Core.Domain.Enums;
using Core.Domain.Exceptions;
using Core.Domain.Interfaces;
using Core.Domain.Interfaces.Financeiro;

namespace Core.Tests.Unit.Application;

public sealed class CartaoServiceTests
{
    [Fact]
    public async Task DeveListarCartoesFiltrandoPeloUsuarioAutenticado()
    {
        var repository = new CartaoRepositoryFake();
        var service = new CartaoService(repository, new HistoricoRepositoryFake(), new UsuarioAutenticadoProviderFake(5));

        await service.ListarAsync();

        Assert.Equal(5, repository.UltimoUsuarioIdConsulta);
    }

    [Fact]
    public async Task DeveExigirUsuarioAutenticado_ParaCriarCartao()
    {
        var service = new CartaoService(new CartaoRepositoryFake(), new HistoricoRepositoryFake(), new UsuarioAutenticadoProviderFake(null));

        var ex = await Assert.ThrowsAsync<DomainException>(() => service.CriarAsync(new CriarCartaoRequest("Cartao", "Visa", TipoCartao.Debito, null, 100m, null, null)));

        Assert.Equal("usuario_nao_autenticado", ex.Message);
    }

    [Fact]
    public async Task DeveValidarCamposObrigatorios_AoCriarCartao()
    {
        var service = new CartaoService(new CartaoRepositoryFake(), new HistoricoRepositoryFake(), new UsuarioAutenticadoProviderFake(1));

        var ex = await Assert.ThrowsAsync<DomainException>(() => service.CriarAsync(new CriarCartaoRequest("", "Visa", TipoCartao.Debito, null, 100m, null, null)));

        Assert.Equal("campo_obrigatorio", ex.Message);
    }

    [Fact]
    public async Task DeveExigirDadosDeCredito_QuandoTipoForCredito()
    {
        var service = new CartaoService(new CartaoRepositoryFake(), new HistoricoRepositoryFake(), new UsuarioAutenticadoProviderFake(1));

        var ex = await Assert.ThrowsAsync<DomainException>(() => service.CriarAsync(new CriarCartaoRequest("Cartao", "Visa", TipoCartao.Credito, null, 100m, null, null)));

        Assert.Equal("dados_credito_obrigatorios", ex.Message);
    }

    [Fact]
    public async Task DeveImpedirInativacao_QuandoExistiremPendencias()
    {
        var repository = new CartaoRepositoryFake
        {
            Cartao = new Cartao { Id = 1, Descricao = "Cartao", Bandeira = "Visa", Tipo = TipoCartao.Debito, SaldoDisponivel = 100m, Status = StatusCartao.Ativo }
        };
        var service = new CartaoService(repository, new HistoricoRepositoryFake(), new UsuarioAutenticadoProviderFake(1));

        var ex = await Assert.ThrowsAsync<DomainException>(() => service.InativarAsync(1, new AlternarStatusCartaoRequest(2)));

        Assert.Equal("cartao_com_pendencias", ex.Message);
    }

    [Fact]
    public async Task DeveRetornarErro_QuandoCartaoNaoForEncontrado()
    {
        var service = new CartaoService(new CartaoRepositoryFake(), new HistoricoRepositoryFake(), new UsuarioAutenticadoProviderFake(1));

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => service.ObterAsync(99));

        Assert.Equal("cartao_nao_encontrado", ex.Message);
    }

    [Fact]
    public async Task DeveListarLancamentosVinculadosDoCartaoPorCompetencia()
    {
        var repository = new CartaoRepositoryFake
        {
            Cartao = new Cartao { Id = 2, Descricao = "Cartao", Bandeira = "Visa", Tipo = TipoCartao.Credito, Limite = 1000m, SaldoDisponivel = 700m, Status = StatusCartao.Ativo }
        };
        var historicoRepository = new HistoricoRepositoryFake
        {
            Historicos = [new HistoricoTransacaoFinanceira { Id = 33, TransacaoId = 120, DataTransacao = new DateOnly(2026, 4, 20), Descricao = "Efetivacao no cartao", TipoTransacao = TipoTransacaoFinanceira.Receita, TipoOperacao = TipoOperacaoTransacaoFinanceira.Efetivacao, ValorAntesTransacao = 10m, ValorTransacao = 15m, ValorDepoisTransacao = 25m }]
        };
        var service = new CartaoService(repository, historicoRepository, new UsuarioAutenticadoProviderFake(5));

        var resultado = await service.ListarLancamentosAsync(2, "04/2026");

        var item = Assert.Single(resultado);
        Assert.Equal(33, item.Id);
        Assert.Equal(2, historicoRepository.UltimoCartaoIdConsulta);
        Assert.Equal(5, historicoRepository.UltimoUsuarioIdConsulta);
        Assert.Equal("04/2026", historicoRepository.UltimaCompetenciaConsulta);
    }

    private sealed class CartaoRepositoryFake : ICartaoRepository
    {
        public Cartao? Cartao { get; set; }
        public int? UltimoUsuarioIdConsulta { get; private set; }

        public Task<List<Cartao>> ListarAsync(CancellationToken cancellationToken = default) => Task.FromResult(new List<Cartao>());
        public Task<List<Cartao>> ListarAsync(int usuarioCadastroId, CancellationToken cancellationToken = default)
        {
            UltimoUsuarioIdConsulta = usuarioCadastroId;
            return ListarAsync(cancellationToken);
        }
        public Task<Cartao?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default) => Task.FromResult(Cartao);
        public Task<Cartao?> ObterPorIdAsync(long id, int usuarioCadastroId, CancellationToken cancellationToken = default)
        {
            UltimoUsuarioIdConsulta = usuarioCadastroId;
            return ObterPorIdAsync(id, cancellationToken);
        }
        public Task<Cartao> CriarAsync(Cartao cartao, CancellationToken cancellationToken = default) => Task.FromResult(cartao);
        public Task<Cartao> AtualizarAsync(Cartao cartao, CancellationToken cancellationToken = default) => Task.FromResult(cartao);
    }

    private sealed class UsuarioAutenticadoProviderFake(int? usuarioId) : IUsuarioAutenticadoProvider
    {
        public int? ObterUsuarioId() => usuarioId;
    }

    private sealed class HistoricoRepositoryFake : IHistoricoTransacaoFinanceiraRepository
    {
        public List<HistoricoTransacaoFinanceira> Historicos { get; set; } = [];
        public long? UltimoCartaoIdConsulta { get; private set; }
        public int? UltimoUsuarioIdConsulta { get; private set; }
        public string? UltimaCompetenciaConsulta { get; private set; }

        public Task<HistoricoTransacaoFinanceira> CriarAsync(HistoricoTransacaoFinanceira historico, CancellationToken cancellationToken = default) =>
            Task.FromResult(historico);

        public Task<HistoricoTransacaoFinanceira?> ObterUltimoPorTransacaoAsync(TipoTransacaoFinanceira tipoTransacao, long transacaoId, CancellationToken cancellationToken = default) =>
            Task.FromResult<HistoricoTransacaoFinanceira?>(null);

        public Task<List<HistoricoTransacaoFinanceira>> ListarPorContaBancariaCompetenciaAsync(long contaBancariaId, int usuarioOperacaoId, string? competencia, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<HistoricoTransacaoFinanceira>());

        public Task<List<HistoricoTransacaoFinanceira>> ListarPorCartaoCompetenciaAsync(long cartaoId, int usuarioOperacaoId, string? competencia, CancellationToken cancellationToken = default)
        {
            UltimoCartaoIdConsulta = cartaoId;
            UltimoUsuarioIdConsulta = usuarioOperacaoId;
            UltimaCompetenciaConsulta = competencia;
            return Task.FromResult(Historicos);
        }
    }
}
