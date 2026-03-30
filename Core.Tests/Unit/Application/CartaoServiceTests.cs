using Core.Application.DTOs;
using Core.Application.Services.Financeiro;
using Core.Domain.Entities;
using Core.Domain.Enums;
using Core.Domain.Exceptions;
using Core.Domain.Interfaces;

namespace Core.Tests.Unit.Application;

public sealed class CartaoServiceTests
{
    [Fact]
    public async Task DeveListarCartoesFiltrandoPeloUsuarioAutenticado()
    {
        var repository = new CartaoRepositoryFake();
        var service = new CartaoService(repository, new UsuarioAutenticadoProviderFake(5));

        await service.ListarAsync();

        Assert.Equal(5, repository.UltimoUsuarioIdConsulta);
    }

    [Fact]
    public async Task DeveExigirUsuarioAutenticado_ParaCriarCartao()
    {
        var service = new CartaoService(new CartaoRepositoryFake(), new UsuarioAutenticadoProviderFake(null));

        var ex = await Assert.ThrowsAsync<DomainException>(() => service.CriarAsync(new CriarCartaoRequest("Cartao", "Visa", TipoCartao.Debito, null, 100m, null, null)));

        Assert.Equal("usuario_nao_autenticado", ex.Message);
    }

    [Fact]
    public async Task DeveValidarCamposObrigatorios_AoCriarCartao()
    {
        var service = new CartaoService(new CartaoRepositoryFake(), new UsuarioAutenticadoProviderFake(1));

        var ex = await Assert.ThrowsAsync<DomainException>(() => service.CriarAsync(new CriarCartaoRequest("", "Visa", TipoCartao.Debito, null, 100m, null, null)));

        Assert.Equal("campo_obrigatorio", ex.Message);
    }

    [Fact]
    public async Task DeveExigirDadosDeCredito_QuandoTipoForCredito()
    {
        var service = new CartaoService(new CartaoRepositoryFake(), new UsuarioAutenticadoProviderFake(1));

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
        var service = new CartaoService(repository, new UsuarioAutenticadoProviderFake(1));

        var ex = await Assert.ThrowsAsync<DomainException>(() => service.InativarAsync(1, new AlternarStatusCartaoRequest(2)));

        Assert.Equal("cartao_com_pendencias", ex.Message);
    }

    [Fact]
    public async Task DeveRetornarErro_QuandoCartaoNaoForEncontrado()
    {
        var service = new CartaoService(new CartaoRepositoryFake(), new UsuarioAutenticadoProviderFake(1));

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => service.ObterAsync(99));

        Assert.Equal("cartao_nao_encontrado", ex.Message);
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
}
