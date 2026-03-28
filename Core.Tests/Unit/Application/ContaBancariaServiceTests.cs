using Core.Application.DTOs;
using Core.Application.Services.Financeiro;
using Core.Domain.Entities;
using Core.Domain.Enums;
using Core.Domain.Exceptions;
using Core.Domain.Interfaces;

namespace Core.Tests.Unit.Application;

public sealed class ContaBancariaServiceTests
{
    [Fact]
    public async Task DeveExigirUsuarioAutenticado_ParaCriarConta()
    {
        var service = new ContaBancariaService(new ContaBancariaRepositoryFake(), new UsuarioAutenticadoProviderFake(null));

        var ex = await Assert.ThrowsAsync<DomainException>(() => service.CriarAsync(new CriarContaBancariaRequest("Conta", "Banco", "0001", "123", 100m, new DateOnly(2026, 1, 1))));

        Assert.Equal("usuario_nao_autenticado", ex.Message);
    }

    [Fact]
    public async Task DeveValidarCamposObrigatorios_AoCriarConta()
    {
        var service = new ContaBancariaService(new ContaBancariaRepositoryFake(), new UsuarioAutenticadoProviderFake(1));

        var ex = await Assert.ThrowsAsync<DomainException>(() => service.CriarAsync(new CriarContaBancariaRequest("", "Banco", "0001", "123", 100m, new DateOnly(2026, 1, 1))));

        Assert.Equal("campo_obrigatorio", ex.Message);
    }

    [Fact]
    public async Task DeveValidarSaldoInicial_AoCriarConta()
    {
        var service = new ContaBancariaService(new ContaBancariaRepositoryFake(), new UsuarioAutenticadoProviderFake(1));

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
        var service = new ContaBancariaService(repository, new UsuarioAutenticadoProviderFake(1));

        var ex = await Assert.ThrowsAsync<DomainException>(() => service.InativarAsync(1, new AlternarStatusContaBancariaRequest(1)));

        Assert.Equal("conta_com_pendencias", ex.Message);
    }

    [Fact]
    public async Task DeveRetornarErro_QuandoContaNaoForEncontrada()
    {
        var service = new ContaBancariaService(new ContaBancariaRepositoryFake(), new UsuarioAutenticadoProviderFake(1));

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => service.ObterAsync(99));

        Assert.Equal("conta_bancaria_nao_encontrada", ex.Message);
    }

    private sealed class ContaBancariaRepositoryFake : IContaBancariaRepository
    {
        public ContaBancaria? Conta { get; set; }

        public Task<List<ContaBancaria>> ListarAsync(CancellationToken cancellationToken = default) => Task.FromResult(new List<ContaBancaria>());
        public Task<ContaBancaria?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default) => Task.FromResult(Conta);
        public Task<ContaBancaria> CriarAsync(ContaBancaria conta, CancellationToken cancellationToken = default) => Task.FromResult(conta);
        public Task<ContaBancaria> AtualizarAsync(ContaBancaria conta, CancellationToken cancellationToken = default) => Task.FromResult(conta);
    }

    private sealed class UsuarioAutenticadoProviderFake(int? usuarioId) : IUsuarioAutenticadoProvider
    {
        public int? ObterUsuarioId() => usuarioId;
    }
}
