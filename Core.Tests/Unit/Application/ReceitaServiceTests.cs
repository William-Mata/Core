using Core.Application.DTOs.Financeiro;
using Core.Application.Contracts.Financeiro;
using Core.Application.Services.Financeiro;
using Core.Domain.Entities;
using Core.Domain.Entities.Administracao;
using Core.Domain.Entities.Financeiro;
using Core.Domain.Enums;
using Core.Domain.Exceptions;
using Core.Domain.Interfaces;
using Core.Domain.Interfaces.Administracao;
using Core.Domain.Interfaces.Financeiro;

namespace Core.Tests.Unit.Application;

public sealed class ReceitaServiceTests
{
    [Fact]
    public async Task DeveListarPorCompetencia_QuandoParametroForInformado()
    {
        var repository = new ReceitaRepoFake();
        var service = CriarService(repository, new ContaRepoFake(), new AreaRepoFake(), 99);

        await service.ListarAsync(new ListarReceitasRequest("10", "Salario", "04/2026", null, null, false));

        Assert.Equal(99, repository.UltimoUsuarioIdFiltro);
        Assert.Equal("10", repository.UltimoIdFiltro);
        Assert.Equal("Salario", repository.UltimaDescricaoFiltro);
        Assert.Equal("04/2026", repository.UltimaCompetenciaFiltro);
        Assert.Null(repository.UltimaDataInicioFiltro);
        Assert.Null(repository.UltimaDataFimFiltro);
    }

    [Fact]
    public async Task DeveListarPelaCompetenciaAtual_QuandoNenhumFiltroForInformado()
    {
        var repository = new ReceitaRepoFake();
        var service = CriarService(repository, new ContaRepoFake(), new AreaRepoFake(), 99);

        await service.ListarAsync(new ListarReceitasRequest(null, null, null, null, null, false));

        var hoje = DateOnly.FromDateTime(DateTime.Today);
        Assert.Equal(new DateOnly(hoje.Year, hoje.Month, 1), repository.UltimaDataInicioFiltro);
        Assert.Equal(new DateOnly(hoje.Year, hoje.Month, DateTime.DaysInMonth(hoje.Year, hoje.Month)), repository.UltimaDataFimFiltro);
    }

    [Fact]
    public async Task DeveConsiderarCompetenciaEPeriodo_QuandoAmbosForemInformados()
    {
        var repository = new ReceitaRepoFake();
        var service = CriarService(repository, new ContaRepoFake(), new AreaRepoFake(), 99);

        await service.ListarAsync(new ListarReceitasRequest(null, null, "2026/04", new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31), false));

        Assert.Equal("2026/04", repository.UltimaCompetenciaFiltro);
        Assert.Equal(new DateOnly(2026, 1, 1), repository.UltimaDataInicioFiltro);
        Assert.Equal(new DateOnly(2026, 1, 31), repository.UltimaDataFimFiltro);
    }

    [Fact]
    public async Task DevePublicarMais10Recorrencias_AoListarCompetenciaComUltimaOcorrencia()
    {
        var publisher = new RecorrenciaPublisherFake();
        var repository = new ReceitaRepoFake
        {
            ReceitasListadas =
            [
                new Receita
                {
                    Id = 200,
                    UsuarioCadastroId = 99,
                    Descricao = "Freelance fixo",
                    DataLancamento = new DateOnly(2026, 4, 5),
                    DataVencimento = new DateOnly(2026, 4, 5),
                    TipoReceita = "freelance",
                    TipoRecebimento = "dinheiro",
                    Recorrencia = Recorrencia.Mensal,
                    QuantidadeRecorrencia = 2,
                    ValorTotal = 500m,
                    ValorLiquido = 500m,
                    Status = StatusReceita.Pendente
                },
                new Receita
                {
                    Id = 201,
                    UsuarioCadastroId = 99,
                    Descricao = "Freelance fixo",
                    DataLancamento = new DateOnly(2026, 5, 5),
                    DataVencimento = new DateOnly(2026, 5, 5),
                    TipoReceita = "freelance",
                    TipoRecebimento = "dinheiro",
                    Recorrencia = Recorrencia.Mensal,
                    QuantidadeRecorrencia = 2,
                    ValorTotal = 500m,
                    ValorLiquido = 500m,
                    Status = StatusReceita.Pendente
                }
            ]
        };
        var service = CriarService(repository, new ContaRepoFake(), new AreaRepoFake(), publisher, 99);

        await service.ListarAsync(new ListarReceitasRequest(null, null, "05/2026", null, null, true));

        Assert.NotNull(publisher.ReceitaMessage);
        Assert.Equal(12, publisher.ReceitaMessage!.QuantidadeRecorrencia);
        Assert.Equal(new DateOnly(2026, 4, 5), publisher.ReceitaMessage.DataLancamento);
    }

    [Fact]
    public async Task NaoDevePublicarMais10Recorrencias_QuandoJaExistirProximaOcorrencia()
    {
        var publisher = new RecorrenciaPublisherFake();
        var repository = new ReceitaRepoFake
        {
            ReceitasListadas =
            [
                new Receita
                {
                    Id = 200,
                    UsuarioCadastroId = 99,
                    Descricao = "Freelance fixo",
                    DataLancamento = new DateOnly(2026, 4, 5),
                    DataVencimento = new DateOnly(2026, 4, 5),
                    TipoReceita = "freelance",
                    TipoRecebimento = "dinheiro",
                    Recorrencia = Recorrencia.Mensal,
                    QuantidadeRecorrencia = 2,
                    ValorTotal = 500m,
                    ValorLiquido = 500m,
                    Status = StatusReceita.Pendente
                },
                new Receita
                {
                    Id = 201,
                    UsuarioCadastroId = 99,
                    Descricao = "Freelance fixo",
                    DataLancamento = new DateOnly(2026, 5, 5),
                    DataVencimento = new DateOnly(2026, 5, 5),
                    TipoReceita = "freelance",
                    TipoRecebimento = "dinheiro",
                    Recorrencia = Recorrencia.Mensal,
                    QuantidadeRecorrencia = 2,
                    ValorTotal = 500m,
                    ValorLiquido = 500m,
                    Status = StatusReceita.Pendente
                }
            ]
        };
        var service = CriarService(repository, new ContaRepoFake(), new AreaRepoFake(), publisher, 99);

        await service.ListarAsync(new ListarReceitasRequest(null, null, "04/2026", null, null, true));

        Assert.Null(publisher.ReceitaMessage);
    }

    [Fact]
    public async Task DeveExigirUsuarioAutenticado_ParaCriarReceita()
    {
        var service = CriarService(new ReceitaRepoFake(), new ContaRepoFake(), new AreaRepoFake(), null);

        var request = CriarRequestPadrao();
        var ex = await Assert.ThrowsAsync<DomainException>(() => service.CriarAsync(request));

        Assert.Equal("usuario_nao_autenticado", ex.Message);
    }

    [Fact]
    public async Task DeveExigirContaBancaria_QuandoTipoRecebimentoForPix()
    {
        var service = CriarService(new ReceitaRepoFake(), new ContaRepoFake(), new AreaRepoFake(), 99);

        var request = CriarRequestPadrao(tipoRecebimento: "pix", contaBancaria: null);
        var ex = await Assert.ThrowsAsync<DomainException>(() => service.CriarAsync(request));

        Assert.Equal("conta_bancaria_obrigatoria", ex.Message);
    }

    [Fact]
    public async Task DeveValidarContaBancariaInformada()
    {
        var service = CriarService(new ReceitaRepoFake(), new ContaRepoFake(), new AreaRepoFake(), 99);

        var request = CriarRequestPadrao(tipoRecebimento: "dinheiro", contaBancaria: "Conta Inexistente");
        var ex = await Assert.ThrowsAsync<DomainException>(() => service.CriarAsync(request));

        Assert.Equal("conta_bancaria_invalida", ex.Message);
    }

    [Fact]
    public async Task DeveValidarRelacaoEntreAreaESubArea()
    {
        var areaRepository = new AreaRepoFake
        {
            SubAreas = [new SubArea { Id = 2, AreaId = 99, Nome = "SubArea" }]
        };
        var service = CriarService(new ReceitaRepoFake(), new ContaRepoFake(), areaRepository, 99);

        var request = CriarRequestPadrao(areasRateio: [new ReceitaAreaRateioRequest(1, 2, 100m)]);
        var ex = await Assert.ThrowsAsync<DomainException>(() => service.CriarAsync(request));

        Assert.Equal("relacao_area_subarea_invalida", ex.Message);
    }

    [Fact]
    public async Task DeveRejeitarRateioDeAmigosQuandoSomaForDiferenteDoValorTotal()
    {
        var service = CriarService(new ReceitaRepoFake(), new ContaRepoFake(), new AreaRepoFake(), 99);

        var request = CriarRequestPadrao(amigos: [new AmigoRateioRequest(2, 600m), new AmigoRateioRequest(3, 300m)]);
        var ex = await Assert.ThrowsAsync<DomainException>(() => service.CriarAsync(request));

        Assert.Equal("rateio_amigos_invalido", ex.Message);
    }

    [Fact]
    public async Task DeveRejeitarRateioDeAreaQuandoSomaForDiferenteDoValorTotal()
    {
        var areaRepository = CriarAreaRepoValida(TipoAreaFinanceira.Receita);
        var service = CriarService(new ReceitaRepoFake(), new ContaRepoFake(), areaRepository, 99);

        var request = CriarRequestPadrao(areasRateio: [new ReceitaAreaRateioRequest(1, 2, 600m)]);
        var ex = await Assert.ThrowsAsync<DomainException>(() => service.CriarAsync(request));

        Assert.Equal("rateio_area_invalido", ex.Message);
    }

    [Fact]
    public async Task DevePermitirCriacao_QuandoRateiosSomamValorTotal()
    {
        var areaRepository = CriarAreaRepoValida(TipoAreaFinanceira.Receita);
        var service = CriarService(new ReceitaRepoFake(), new ContaRepoFake(), areaRepository, 99);

        var result = await service.CriarAsync(
            CriarRequestPadrao(
                amigos: [new AmigoRateioRequest(2, 400m), new AmigoRateioRequest(3, 600m)],
                areasRateio: [new ReceitaAreaRateioRequest(1, 2, 1000m)]));

        Assert.Equal("Freelance", result.Descricao);
        Assert.Equal(1000m, result.ValorTotal);
        Assert.Equal(2, result.AmigosRateio.Count);
        Assert.Single(result.AreasSubAreasRateio);
    }

    [Fact]
    public async Task DeveRejeitarRateioQuandoAmigoNaoForAceito()
    {
        var areaRepository = CriarAreaRepoValida(TipoAreaFinanceira.Receita);
        var service = new ReceitaService(
            new ReceitaRepoFake(),
            new ContaRepoFake(),
            new CartaoRepoFake(),
            areaRepository,
            new AmizadeRepositoryFake { AmigosAceitosIds = [2] },
            new UsuarioRepositoryFake(),
            new UsuarioAutenticadoProviderFake(99),
            new HistoricoTransacaoFinanceiraService(new HistoricoRepositoryFake()),
            new DocumentoStorageServiceFake(),
            new RecorrenciaPublisherFake());

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            service.CriarAsync(CriarRequestPadrao(amigos: [new AmigoRateioRequest(2, 500m), new AmigoRateioRequest(3, 500m)])));

        Assert.Equal("amigo_rateio_invalido", ex.Message);
    }

    [Fact]
    public async Task DeveCriarEspelhosEmPendenteAprovacao_AoCriarComRateioDeAmigos()
    {
        var repository = new ReceitaRepoFake();
        var areaRepository = CriarAreaRepoValida(TipoAreaFinanceira.Receita);
        var service = CriarService(repository, new ContaRepoFake(), areaRepository, 99);

        await service.CriarAsync(CriarRequestPadrao(
            amigos: [new AmigoRateioRequest(2, 400m), new AmigoRateioRequest(3, 600m)],
            areasRateio: [new ReceitaAreaRateioRequest(1, 2, 1000m)]));

        Assert.Equal(3, repository.ReceitasCriadas.Count);
        Assert.Single(repository.ReceitasCriadas.Where(x => x.ReceitaOrigemId is null));
        Assert.Equal(2, repository.ReceitasCriadas.Count(x => x.Status == StatusReceita.PendenteAprovacao));
    }

    [Fact]
    public async Task DeveAprovarRateioQuandoReceitaEspelhoEstiverPendenteAprovacao()
    {
        var repository = new ReceitaRepoFake
        {
            Receita = new Receita
            {
                Id = 10,
                ReceitaOrigemId = 1,
                UsuarioCadastroId = 99,
                Status = StatusReceita.PendenteAprovacao
            }
        };
        var service = CriarService(repository, new ContaRepoFake(), new AreaRepoFake(), 99);

        var result = await service.AprovarRateioAsync(10);

        Assert.Equal("pendente", result.Status);
    }

    [Fact]
    public async Task DeveRejeitarRateioQuandoReceitaEspelhoEstiverPendenteAprovacao()
    {
        var repository = new ReceitaRepoFake
        {
            Receita = new Receita
            {
                Id = 10,
                ReceitaOrigemId = 1,
                UsuarioCadastroId = 99,
                Status = StatusReceita.PendenteAprovacao
            }
        };
        var service = CriarService(repository, new ContaRepoFake(), new AreaRepoFake(), 99);

        var result = await service.RejeitarRateioAsync(10);

        Assert.Equal("rejeitado", result.Status);
    }

    [Fact]
    public async Task DeveReabrirEspelhoRejeitadoNoMesmoRegistro_AoAtualizarReceita()
    {
        var repository = new ReceitaRepoFake
        {
            Receita = new Receita
            {
                Id = 1,
                UsuarioCadastroId = 99,
                Descricao = "Receita",
                DataLancamento = new DateOnly(2026, 3, 1),
                DataVencimento = new DateOnly(2026, 3, 2),
                TipoReceita = "freelance",
                TipoRecebimento = "dinheiro",
                Recorrencia = Recorrencia.Unica,
                ValorTotal = 1000m,
                ValorLiquido = 1000m,
                Status = StatusReceita.Pendente
            },
            EspelhosPorOrigem =
            [
                new Receita
                {
                    Id = 10,
                    ReceitaOrigemId = 1,
                    UsuarioCadastroId = 2,
                    Descricao = "Espelho",
                    DataLancamento = new DateOnly(2026, 3, 1),
                    DataVencimento = new DateOnly(2026, 3, 2),
                    TipoReceita = "freelance",
                    TipoRecebimento = "dinheiro",
                    Recorrencia = Recorrencia.Unica,
                    ValorTotal = 1000m,
                    ValorLiquido = 1000m,
                    Status = StatusReceita.Rejeitado
                }
            ]
        };
        var service = CriarService(repository, new ContaRepoFake(), CriarAreaRepoValida(TipoAreaFinanceira.Receita), 99);

        await service.AtualizarAsync(1, CriarAtualizacaoPadrao(
            amigos: [new AmigoRateioRequest(2, 1000m)],
            areasRateio: [new ReceitaAreaRateioRequest(1, 2, 1000m)]));

        var espelhoAtualizado = repository.ReceitasAtualizadas.Last(x => x.Id == 10);
        Assert.Equal(StatusReceita.PendenteAprovacao, espelhoAtualizado.Status);
        Assert.DoesNotContain(repository.ReceitasCriadas, x => x.Id != 1 && x.UsuarioCadastroId == 2 && x.ReceitaOrigemId == 1);
    }

    [Fact]
    public async Task DeveCancelarEspelhoQuandoAmigoForRemovido_AoAtualizarReceita()
    {
        var repository = new ReceitaRepoFake
        {
            Receita = new Receita
            {
                Id = 1,
                UsuarioCadastroId = 99,
                Descricao = "Receita",
                DataLancamento = new DateOnly(2026, 3, 1),
                DataVencimento = new DateOnly(2026, 3, 2),
                TipoReceita = "freelance",
                TipoRecebimento = "dinheiro",
                Recorrencia = Recorrencia.Unica,
                ValorTotal = 1000m,
                ValorLiquido = 1000m,
                Status = StatusReceita.Pendente
            },
            EspelhosPorOrigem =
            [
                new Receita
                {
                    Id = 10,
                    ReceitaOrigemId = 1,
                    UsuarioCadastroId = 2,
                    Descricao = "Espelho",
                    DataLancamento = new DateOnly(2026, 3, 1),
                    DataVencimento = new DateOnly(2026, 3, 2),
                    TipoReceita = "freelance",
                    TipoRecebimento = "dinheiro",
                    Recorrencia = Recorrencia.Unica,
                    ValorTotal = 1000m,
                    ValorLiquido = 1000m,
                    Status = StatusReceita.PendenteAprovacao
                }
            ]
        };
        var service = CriarService(repository, new ContaRepoFake(), new AreaRepoFake(), 99);

        await service.AtualizarAsync(1, CriarAtualizacaoPadrao(amigos: [], areasRateio: []));

        var espelhoCancelado = repository.ReceitasAtualizadas.Last(x => x.Id == 10);
        Assert.Equal(StatusReceita.Cancelada, espelhoCancelado.Status);
    }

    [Fact]
    public async Task DeveDistribuirRateioDeAreaProporcionalmenteNoEspelho_ComFechamentoDeSoma()
    {
        var repository = new ReceitaRepoFake();
        var area = new Area { Id = 1, Nome = "Area", Tipo = TipoAreaFinanceira.Receita };
        var areaRepository = new AreaRepoFake
        {
            SubAreas =
            [
                new SubArea { Id = 2, AreaId = 1, Area = area, Nome = "SubArea 1" },
                new SubArea { Id = 3, AreaId = 1, Area = area, Nome = "SubArea 2" }
            ]
        };
        var service = CriarService(repository, new ContaRepoFake(), areaRepository, 99);

        await service.CriarAsync(CriarRequestPadrao(
            amigos:
            [
                new AmigoRateioRequest(2, 333.33m),
                new AmigoRateioRequest(3, 666.67m)
            ],
            areasRateio:
            [
                new ReceitaAreaRateioRequest(1, 2, 700m),
                new ReceitaAreaRateioRequest(1, 3, 300m)
            ]));

        var espelho = repository.ReceitasCriadas.Single(x => x.ReceitaOrigemId.HasValue && x.UsuarioCadastroId == 2);
        var somaAreas = espelho.AreasRateio.Sum(x => x.Valor ?? 0m);
        Assert.Equal(333.33m, somaAreas);
    }

    [Fact]
    public async Task DeveImpedirAprovacaoQuandoUsuarioNaoForDonoDoEspelho()
    {
        var repository = new ReceitaRepoFake
        {
            ValidarUsuarioNoObter = true,
            Receita = new Receita
            {
                Id = 10,
                ReceitaOrigemId = 1,
                UsuarioCadastroId = 2,
                Status = StatusReceita.PendenteAprovacao
            }
        };
        var service = CriarService(repository, new ContaRepoFake(), new AreaRepoFake(), 99);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => service.AprovarRateioAsync(10));

        Assert.Equal("receita_nao_encontrada", ex.Message);
    }

    [Fact]
    public async Task DeveRetornarErro_QuandoReceitaNaoForEncontradaAoEfetivar()
    {
        var service = CriarService(new ReceitaRepoFake(), new ContaRepoFake(), new AreaRepoFake(), 99);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => service.EfetivarAsync(10, new EfetivarReceitaRequest(new DateOnly(2026, 3, 5), "dinheiro", null, 100m, 0m, 0m, 0m, 0m, null)));

        Assert.Equal("receita_nao_encontrada", ex.Message);
    }

    [Fact]
    public async Task DeveImpedirEfetivacao_QuandoDataEfetivacaoForMenorQueDataLancamento()
    {
        var service = CriarService(
            new ReceitaRepoFake
            {
                Receita = new Receita
                {
                    Id = 1,
                    Descricao = "Receita",
                    DataLancamento = new DateOnly(2026, 3, 10),
                    DataVencimento = new DateOnly(2026, 3, 15),
                    TipoReceita = "freelance",
                    TipoRecebimento = "dinheiro",
                    Recorrencia = Recorrencia.Unica,
                    ValorTotal = 100m,
                    ValorLiquido = 100m,
                    Status = StatusReceita.Pendente
                }
            },
            new ContaRepoFake(),
            new AreaRepoFake(),
            99);

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            service.EfetivarAsync(1, new EfetivarReceitaRequest(new DateOnly(2026, 3, 9), "dinheiro", null, 100m, 0m, 0m, 0m, 0m, null)));

        Assert.Equal("periodo_invalido", ex.Message);
    }

    [Fact]
    public async Task DevePermitirEfetivacao_QuandoDataEfetivacaoForIgualDataLancamento()
    {
        var service = CriarService(
            new ReceitaRepoFake
            {
                Receita = new Receita
                {
                    Id = 1,
                    Descricao = "Receita",
                    DataLancamento = new DateOnly(2026, 3, 10),
                    DataVencimento = new DateOnly(2026, 3, 15),
                    TipoReceita = "freelance",
                    TipoRecebimento = "dinheiro",
                    Recorrencia = Recorrencia.Unica,
                    ValorTotal = 100m,
                    ValorLiquido = 100m,
                    Status = StatusReceita.Pendente
                }
            },
            new ContaRepoFake(),
            new AreaRepoFake(),
            99);

        var result = await service.EfetivarAsync(1, new EfetivarReceitaRequest(new DateOnly(2026, 3, 10), "dinheiro", null, 100m, 0m, 0m, 0m, 0m, null));

        Assert.Equal("efetivada", result.Status);
    }

    [Fact]
    public async Task DevePublicarMensagemDeRecorrencia_AoCriarReceitaRecorrente()
    {
        var repository = new ReceitaRepoFake();
        var publisher = new RecorrenciaPublisherFake();
        var service = CriarService(repository, new ContaRepoFake(), new AreaRepoFake(), publisher, 99);

        await service.CriarAsync(CriarRequestPadrao(recorrencia: Recorrencia.Semanal, quantidadeRecorrencia: 2));

        Assert.Single(repository.ReceitasCriadas);
        Assert.NotNull(publisher.ReceitaMessage);
        Assert.Equal(2, publisher.ReceitaMessage!.QuantidadeRecorrencia);
    }

    [Fact]
    public async Task DevePublicarMensagemComAlvo100_AoCriarReceitaComRecorrenciaFixa()
    {
        var repository = new ReceitaRepoFake();
        var publisher = new RecorrenciaPublisherFake();
        var service = CriarService(repository, new ContaRepoFake(), new AreaRepoFake(), publisher, 99);

        await service.CriarAsync(CriarRequestPadrao(recorrencia: Recorrencia.Mensal, quantidadeRecorrencia: null, recorrenciaFixa: true));

        Assert.Single(repository.ReceitasCriadas);
        Assert.NotNull(publisher.ReceitaMessage);
        Assert.True(publisher.ReceitaMessage!.RecorrenciaFixa);
        Assert.Equal(100, publisher.ReceitaMessage!.QuantidadeRecorrencia);
    }

    [Fact]
    public async Task DeveRejeitarRecorrenciaFixaComTipoUnico()
    {
        var service = CriarService(new ReceitaRepoFake(), new ContaRepoFake(), new AreaRepoFake(), 99);

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            service.CriarAsync(CriarRequestPadrao(recorrencia: Recorrencia.Unica, recorrenciaFixa: true)));

        Assert.Equal("recorrencia_fixa_invalida", ex.Message);
    }

    [Fact]
    public async Task DeveRejeitarQuantidadeRecorrenciaMaiorQue100_QuandoNaoFixa()
    {
        var service = CriarService(new ReceitaRepoFake(), new ContaRepoFake(), new AreaRepoFake(), 99);

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            service.CriarAsync(CriarRequestPadrao(recorrencia: Recorrencia.Mensal, quantidadeRecorrencia: 101, recorrenciaFixa: false)));

        Assert.Equal("quantidade_recorrencia_invalida", ex.Message);
    }

    [Fact]
    public async Task DeveAtualizarEssaEAsProximasPendentes_EmSerieRecorrente()
    {
        var repository = new ReceitaRepoFake
        {
            Receita = new Receita
            {
                Id = 2,
                UsuarioCadastroId = 99,
                Descricao = "Assinatura",
                DataLancamento = new DateOnly(2026, 2, 1),
                DataVencimento = new DateOnly(2026, 2, 2),
                TipoReceita = "freelance",
                TipoRecebimento = "dinheiro",
                Recorrencia = Recorrencia.Mensal,
                QuantidadeRecorrencia = 3,
                ValorTotal = 1000m,
                ValorLiquido = 1000m,
                Status = StatusReceita.Pendente
            },
            ReceitasListadas =
            [
                new Receita
                {
                    Id = 1,
                    UsuarioCadastroId = 99,
                    Descricao = "Assinatura",
                    DataLancamento = new DateOnly(2026, 1, 1),
                    DataVencimento = new DateOnly(2026, 1, 2),
                    TipoReceita = "freelance",
                    TipoRecebimento = "dinheiro",
                    Recorrencia = Recorrencia.Mensal,
                    QuantidadeRecorrencia = 3,
                    ValorTotal = 1000m,
                    ValorLiquido = 1000m,
                    Status = StatusReceita.Efetivada
                },
                new Receita
                {
                    Id = 2,
                    UsuarioCadastroId = 99,
                    Descricao = "Assinatura",
                    DataLancamento = new DateOnly(2026, 2, 1),
                    DataVencimento = new DateOnly(2026, 2, 2),
                    TipoReceita = "freelance",
                    TipoRecebimento = "dinheiro",
                    Recorrencia = Recorrencia.Mensal,
                    QuantidadeRecorrencia = 3,
                    ValorTotal = 1000m,
                    ValorLiquido = 1000m,
                    Status = StatusReceita.Pendente
                },
                new Receita
                {
                    Id = 3,
                    UsuarioCadastroId = 99,
                    Descricao = "Assinatura",
                    DataLancamento = new DateOnly(2026, 3, 1),
                    DataVencimento = new DateOnly(2026, 3, 2),
                    TipoReceita = "freelance",
                    TipoRecebimento = "dinheiro",
                    Recorrencia = Recorrencia.Mensal,
                    QuantidadeRecorrencia = 3,
                    ValorTotal = 1000m,
                    ValorLiquido = 1000m,
                    Status = StatusReceita.Pendente
                }
            ]
        };
        var service = CriarService(repository, new ContaRepoFake(), new AreaRepoFake(), 99);

        var request = new AtualizarReceitaRequest(
            "Receita Atualizada",
            null,
            new DateOnly(2026, 3, 10),
            new DateOnly(2026, 3, 12),
            "freelance",
            "dinheiro",
            Recorrencia.Mensal,
            1000m,
            0m,
            0m,
            0m,
            0m,
            [],
            null,
            null,
            [],
            3,
            false);

        await service.AtualizarAsync(2, request, EscopoRecorrencia.EssaEAsProximas);

        Assert.DoesNotContain(repository.ReceitasAtualizadas, x => x.Id == 1);

        var atualizadaBase = repository.ReceitasAtualizadas.Last(x => x.Id == 2);
        var atualizadaProxima = repository.ReceitasAtualizadas.Last(x => x.Id == 3);

        Assert.Equal(new DateOnly(2026, 3, 10), atualizadaBase.DataLancamento);
        Assert.Equal(new DateOnly(2026, 3, 12), atualizadaBase.DataVencimento);
        Assert.Equal(new DateOnly(2026, 4, 10), atualizadaProxima.DataLancamento);
        Assert.Equal(new DateOnly(2026, 4, 12), atualizadaProxima.DataVencimento);
    }

    [Fact]
    public async Task DeveEncerrarRecorrenciaFixa_AoCancelarTodasPendentes()
    {
        var repository = new ReceitaRepoFake
        {
            Receita = new Receita
            {
                Id = 2,
                UsuarioCadastroId = 99,
                Descricao = "Plano",
                DataLancamento = new DateOnly(2026, 2, 1),
                DataVencimento = new DateOnly(2026, 2, 1),
                TipoReceita = "freelance",
                TipoRecebimento = "dinheiro",
                Recorrencia = Recorrencia.Mensal,
                RecorrenciaFixa = true,
                QuantidadeRecorrencia = 100,
                ValorTotal = 1000m,
                ValorLiquido = 1000m,
                Status = StatusReceita.Pendente
            },
            ReceitasListadas =
            [
                new Receita
                {
                    Id = 1,
                    UsuarioCadastroId = 99,
                    Descricao = "Plano",
                    DataLancamento = new DateOnly(2026, 1, 1),
                    DataVencimento = new DateOnly(2026, 1, 1),
                    TipoReceita = "freelance",
                    TipoRecebimento = "dinheiro",
                    Recorrencia = Recorrencia.Mensal,
                    RecorrenciaFixa = true,
                    QuantidadeRecorrencia = 100,
                    ValorTotal = 1000m,
                    ValorLiquido = 1000m,
                    Status = StatusReceita.Efetivada
                },
                new Receita
                {
                    Id = 2,
                    UsuarioCadastroId = 99,
                    Descricao = "Plano",
                    DataLancamento = new DateOnly(2026, 2, 1),
                    DataVencimento = new DateOnly(2026, 2, 1),
                    TipoReceita = "freelance",
                    TipoRecebimento = "dinheiro",
                    Recorrencia = Recorrencia.Mensal,
                    RecorrenciaFixa = true,
                    QuantidadeRecorrencia = 100,
                    ValorTotal = 1000m,
                    ValorLiquido = 1000m,
                    Status = StatusReceita.Pendente
                },
                new Receita
                {
                    Id = 3,
                    UsuarioCadastroId = 99,
                    Descricao = "Plano",
                    DataLancamento = new DateOnly(2026, 3, 1),
                    DataVencimento = new DateOnly(2026, 3, 1),
                    TipoReceita = "freelance",
                    TipoRecebimento = "dinheiro",
                    Recorrencia = Recorrencia.Mensal,
                    RecorrenciaFixa = true,
                    QuantidadeRecorrencia = 100,
                    ValorTotal = 1000m,
                    ValorLiquido = 1000m,
                    Status = StatusReceita.Pendente
                }
            ]
        };
        var service = CriarService(repository, new ContaRepoFake(), new AreaRepoFake(), 99);

        await service.CancelarAsync(2, EscopoRecorrencia.TodasPendentes);

        var itemBase = repository.ReceitasListadas.Single(x => x.Id == 2);
        var itemProximo = repository.ReceitasListadas.Single(x => x.Id == 3);
        var itemEfetivado = repository.ReceitasListadas.Single(x => x.Id == 1);

        Assert.Equal(StatusReceita.Cancelada, itemBase.Status);
        Assert.Equal(StatusReceita.Cancelada, itemProximo.Status);
        Assert.Equal(StatusReceita.Efetivada, itemEfetivado.Status);
        Assert.All(repository.ReceitasListadas, x => Assert.False(x.RecorrenciaFixa));
    }

    private static CriarReceitaRequest CriarRequestPadrao(
        string tipoRecebimento = "dinheiro",
        string? contaBancaria = null,
        IReadOnlyCollection<ReceitaAreaRateioRequest>? areasRateio = null,
        IReadOnlyCollection<AmigoRateioRequest>? amigos = null,
        Recorrencia recorrencia = Recorrencia.Unica,
        int? quantidadeRecorrencia = null,
        bool recorrenciaFixa = false) =>
        new(
            "Freelance",
            null,
            new DateOnly(2026, 3, 1),
            new DateOnly(2026, 3, 2),
            "freelance",
            tipoRecebimento,
            recorrencia,
            1000m,
            0m,
            0m,
            0m,
            0m,
            areasRateio ?? [],
            contaBancaria,
            null,
            amigos,
            quantidadeRecorrencia,
            recorrenciaFixa);

    private static AtualizarReceitaRequest CriarAtualizacaoPadrao(
        string tipoRecebimento = "dinheiro",
        string? contaBancaria = null,
        IReadOnlyCollection<ReceitaAreaRateioRequest>? areasRateio = null,
        IReadOnlyCollection<AmigoRateioRequest>? amigos = null,
        Recorrencia recorrencia = Recorrencia.Unica,
        int? quantidadeRecorrencia = null,
        bool recorrenciaFixa = false) =>
        new(
            "Receita Atualizada",
            null,
            new DateOnly(2026, 3, 1),
            new DateOnly(2026, 3, 2),
            "freelance",
            tipoRecebimento,
            recorrencia,
            1000m,
            0m,
            0m,
            0m,
            0m,
            areasRateio ?? [],
            contaBancaria,
            null,
            amigos,
            quantidadeRecorrencia,
            recorrenciaFixa);

    private static ReceitaService CriarService(IReceitaRepository receitaRepository, IContaBancariaRepository contaRepository, IAreaRepository areaRepository, int? usuarioId) =>
        CriarService(receitaRepository, contaRepository, areaRepository, new RecorrenciaPublisherFake(), usuarioId);

    private static ReceitaService CriarService(IReceitaRepository receitaRepository, IContaBancariaRepository contaRepository, IAreaRepository areaRepository, IRecorrenciaBackgroundPublisher publisher, int? usuarioId) =>
        new(
            receitaRepository,
            contaRepository,
            new CartaoRepoFake(),
            areaRepository,
            new AmizadeRepositoryFake(),
            new UsuarioRepositoryFake(),
            new UsuarioAutenticadoProviderFake(usuarioId),
            new HistoricoTransacaoFinanceiraService(new HistoricoRepositoryFake()),
            new DocumentoStorageServiceFake(),
            publisher);

    private sealed class UsuarioAutenticadoProviderFake(int? usuarioId) : IUsuarioAutenticadoProvider
    {
        public int? ObterUsuarioId() => usuarioId;
    }

    private sealed class ReceitaRepoFake : IReceitaRepository
    {
        public Receita? Receita { get; set; }
        public List<Receita> ReceitasListadas { get; set; } = [];
        public List<Receita> ReceitasCriadas { get; } = [];
        public int? UltimoUsuarioIdFiltro { get; private set; }
        public string? UltimoIdFiltro { get; private set; }
        public string? UltimaDescricaoFiltro { get; private set; }
        public string? UltimaCompetenciaFiltro { get; private set; }
        public DateOnly? UltimaDataInicioFiltro { get; private set; }
        public DateOnly? UltimaDataFimFiltro { get; private set; }
        public List<Receita> EspelhosPorOrigem { get; set; } = [];
        public List<Receita> ReceitasAtualizadas { get; } = [];
        public bool ValidarUsuarioNoObter { get; set; }

        public Task<List<Receita>> ListarAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(FiltrarListagem(null, null, null, null, null, null));

        public Task<List<Receita>> ListarAsync(string? filtroId, string? descricao, string? competencia, DateOnly? dataInicio, DateOnly? dataFim, CancellationToken cancellationToken = default)
        {
            UltimoIdFiltro = filtroId;
            UltimaDescricaoFiltro = descricao;
            UltimaCompetenciaFiltro = competencia;
            UltimaDataInicioFiltro = dataInicio;
            UltimaDataFimFiltro = dataFim;
            return Task.FromResult(FiltrarListagem(null, filtroId, descricao, competencia, dataInicio, dataFim));
        }
        public Task<List<Receita>> ListarPorUsuarioAsync(int usuarioCadastroId, string? filtroId, string? descricao, string? competencia, DateOnly? dataInicio, DateOnly? dataFim, CancellationToken cancellationToken = default) =>
            ListarPorUsuarioInternoAsync(usuarioCadastroId, filtroId, descricao, competencia, dataInicio, dataFim);
        public Task<List<Receita>> ListarPendentesAprovacaoPorUsuarioAsync(int usuarioCadastroId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<Receita>());
        public Task<List<Receita>> ListarEspelhosPorOrigemAsync(long receitaOrigemId, CancellationToken cancellationToken = default) =>
            Task.FromResult(EspelhosPorOrigem.Where(x => x.ReceitaOrigemId == receitaOrigemId).ToList());
        private Task<List<Receita>> ListarPorUsuarioInternoAsync(int usuarioCadastroId, string? filtroId, string? descricao, string? competencia, DateOnly? dataInicio, DateOnly? dataFim)
        {
            UltimoUsuarioIdFiltro = usuarioCadastroId;
            UltimoIdFiltro = filtroId;
            UltimaDescricaoFiltro = descricao;
            UltimaCompetenciaFiltro = competencia;
            UltimaDataInicioFiltro = dataInicio;
            UltimaDataFimFiltro = dataFim;
            return Task.FromResult(FiltrarListagem(usuarioCadastroId, filtroId, descricao, competencia, dataInicio, dataFim));
        }

        private List<Receita> FiltrarListagem(int? usuarioCadastroId, string? filtroId, string? descricao, string? competencia, DateOnly? dataInicio, DateOnly? dataFim)
        {
            var query = ReceitasListadas.AsEnumerable();

            if (usuarioCadastroId.HasValue)
                query = query.Where(x => x.UsuarioCadastroId == usuarioCadastroId.Value);

            if (!string.IsNullOrWhiteSpace(filtroId))
                query = query.Where(x => x.Id.ToString().Contains(filtroId.Trim(), StringComparison.Ordinal));

            if (!string.IsNullOrWhiteSpace(descricao))
                query = query.Where(x => x.Descricao.Contains(descricao.Trim(), StringComparison.OrdinalIgnoreCase));

            if (dataInicio.HasValue)
                query = query.Where(x => x.DataLancamento >= dataInicio.Value);

            if (dataFim.HasValue)
                query = query.Where(x => x.DataLancamento <= dataFim.Value);

            if (TryParseCompetencia(competencia, out var ano, out var mes))
                query = query.Where(x => x.DataLancamento.Year == ano && x.DataLancamento.Month == mes);

            return query.OrderByDescending(x => x.DataLancamento).ToList();
        }

        private static bool TryParseCompetencia(string? competencia, out int ano, out int mes)
        {
            ano = 0;
            mes = 0;
            if (string.IsNullOrWhiteSpace(competencia))
                return false;

            var formatos = new[] { "MM/yyyy", "MM-yyyy", "yyyy/MM", "yyyy-MM" };
            if (!DateTime.TryParseExact(competencia.Trim(), formatos, null, System.Globalization.DateTimeStyles.None, out var data))
                return false;

            ano = data.Year;
            mes = data.Month;
            return true;
        }
        public Task<Receita?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default) => Task.FromResult(Receita);
        public Task<Receita?> ObterPorIdAsync(long id, int usuarioCadastroId, CancellationToken cancellationToken = default) =>
            Task.FromResult(
                Receita is not null &&
                Receita.Id == id &&
                (!ValidarUsuarioNoObter || Receita.UsuarioCadastroId == usuarioCadastroId)
                    ? Receita
                    : null);
        public Task<Receita> CriarAsync(Receita receita, CancellationToken cancellationToken = default)
        {
            ReceitasCriadas.Add(receita);
            return Task.FromResult(receita);
        }
        public Task<Receita> AtualizarAsync(Receita receita, CancellationToken cancellationToken = default)
        {
            ReceitasAtualizadas.Add(receita);

            var espelhoIndex = EspelhosPorOrigem.FindIndex(x => x.Id == receita.Id);
            if (espelhoIndex >= 0)
            {
                EspelhosPorOrigem[espelhoIndex] = receita;
            }

            return Task.FromResult(receita);
        }
    }

    private sealed class ContaRepoFake : IContaBancariaRepository
    {
        public List<ContaBancaria> Contas { get; set; } = [];

        public Task<List<ContaBancaria>> ListarAsync(CancellationToken cancellationToken = default) => Task.FromResult(Contas);
        public Task<List<ContaBancaria>> ListarAsync(int usuarioCadastroId, CancellationToken cancellationToken = default) =>
            ListarAsync(cancellationToken);
        public Task<ContaBancaria?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default) => Task.FromResult<ContaBancaria?>(null);
        public Task<ContaBancaria?> ObterPorIdAsync(long id, int usuarioCadastroId, CancellationToken cancellationToken = default) =>
            ObterPorIdAsync(id, cancellationToken);
        public Task<ContaBancaria> CriarAsync(ContaBancaria conta, CancellationToken cancellationToken = default) => Task.FromResult(conta);
        public Task<ContaBancaria> AtualizarAsync(ContaBancaria conta, CancellationToken cancellationToken = default) => Task.FromResult(conta);
    }

    private sealed class CartaoRepoFake : ICartaoRepository
    {
        public Task<List<Cartao>> ListarAsync(CancellationToken cancellationToken = default) => Task.FromResult(new List<Cartao>());
        public Task<List<Cartao>> ListarAsync(int usuarioCadastroId, CancellationToken cancellationToken = default) => Task.FromResult(new List<Cartao>());
        public Task<Cartao?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Cartao?>(new Cartao { Id = id, UsuarioCadastroId = 99 });
        public Task<Cartao?> ObterPorIdAsync(long id, int usuarioCadastroId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Cartao?>(new Cartao { Id = id, UsuarioCadastroId = usuarioCadastroId });
        public Task<Cartao> CriarAsync(Cartao cartao, CancellationToken cancellationToken = default) => Task.FromResult(cartao);
        public Task<Cartao> AtualizarAsync(Cartao cartao, CancellationToken cancellationToken = default) => Task.FromResult(cartao);
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

    private sealed class AmizadeRepositoryFake : IAmizadeRepository
    {
        public IReadOnlyCollection<int> AmigosAceitosIds { get; set; } = [2, 3, 4, 5];

        public Task<IReadOnlyCollection<Usuario>> ListarAmigosAceitosAsync(int usuarioId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Usuario>>(Array.Empty<Usuario>());

        public Task<IReadOnlyCollection<int>> ListarIdsAmigosAceitosAsync(int usuarioId, CancellationToken cancellationToken = default) =>
            Task.FromResult(AmigosAceitosIds);

        public Task<IReadOnlyCollection<ConviteAmizade>> ListarConvitesPendentesAsync(int usuarioId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<ConviteAmizade>>(Array.Empty<ConviteAmizade>());

        public Task<ConviteAmizade?> ObterConvitePorIdAsync(long conviteId, CancellationToken cancellationToken = default) =>
            Task.FromResult<ConviteAmizade?>(null);

        public Task<ConviteAmizade?> ObterConvitePendenteAsync(int usuarioOrigemId, int usuarioDestinoId, CancellationToken cancellationToken = default) =>
            Task.FromResult<ConviteAmizade?>(null);

        public Task<bool> ExisteAmizadeAsync(int usuarioId, int amigoId, CancellationToken cancellationToken = default) =>
            Task.FromResult(AmigosAceitosIds.Contains(amigoId));

        public Task<Amizade?> ObterAmizadeAsync(int usuarioId, int amigoId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Amizade?>(null);

        public Task<ConviteAmizade> CriarConviteAsync(ConviteAmizade convite, CancellationToken cancellationToken = default) =>
            Task.FromResult(convite);

        public Task<ConviteAmizade> AtualizarConviteAsync(ConviteAmizade convite, CancellationToken cancellationToken = default) =>
            Task.FromResult(convite);

        public Task<Amizade> CriarAmizadeAsync(Amizade amizade, CancellationToken cancellationToken = default) =>
            Task.FromResult(amizade);

        public Task ExcluirAmizadeAsync(Amizade amizade, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class UsuarioRepositoryFake : IUsuarioRepository
    {
        public IReadOnlyCollection<Usuario> Usuarios { get; set; } =
        [
            new Usuario { Id = 1, Nome = "William", Email = "william@email.com", Ativo = true },
            new Usuario { Id = 2, Nome = "Alex", Email = "alex@email.com", Ativo = true },
            new Usuario { Id = 3, Nome = "Bianca", Email = "bianca@email.com", Ativo = true },
            new Usuario { Id = 4, Nome = "Carlos", Email = "carlos@email.com", Ativo = true },
            new Usuario { Id = 5, Nome = "Dani", Email = "dani@email.com", Ativo = true }
        ];

        public Task<IReadOnlyCollection<Usuario>> ListarAsync(string? filtroId, string? descricao, DateOnly? dataInicio, DateOnly? dataFim, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Usuario>>(Array.Empty<Usuario>());

        public Task<IReadOnlyCollection<Usuario>> ListarAtivosAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Usuarios.Where(x => x.Ativo).ToArray() as IReadOnlyCollection<Usuario>);

        public Task<Usuario?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Usuarios.FirstOrDefault(x => x.Id == id));

        public Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken cancellationToken = default) =>
            Task.FromResult(Usuarios.FirstOrDefault(x => x.Email == email));

        public Task<IReadOnlyCollection<Modulo>> ListarModulosAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Modulo>>(Array.Empty<Modulo>());

        public Task<IReadOnlyCollection<Tela>> ListarTelasAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Tela>>(Array.Empty<Tela>());

        public Task<IReadOnlyCollection<Funcionalidade>> ListarFuncionalidadesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Funcionalidade>>(Array.Empty<Funcionalidade>());

        public Task<bool> ValidarSenhaAsync(Usuario usuario, string senha, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<Usuario> CriarAsync(Usuario usuario, CancellationToken cancellationToken = default) =>
            Task.FromResult(usuario);

        public Task<Usuario> AtualizarAsync(Usuario usuario, CancellationToken cancellationToken = default) =>
            Task.FromResult(usuario);

        public Task SincronizarPermissoesAsync(int usuarioId, int usuarioCadastroId, IReadOnlyCollection<int> modulosAtivosIds, IReadOnlyCollection<int> telasAtivasIds, IReadOnlyCollection<int> funcionalidadesAtivasIds, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task AlterarSenhaAsync(Usuario usuario, string novaSenha, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class HistoricoRepositoryFake : IHistoricoTransacaoFinanceiraRepository
    {
        public Task<HistoricoTransacaoFinanceira> CriarAsync(HistoricoTransacaoFinanceira historico, CancellationToken cancellationToken = default) =>
            Task.FromResult(historico);

        public Task<HistoricoTransacaoFinanceira?> ObterUltimoPorTransacaoAsync(TipoTransacaoFinanceira tipoTransacao, long transacaoId, CancellationToken cancellationToken = default) =>
            Task.FromResult<HistoricoTransacaoFinanceira?>(null);

        public Task<List<HistoricoTransacaoFinanceira>> ListarPorContaBancariaCompetenciaAsync(long contaBancariaId, int usuarioOperacaoId, string? competencia, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<HistoricoTransacaoFinanceira>());

        public Task<List<HistoricoTransacaoFinanceira>> ListarPorCartaoCompetenciaAsync(long cartaoId, int usuarioOperacaoId, string? competencia, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<HistoricoTransacaoFinanceira>());
    }

    private sealed class RecorrenciaPublisherFake : IRecorrenciaBackgroundPublisher
    {
        public ReceitaRecorrenciaBackgroundMessage? ReceitaMessage { get; private set; }

        public Task PublicarDespesaAsync(DespesaRecorrenciaBackgroundMessage message, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task PublicarReceitaAsync(ReceitaRecorrenciaBackgroundMessage message, CancellationToken cancellationToken = default)
        {
            ReceitaMessage = message;
            return Task.CompletedTask;
        }
    }

    private sealed class DocumentoStorageServiceFake : IDocumentoStorageService
    {
        public Task<IReadOnlyCollection<DocumentoDto>> SalvarAsync(IReadOnlyCollection<DocumentoRequest> documentos, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<DocumentoDto>>(
                documentos.Select(x => new DocumentoDto(x.NomeArquivo, $@"C:\temp\{x.NomeArquivo}", x.ContentType, 1)).ToArray());
    }
}
