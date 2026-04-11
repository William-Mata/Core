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

public sealed class DespesaServiceTests
{
    [Fact]
    public async Task DeveListarPorCompetencia_QuandoParametroForInformado()
    {
        var repository = new DespesaRepositoryFake();
        var service = CriarService(repository, 1);

        await service.ListarAsync(new ListarDespesasRequest("12", "Telefone", "03/2026", null, null, false));

        Assert.Equal(1, repository.UltimoUsuarioIdFiltro);
        Assert.Equal("12", repository.UltimoIdFiltro);
        Assert.Equal("Telefone", repository.UltimaDescricaoFiltro);
        Assert.Equal("03/2026", repository.UltimaCompetenciaFiltro);
        Assert.Null(repository.UltimaDataInicioFiltro);
        Assert.Null(repository.UltimaDataFimFiltro);
    }

    [Fact]
    public async Task DeveListarPelaCompetenciaAtual_QuandoNenhumFiltroForInformado()
    {
        var repository = new DespesaRepositoryFake();
        var service = CriarService(repository, 1);

        await service.ListarAsync(new ListarDespesasRequest(null, null, null, null, null, false));

        var hoje = DateOnly.FromDateTime(DateTime.Today);
        Assert.Equal(new DateOnly(hoje.Year, hoje.Month, 1), repository.UltimaDataInicioFiltro);
        Assert.Equal(new DateOnly(hoje.Year, hoje.Month, DateTime.DaysInMonth(hoje.Year, hoje.Month)), repository.UltimaDataFimFiltro);
    }

    [Fact]
    public async Task DeveConsiderarCompetenciaEPeriodo_QuandoAmbosForemInformados()
    {
        var repository = new DespesaRepositoryFake();
        var service = CriarService(repository, 1);

        await service.ListarAsync(new ListarDespesasRequest(null, null, "04-2026", new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31), false));

        Assert.Equal("04-2026", repository.UltimaCompetenciaFiltro);
        Assert.Equal(new DateOnly(2026, 1, 1), repository.UltimaDataInicioFiltro);
        Assert.Equal(new DateOnly(2026, 1, 31), repository.UltimaDataFimFiltro);
    }

    [Fact]
    public async Task NaoDeveExpandirQuantidadeDefinida_AoListarCompetenciaComSerieCompleta()
    {
        var publisher = new RecorrenciaPublisherFake();
        var repository = new DespesaRepositoryFake
        {
            DespesasListadas =
            [
                new Despesa
                {
                    Id = 100,
                    UsuarioCadastroId = 1,
                    Descricao = "Academia",
                    DataLancamento = new DateOnly(2026, 4, 10),
                    DataVencimento = new DateOnly(2026, 4, 10),
                    TipoDespesa = TipoDespesa.Saude,
                    TipoPagamento = TipoPagamento.Pix,
                    Recorrencia = Recorrencia.Mensal,
                    QuantidadeRecorrencia = 2,
                    ValorTotal = 120m,
                    ValorLiquido = 120m,
                    Status = StatusDespesa.Pendente
                },
                new Despesa
                {
                    Id = 101,
                    UsuarioCadastroId = 1,
                    Descricao = "Academia",
                    DataLancamento = new DateOnly(2026, 5, 10),
                    DataVencimento = new DateOnly(2026, 5, 10),
                    TipoDespesa = TipoDespesa.Saude,
                    TipoPagamento = TipoPagamento.Pix,
                    Recorrencia = Recorrencia.Mensal,
                    QuantidadeRecorrencia = 2,
                    ValorTotal = 120m,
                    ValorLiquido = 120m,
                    Status = StatusDespesa.Pendente
                }
            ]
        };
        var service = CriarService(repository, new AreaRepoFake(), publisher, 1);

        await service.ListarAsync(new ListarDespesasRequest(null, null, "05/2026", null, null, true));

        Assert.Null(publisher.DespesaMessage);
    }

    [Fact]
    public async Task NaoDevePublicarMais10Recorrencias_QuandoJaExistirProximaOcorrencia()
    {
        var publisher = new RecorrenciaPublisherFake();
        var repository = new DespesaRepositoryFake
        {
            DespesasListadas =
            [
                new Despesa
                {
                    Id = 100,
                    UsuarioCadastroId = 1,
                    Descricao = "Academia",
                    DataLancamento = new DateOnly(2026, 4, 10),
                    DataVencimento = new DateOnly(2026, 4, 10),
                    TipoDespesa = TipoDespesa.Saude,
                    TipoPagamento = TipoPagamento.Pix,
                    Recorrencia = Recorrencia.Mensal,
                    QuantidadeRecorrencia = 2,
                    ValorTotal = 120m,
                    ValorLiquido = 120m,
                    Status = StatusDespesa.Pendente
                },
                new Despesa
                {
                    Id = 101,
                    UsuarioCadastroId = 1,
                    Descricao = "Academia",
                    DataLancamento = new DateOnly(2026, 5, 10),
                    DataVencimento = new DateOnly(2026, 5, 10),
                    TipoDespesa = TipoDespesa.Saude,
                    TipoPagamento = TipoPagamento.Pix,
                    Recorrencia = Recorrencia.Mensal,
                    QuantidadeRecorrencia = 2,
                    ValorTotal = 120m,
                    ValorLiquido = 120m,
                    Status = StatusDespesa.Pendente
                }
            ]
        };
        var service = CriarService(repository, new AreaRepoFake(), publisher, 1);

        await service.ListarAsync(new ListarDespesasRequest(null, null, "04/2026", null, null, true));

        Assert.Null(publisher.DespesaMessage);
    }

    [Fact]
    public async Task NaoDeveExpandirParcelasDeCartaoDezEmDez_AoListarComVerificacaoDeRecorrencia()
    {
        var publisher = new RecorrenciaPublisherFake();
        var repository = new DespesaRepositoryFake
        {
            DespesasListadas =
            [
                new Despesa
                {
                    Id = 200,
                    UsuarioCadastroId = 1,
                    Descricao = "Compra cartao",
                    DataLancamento = new DateOnly(2026, 4, 10),
                    DataVencimento = new DateOnly(2026, 4, 10),
                    TipoDespesa = TipoDespesa.Saude,
                    TipoPagamento = TipoPagamento.CartaoCredito,
                    Recorrencia = Recorrencia.Mensal,
                    QuantidadeRecorrencia = 2,
                    CartaoId = 77,
                    ValorTotal = 120m,
                    ValorLiquido = 120m,
                    Status = StatusDespesa.Pendente
                },
                new Despesa
                {
                    Id = 201,
                    UsuarioCadastroId = 1,
                    Descricao = "Compra cartao",
                    DataLancamento = new DateOnly(2026, 5, 10),
                    DataVencimento = new DateOnly(2026, 5, 10),
                    TipoDespesa = TipoDespesa.Saude,
                    TipoPagamento = TipoPagamento.CartaoCredito,
                    Recorrencia = Recorrencia.Mensal,
                    QuantidadeRecorrencia = 2,
                    CartaoId = 77,
                    ValorTotal = 120m,
                    ValorLiquido = 120m,
                    Status = StatusDespesa.Pendente
                }
            ]
        };
        var service = CriarService(repository, new AreaRepoFake(), publisher, 1);

        await service.ListarAsync(new ListarDespesasRequest(null, null, "05/2026", null, null, true));

        Assert.Null(publisher.DespesaMessage);
    }

    [Fact]
    public async Task DevePublicarRecuperacao_QuandoSerieNaoAtingiuQuantidadeDefinida()
    {
        var publisher = new RecorrenciaPublisherFake();
        var repository = new DespesaRepositoryFake
        {
            DespesasListadas =
            [
                new Despesa
                {
                    Id = 300,
                    UsuarioCadastroId = 1,
                    Descricao = "Academia",
                    DataLancamento = new DateOnly(2026, 4, 10),
                    DataVencimento = new DateOnly(2026, 4, 10),
                    TipoDespesa = TipoDespesa.Saude,
                    TipoPagamento = TipoPagamento.Pix,
                    Recorrencia = Recorrencia.Mensal,
                    QuantidadeRecorrencia = 3,
                    ValorTotal = 120m,
                    ValorLiquido = 120m,
                    Status = StatusDespesa.Pendente
                },
                new Despesa
                {
                    Id = 301,
                    UsuarioCadastroId = 1,
                    Descricao = "Academia",
                    DataLancamento = new DateOnly(2026, 5, 10),
                    DataVencimento = new DateOnly(2026, 5, 10),
                    TipoDespesa = TipoDespesa.Saude,
                    TipoPagamento = TipoPagamento.Pix,
                    Recorrencia = Recorrencia.Mensal,
                    QuantidadeRecorrencia = 3,
                    ValorTotal = 120m,
                    ValorLiquido = 120m,
                    Status = StatusDespesa.Pendente
                }
            ]
        };
        var service = CriarService(repository, new AreaRepoFake(), publisher, 1);

        await service.ListarAsync(new ListarDespesasRequest(null, null, "05/2026", null, null, true));

        Assert.NotNull(publisher.DespesaMessage);
        Assert.Equal(3, publisher.DespesaMessage!.QuantidadeRecorrencia);
        Assert.Equal(new DateOnly(2026, 4, 10), publisher.DespesaMessage.DataLancamento);
    }

    [Fact]
    public async Task DeveExpandirMais100_QuandoRecorrenciaForFixaENaoHouverLacuna()
    {
        var publisher = new RecorrenciaPublisherFake();
        var despesas = new List<Despesa>();
        var dataBase = new DateOnly(2026, 4, 10);

        for (var i = 0; i < 100; i++)
        {
            var data = dataBase.AddMonths(i);
            despesas.Add(new Despesa
            {
                Id = 400 + i,
                UsuarioCadastroId = 1,
                Descricao = "Contrato fixo",
                DataLancamento = data,
                DataVencimento = data,
                TipoDespesa = TipoDespesa.Servicos,
                TipoPagamento = TipoPagamento.Pix,
                Recorrencia = Recorrencia.Mensal,
                RecorrenciaFixa = true,
                QuantidadeRecorrencia = 100,
                ValorTotal = 100m,
                ValorLiquido = 100m,
                Status = StatusDespesa.Pendente
            });
        }

        var repository = new DespesaRepositoryFake { DespesasListadas = despesas };
        var service = CriarService(repository, new AreaRepoFake(), publisher, 1);

        await service.ListarAsync(new ListarDespesasRequest(null, null, "07/2034", null, null, true));

        Assert.NotNull(publisher.DespesaMessage);
        Assert.True(publisher.DespesaMessage!.RecorrenciaFixa);
        Assert.Equal(200, publisher.DespesaMessage.QuantidadeRecorrencia);
    }

    [Fact]
    public async Task DeveExigirUsuarioAutenticado_ParaCriarDespesa()
    {
        var service = CriarService(new DespesaRepositoryFake(), null);

        var ex = await Assert.ThrowsAsync<DomainException>(() => service.CriarAsync(CriarRequestPadrao()));

        Assert.Equal("usuario_nao_autenticado", ex.Message);
    }

    [Fact]
    public async Task DeveValidarDescricaoObrigatoria_AoCriarDespesa()
    {
        var service = CriarService(new DespesaRepositoryFake(), 1);

        var ex = await Assert.ThrowsAsync<DomainException>(() => service.CriarAsync(CriarRequestPadrao(descricao: "")));

        Assert.Equal("descricao_obrigatoria", ex.Message);
    }

    [Fact]
    public async Task DeveValidarPeriodoDaDespesa()
    {
        var service = CriarService(new DespesaRepositoryFake(), 1);

        var ex = await Assert.ThrowsAsync<DomainException>(() => service.CriarAsync(CriarRequestPadrao(dataLancamento: new DateOnly(2026, 3, 10), dataVencimento: new DateOnly(2026, 3, 1))));

        Assert.Equal("periodo_invalido", ex.Message);
    }

    [Fact]
    public async Task DeveImpedirEfetivacao_ComDadosInvalidos()
    {
        var repository = new DespesaRepositoryFake
        {
            Despesa = new Despesa { Id = 1, Descricao = "Despesa", DataLancamento = new DateOnly(2026, 3, 1), DataVencimento = new DateOnly(2026, 3, 2), TipoDespesa = TipoDespesa.Alimentacao, TipoPagamento = TipoPagamento.Pix, Recorrencia = Recorrencia.Unica, ValorTotal = 100m, ValorLiquido = 100m, Status = StatusDespesa.Pendente }
        };
        var service = CriarService(repository, 1);

        var ex = await Assert.ThrowsAsync<DomainException>(() => service.EfetivarAsync(1, new EfetivarDespesaRequest(new DateOnly(2026, 3, 5), (TipoPagamento)0, 0m, 0m, 0m, 0m, 0m, null)));

        Assert.Equal("dados_invalidos", ex.Message);
    }

    [Fact]
    public async Task DeveRetornarErro_QuandoDespesaNaoForEncontrada()
    {
        var service = CriarService(new DespesaRepositoryFake(), 1);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => service.ObterAsync(99));

        Assert.Equal("despesa_nao_encontrada", ex.Message);
    }

    [Fact]
    public async Task DeveImpedirEfetivacao_QuandoDataEfetivacaoForMenorQueDataLancamento()
    {
        var repository = new DespesaRepositoryFake
        {
            Despesa = new Despesa
            {
                Id = 1,
                Descricao = "Despesa",
                DataLancamento = new DateOnly(2026, 3, 10),
                DataVencimento = new DateOnly(2026, 3, 15),
                TipoDespesa = TipoDespesa.Alimentacao,
                TipoPagamento = TipoPagamento.Pix,
                Recorrencia = Recorrencia.Unica,
                ValorTotal = 100m,
                ValorLiquido = 100m,
                Status = StatusDespesa.Pendente
            }
        };
        var service = CriarService(repository, 1);

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            service.EfetivarAsync(1, new EfetivarDespesaRequest(new DateOnly(2026, 3, 9), TipoPagamento.Pix, 100m, 0m, 0m, 0m, 0m, null)));

        Assert.Equal("periodo_invalido", ex.Message);
    }

    [Fact]
    public async Task DevePermitirEfetivacao_QuandoDataEfetivacaoForIgualDataLancamento()
    {
        var repository = new DespesaRepositoryFake
        {
            Despesa = new Despesa
            {
                Id = 1,
                Descricao = "Despesa",
                DataLancamento = new DateOnly(2026, 3, 10),
                DataVencimento = new DateOnly(2026, 3, 15),
                TipoDespesa = TipoDespesa.Alimentacao,
                TipoPagamento = TipoPagamento.Pix,
                Recorrencia = Recorrencia.Unica,
                ValorTotal = 100m,
                ValorLiquido = 100m,
                Status = StatusDespesa.Pendente
            }
        };
        var service = CriarService(repository, 1);

        var result = await service.EfetivarAsync(1, new EfetivarDespesaRequest(new DateOnly(2026, 3, 10), TipoPagamento.Pix, 100m, 0m, 0m, 0m, 0m, null));

        Assert.Equal("efetivada", result.Status);
    }

    [Fact]
    public async Task DevePublicarMensagemDeRecorrencia_AoCriarDespesaRecorrente()
    {
        var repository = new DespesaRepositoryFake();
        var publisher = new RecorrenciaPublisherFake();
        var service = CriarService(repository, new AreaRepoFake(), publisher, 1);

        await service.CriarAsync(CriarRequestPadrao(recorrencia: Recorrencia.Mensal, quantidadeRecorrencia: 2));

        Assert.Single(repository.DespesasCriadas);
        Assert.NotNull(publisher.DespesaMessage);
        Assert.Equal(2, publisher.DespesaMessage!.QuantidadeRecorrencia);
    }

    [Fact]
    public async Task DevePublicarMensagemComAlvo100_AoCriarDespesaComRecorrenciaFixa()
    {
        var repository = new DespesaRepositoryFake();
        var publisher = new RecorrenciaPublisherFake();
        var service = CriarService(repository, new AreaRepoFake(), publisher, 1);

        await service.CriarAsync(CriarRequestPadrao(recorrencia: Recorrencia.Mensal, quantidadeRecorrencia: null, recorrenciaFixa: true));

        Assert.Single(repository.DespesasCriadas);
        Assert.NotNull(publisher.DespesaMessage);
        Assert.True(publisher.DespesaMessage!.RecorrenciaFixa);
        Assert.Equal(100, publisher.DespesaMessage!.QuantidadeRecorrencia);
    }

    [Fact]
    public async Task DeveRejeitarRecorrenciaFixaComTipoUnico()
    {
        var service = CriarService(new DespesaRepositoryFake(), 1);

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            service.CriarAsync(CriarRequestPadrao(recorrencia: Recorrencia.Unica, recorrenciaFixa: true)));

        Assert.Equal("recorrencia_fixa_invalida", ex.Message);
    }

    [Fact]
    public async Task DeveValidarRelacaoEntreAreaESubArea_AoCriarDespesa()
    {
        var areaRepository = new AreaRepoFake
        {
            SubAreas = [new SubArea { Id = 2, AreaId = 99, Nome = "SubArea" }]
        };
        var service = CriarService(new DespesaRepositoryFake(), areaRepository, 1);

        var ex = await Assert.ThrowsAsync<DomainException>(() => service.CriarAsync(CriarRequestPadrao(areasRateio: [new DespesaAreaRateioRequest(1, 2, 100m)])));

        Assert.Equal("relacao_area_subarea_invalida", ex.Message);
    }

    [Fact]
    public async Task DeveRejeitarRateioDeAmigosQuandoSomaForDiferenteDoValorTotal_AoCriarDespesa()
    {
        var service = CriarService(new DespesaRepositoryFake(), 1);

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            service.CriarAsync(CriarRequestPadrao(amigos: [new AmigoRateioRequest(2, 40m), new AmigoRateioRequest(3, 30m)])));

        Assert.Equal("rateio_amigos_invalido", ex.Message);
    }

    [Fact]
    public async Task DeveExigirValorTotalRateioAmigos_QuandoHouverRateioComAmigos_AoCriarDespesa()
    {
        var service = CriarService(new DespesaRepositoryFake(), 1);
        var request = CriarRequestPadrao(amigos: [new AmigoRateioRequest(2, 50m), new AmigoRateioRequest(3, 50m)])
            with { ValorTotalRateioAmigos = null };

        var ex = await Assert.ThrowsAsync<DomainException>(() => service.CriarAsync(request));

        Assert.Equal("rateio_amigos_invalido", ex.Message);
    }

    [Fact]
    public async Task DeveRejeitarValorTotalRateioAmigos_QuandoNaoForMaiorQueValorLiquido_AoCriarDespesa()
    {
        var service = CriarService(new DespesaRepositoryFake(), 1);
        var request = CriarRequestPadrao(amigos: [new AmigoRateioRequest(2, 50m), new AmigoRateioRequest(3, 50m)])
            with { ValorTotalRateioAmigos = 99m };

        var ex = await Assert.ThrowsAsync<DomainException>(() => service.CriarAsync(request));

        Assert.Equal("rateio_amigos_invalido", ex.Message);
    }

    [Fact]
    public async Task DeveRejeitarRateioDeAreaQuandoSomaForDiferenteDoValorTotal_AoCriarDespesa()
    {
        var areaRepository = CriarAreaRepoValida(TipoAreaFinanceira.Despesa);
        var service = CriarService(new DespesaRepositoryFake(), areaRepository, 1);

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            service.CriarAsync(CriarRequestPadrao(areasRateio: [new DespesaAreaRateioRequest(1, 2, 40m)])));

        Assert.Equal("rateio_area_invalido", ex.Message);
    }

    [Fact]
    public async Task DevePermitirCriacao_QuandoRateiosSomamValorTotal()
    {
        var areaRepository = CriarAreaRepoValida(TipoAreaFinanceira.Despesa);
        var service = CriarService(new DespesaRepositoryFake(), areaRepository, 1);

        var result = await service.CriarAsync(
            CriarRequestPadrao(
                amigos: [new AmigoRateioRequest(2, 60m), new AmigoRateioRequest(3, 40m)],
                areasRateio: [new DespesaAreaRateioRequest(1, 2, 100m)]));

        Assert.Equal("Despesa", result.Descricao);
        Assert.Equal(100m, result.ValorTotal);
        Assert.Equal(2, result.AmigosRateio.Count);
        Assert.Single(result.AreasSubAreasRateio);
    }

    [Fact]
    public async Task DevePermitirRateioIgualitarioComUsuarioLogadoSemGerarAutoEspelho()
    {
        var repository = new DespesaRepositoryFake();
        var areaRepository = CriarAreaRepoValida(TipoAreaFinanceira.Despesa);
        var service = CriarService(repository, areaRepository, 1);

        var result = await service.CriarAsync(
            CriarRequestPadrao(
                amigos:
                [
                    new AmigoRateioRequest(1, null),
                    new AmigoRateioRequest(2, null),
                    new AmigoRateioRequest(3, null)
                ],
                areasRateio: [new DespesaAreaRateioRequest(1, 2, 100m)]));

        Assert.Equal(3, result.AmigosRateio.Count);
        Assert.Equal(100m, result.AmigosRateio.Sum(x => x.Valor ?? 0m));
        Assert.Equal(3, repository.DespesasCriadas.Count);
        Assert.Equal(2, repository.DespesasCriadas.Count(x => x.Status == StatusDespesa.PendenteAprovacao));
        Assert.DoesNotContain(repository.DespesasCriadas, x => x.DespesaOrigemId.HasValue && x.UsuarioCadastroId == 1);
    }

    [Fact]
    public async Task DeveRejeitarRateioQuandoAmigoNaoForAceito()
    {
        var areaRepository = CriarAreaRepoValida(TipoAreaFinanceira.Despesa);
        var service = new DespesaService(
            new DespesaRepositoryFake(),
            new ContaBancariaRepositoryFake(),
            new CartaoRepositoryFake(),
            areaRepository,
            new AmizadeRepositoryFake { AmigosAceitosIds = [2] },
            new UsuarioRepositoryFake(),
            new UsuarioAutenticadoProviderFake(1),
            new HistoricoTransacaoFinanceiraService(new HistoricoRepositoryFake()),
            new DocumentoStorageServiceFake(),
            new RecorrenciaPublisherFake());

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            service.CriarAsync(CriarRequestPadrao(amigos: [new AmigoRateioRequest(2, 50m), new AmigoRateioRequest(3, 50m)])));

        Assert.Equal("amigo_rateio_invalido", ex.Message);
    }

    [Fact]
    public async Task DeveCriarEspelhosEmPendenteAprovacao_AoCriarComRateioDeAmigos()
    {
        var repository = new DespesaRepositoryFake();
        var areaRepository = CriarAreaRepoValida(TipoAreaFinanceira.Despesa);
        var service = CriarService(repository, areaRepository, 1);

        await service.CriarAsync(CriarRequestPadrao(
            amigos: [new AmigoRateioRequest(2, 60m), new AmigoRateioRequest(3, 40m)],
            areasRateio: [new DespesaAreaRateioRequest(1, 2, 100m)]));

        Assert.Equal(3, repository.DespesasCriadas.Count);
        Assert.Single(repository.DespesasCriadas.Where(x => x.DespesaOrigemId is null));
        Assert.Equal(2, repository.DespesasCriadas.Count(x => x.Status == StatusDespesa.PendenteAprovacao));
    }

    [Fact]
    public async Task DeveIgnorarMeioFinanceiroAoCriarDespesaComRateioDeAmigos()
    {
        var repository = new DespesaRepositoryFake();
        var areaRepository = CriarAreaRepoValida(TipoAreaFinanceira.Despesa);
        var publisher = new RecorrenciaPublisherFake();
        var service = CriarService(repository, areaRepository, publisher, 1);

        var result = await service.CriarAsync(CriarRequestPadrao(
            tipoPagamento: TipoPagamento.CartaoCredito,
            cartaoId: 99,
            amigos: [new AmigoRateioRequest(2, 60m), new AmigoRateioRequest(3, 40m)],
            areasRateio: [new DespesaAreaRateioRequest(1, 2, 100m)],
            recorrencia: Recorrencia.Mensal,
            quantidadeRecorrencia: 2));

        Assert.Equal(99, result.CartaoId);
        Assert.Equal(99, repository.DespesasCriadas.Single(x => x.DespesaOrigemId is null).CartaoId);
        Assert.All(repository.DespesasCriadas.Where(x => x.DespesaOrigemId.HasValue), x =>
        {
            Assert.Null(x.ContaBancariaId);
            Assert.Null(x.CartaoId);
        });
        Assert.Equal(99, publisher.DespesaMessage?.CartaoId);
    }

    [Fact]
    public async Task DeveAprovarRateioQuandoDespesaEspelhoEstiverPendenteAprovacao()
    {
        var repository = new DespesaRepositoryFake
        {
            Despesa = new Despesa
            {
                Id = 10,
                DespesaOrigemId = 1,
                UsuarioCadastroId = 2,
                Status = StatusDespesa.PendenteAprovacao
            }
        };
        var service = CriarService(repository, 2);

        var result = await service.AprovarRateioAsync(10);

        Assert.Equal("pendente", result.Status);
    }

    [Fact]
    public async Task DeveRejeitarRateioQuandoDespesaEspelhoEstiverPendenteAprovacao()
    {
        var repository = new DespesaRepositoryFake
        {
            Despesa = new Despesa
            {
                Id = 10,
                DespesaOrigemId = 1,
                UsuarioCadastroId = 2,
                Status = StatusDespesa.PendenteAprovacao
            }
        };
        var service = CriarService(repository, 2);

        var result = await service.RejeitarRateioAsync(10);

        Assert.Equal("rejeitado", result.Status);
    }

    [Fact]
    public async Task DeveReabrirEspelhoRejeitadoNoMesmoRegistro_AoAtualizarDespesa()
    {
        var repository = new DespesaRepositoryFake
        {
            Despesa = new Despesa
            {
                Id = 1,
                UsuarioCadastroId = 1,
                Descricao = "Despesa",
                DataLancamento = new DateOnly(2026, 3, 1),
                DataVencimento = new DateOnly(2026, 3, 2),
                TipoDespesa = TipoDespesa.Alimentacao,
                TipoPagamento = TipoPagamento.Pix,
                Recorrencia = Recorrencia.Unica,
                ValorTotal = 100m,
                ValorLiquido = 100m,
                Status = StatusDespesa.Pendente
            },
            EspelhosPorOrigem =
            [
                new Despesa
                {
                    Id = 10,
                    DespesaOrigemId = 1,
                    UsuarioCadastroId = 2,
                    Descricao = "Espelho",
                    DataLancamento = new DateOnly(2026, 3, 1),
                    DataVencimento = new DateOnly(2026, 3, 2),
                    TipoDespesa = TipoDespesa.Alimentacao,
                    TipoPagamento = TipoPagamento.Pix,
                    Recorrencia = Recorrencia.Unica,
                    ValorTotal = 100m,
                    ValorLiquido = 100m,
                    Status = StatusDespesa.Rejeitado
                }
            ]
        };
        var service = CriarService(repository, CriarAreaRepoValida(TipoAreaFinanceira.Despesa), 1);

        await service.AtualizarAsync(1, CriarAtualizacaoPadrao(
            amigos: [new AmigoRateioRequest(2, 100m)],
            areasRateio: [new DespesaAreaRateioRequest(1, 2, 100m)]));

        var espelhoAtualizado = repository.DespesasAtualizadas.Last(x => x.Id == 10);
        Assert.Equal(StatusDespesa.PendenteAprovacao, espelhoAtualizado.Status);
        Assert.DoesNotContain(repository.DespesasCriadas, x => x.Id != 1 && x.UsuarioCadastroId == 2 && x.DespesaOrigemId == 1);
    }

    [Fact]
    public async Task DeveCancelarEspelhoQuandoAmigoForRemovido_AoAtualizarDespesa()
    {
        var repository = new DespesaRepositoryFake
        {
            Despesa = new Despesa
            {
                Id = 1,
                UsuarioCadastroId = 1,
                Descricao = "Despesa",
                DataLancamento = new DateOnly(2026, 3, 1),
                DataVencimento = new DateOnly(2026, 3, 2),
                TipoDespesa = TipoDespesa.Alimentacao,
                TipoPagamento = TipoPagamento.Pix,
                Recorrencia = Recorrencia.Unica,
                ValorTotal = 100m,
                ValorLiquido = 100m,
                Status = StatusDespesa.Pendente
            },
            EspelhosPorOrigem =
            [
                new Despesa
                {
                    Id = 10,
                    DespesaOrigemId = 1,
                    UsuarioCadastroId = 2,
                    Descricao = "Espelho",
                    DataLancamento = new DateOnly(2026, 3, 1),
                    DataVencimento = new DateOnly(2026, 3, 2),
                    TipoDespesa = TipoDespesa.Alimentacao,
                    TipoPagamento = TipoPagamento.Pix,
                    Recorrencia = Recorrencia.Unica,
                    ValorTotal = 100m,
                    ValorLiquido = 100m,
                    Status = StatusDespesa.PendenteAprovacao
                }
            ]
        };
        var service = CriarService(repository, 1);

        await service.AtualizarAsync(1, CriarAtualizacaoPadrao(amigos: [], areasRateio: []));

        var espelhoCancelado = repository.DespesasAtualizadas.Last(x => x.Id == 10);
        Assert.Equal(StatusDespesa.Cancelada, espelhoCancelado.Status);
    }

    [Fact]
    public async Task DeveDistribuirRateioDeAreaProporcionalmenteNoEspelho_ComFechamentoDeSoma()
    {
        var repository = new DespesaRepositoryFake();
        var area = new Area { Id = 1, Nome = "Area", Tipo = TipoAreaFinanceira.Despesa };
        var areaRepository = new AreaRepoFake
        {
            SubAreas =
            [
                new SubArea { Id = 2, AreaId = 1, Area = area, Nome = "SubArea 1" },
                new SubArea { Id = 3, AreaId = 1, Area = area, Nome = "SubArea 2" }
            ]
        };
        var service = CriarService(repository, areaRepository, 1);

        await service.CriarAsync(CriarRequestPadrao(
            amigos:
            [
                new AmigoRateioRequest(2, 33.33m),
                new AmigoRateioRequest(3, 66.67m)
            ],
            areasRateio:
            [
                new DespesaAreaRateioRequest(1, 2, 70m),
                new DespesaAreaRateioRequest(1, 3, 30m)
            ]));

        var espelho = repository.DespesasCriadas.Single(x => x.DespesaOrigemId.HasValue && x.UsuarioCadastroId == 2);
        var somaAreas = espelho.AreasRateio.Sum(x => x.Valor ?? 0m);
        Assert.Equal(33.33m, somaAreas);
    }

    [Fact]
    public async Task DeveImpedirAprovacaoQuandoUsuarioNaoForDonoDoEspelho()
    {
        var repository = new DespesaRepositoryFake
        {
            ValidarUsuarioNoObter = true,
            Despesa = new Despesa
            {
                Id = 10,
                DespesaOrigemId = 1,
                UsuarioCadastroId = 2,
                Status = StatusDespesa.PendenteAprovacao
            }
        };
        var service = CriarService(repository, 1);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => service.AprovarRateioAsync(10));

        Assert.Equal("despesa_nao_encontrada", ex.Message);
    }

    [Fact]
    public async Task DeveRetornarErro_QuandoPagamentoForCartaoESemQuantidadeParcelas()
    {
        var service = CriarService(new DespesaRepositoryFake(), 1);

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            service.CriarAsync(CriarRequestPadrao(tipoPagamento: TipoPagamento.CartaoCredito, quantidadeRecorrencia: null, quantidadeParcelas: null)));

        Assert.Equal("quantidade_parcelas_invalida", ex.Message);
    }

    [Fact]
    public async Task DeveRetornarErro_QuandoPagamentoForCartaoEApenasQuantidadeRecorrenciaForInformada()
    {
        var service = CriarService(new DespesaRepositoryFake(), 1);

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            service.CriarAsync(CriarRequestPadrao(tipoPagamento: TipoPagamento.CartaoCredito, quantidadeRecorrencia: 23, quantidadeParcelas: null)));

        Assert.Equal("quantidade_parcelas_invalida", ex.Message);
    }

    [Fact]
    public async Task DeveTratarCartaoComoParcelamentoMensal_AoCriarDespesa()
    {
        var repository = new DespesaRepositoryFake();
        var publisher = new RecorrenciaPublisherFake();
        var service = CriarService(repository, new AreaRepoFake(), publisher, 1);

        await service.CriarAsync(CriarRequestPadrao(tipoPagamento: TipoPagamento.CartaoCredito, quantidadeRecorrencia: null, quantidadeParcelas: 3, recorrenciaFixa: true));

        var despesa = Assert.Single(repository.DespesasCriadas);
        Assert.Equal(Recorrencia.Mensal, despesa.Recorrencia);
        Assert.False(despesa.RecorrenciaFixa);
        Assert.Equal(3, despesa.QuantidadeRecorrencia);

        Assert.NotNull(publisher.DespesaMessage);
        Assert.Equal(Recorrencia.Mensal, publisher.DespesaMessage!.Recorrencia);
        Assert.False(publisher.DespesaMessage.RecorrenciaFixa);
        Assert.Equal(3, publisher.DespesaMessage.QuantidadeRecorrencia);
    }

    [Fact]
    public async Task DevePriorizarQuantidadeParcelas_QuandoPagamentoForCartao()
    {
        var repository = new DespesaRepositoryFake();
        var publisher = new RecorrenciaPublisherFake();
        var service = CriarService(repository, new AreaRepoFake(), publisher, 1);

        await service.CriarAsync(CriarRequestPadrao(tipoPagamento: TipoPagamento.CartaoCredito, quantidadeRecorrencia: 23, quantidadeParcelas: 3));

        var despesa = Assert.Single(repository.DespesasCriadas);
        Assert.Equal(3, despesa.QuantidadeRecorrencia);

        Assert.NotNull(publisher.DespesaMessage);
        Assert.Equal(3, publisher.DespesaMessage!.QuantidadeRecorrencia);
    }

    [Fact]
    public async Task DeveRejeitarQuantidadeRecorrenciaMaiorQue100_QuandoNaoFixa()
    {
        var service = CriarService(new DespesaRepositoryFake(), 1);

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            service.CriarAsync(CriarRequestPadrao(recorrencia: Recorrencia.Mensal, quantidadeRecorrencia: 101, recorrenciaFixa: false)));

        Assert.Equal("quantidade_recorrencia_invalida", ex.Message);
    }

    [Fact]
    public async Task DeveAtualizarEssaEAsProximasPendentes_EmSerieRecorrente()
    {
        var repository = new DespesaRepositoryFake
        {
            Despesa = new Despesa
            {
                Id = 2,
                UsuarioCadastroId = 1,
                Descricao = "Academia",
                DataLancamento = new DateOnly(2026, 2, 1),
                DataVencimento = new DateOnly(2026, 2, 2),
                TipoDespesa = TipoDespesa.Alimentacao,
                TipoPagamento = TipoPagamento.Pix,
                Recorrencia = Recorrencia.Mensal,
                QuantidadeRecorrencia = 3,
                ValorTotal = 100m,
                ValorLiquido = 100m,
                Status = StatusDespesa.Pendente
            },
            DespesasListadas =
            [
                new Despesa
                {
                    Id = 1,
                    UsuarioCadastroId = 1,
                    Descricao = "Academia",
                    DataLancamento = new DateOnly(2026, 1, 1),
                    DataVencimento = new DateOnly(2026, 1, 2),
                    TipoDespesa = TipoDespesa.Alimentacao,
                    TipoPagamento = TipoPagamento.Pix,
                    Recorrencia = Recorrencia.Mensal,
                    QuantidadeRecorrencia = 3,
                    ValorTotal = 100m,
                    ValorLiquido = 100m,
                    Status = StatusDespesa.Efetivada
                },
                new Despesa
                {
                    Id = 2,
                    UsuarioCadastroId = 1,
                    Descricao = "Academia",
                    DataLancamento = new DateOnly(2026, 2, 1),
                    DataVencimento = new DateOnly(2026, 2, 2),
                    TipoDespesa = TipoDespesa.Alimentacao,
                    TipoPagamento = TipoPagamento.Pix,
                    Recorrencia = Recorrencia.Mensal,
                    QuantidadeRecorrencia = 3,
                    ValorTotal = 100m,
                    ValorLiquido = 100m,
                    Status = StatusDespesa.Pendente
                },
                new Despesa
                {
                    Id = 3,
                    UsuarioCadastroId = 1,
                    Descricao = "Academia",
                    DataLancamento = new DateOnly(2026, 3, 1),
                    DataVencimento = new DateOnly(2026, 3, 2),
                    TipoDespesa = TipoDespesa.Alimentacao,
                    TipoPagamento = TipoPagamento.Pix,
                    Recorrencia = Recorrencia.Mensal,
                    QuantidadeRecorrencia = 3,
                    ValorTotal = 100m,
                    ValorLiquido = 100m,
                    Status = StatusDespesa.Pendente
                }
            ]
        };
        var service = CriarService(repository, 1);

        await service.AtualizarAsync(
            2,
            CriarAtualizacaoPadrao(
                dataLancamento: new DateOnly(2026, 3, 10),
                dataVencimento: new DateOnly(2026, 3, 12),
                recorrencia: Recorrencia.Mensal,
                quantidadeRecorrencia: 3),
            EscopoRecorrencia.EssaEAsProximas);

        Assert.DoesNotContain(repository.DespesasAtualizadas, x => x.Id == 1);

        var atualizadaBase = repository.DespesasAtualizadas.Last(x => x.Id == 2);
        var atualizadaProxima = repository.DespesasAtualizadas.Last(x => x.Id == 3);

        Assert.Equal(new DateOnly(2026, 3, 10), atualizadaBase.DataLancamento);
        Assert.Equal(new DateOnly(2026, 3, 12), atualizadaBase.DataVencimento);
        Assert.Equal(new DateOnly(2026, 4, 10), atualizadaProxima.DataLancamento);
        Assert.Equal(new DateOnly(2026, 4, 12), atualizadaProxima.DataVencimento);
    }

    [Fact]
    public async Task DeveEncerrarRecorrenciaFixa_AoCancelarTodasPendentes()
    {
        var repository = new DespesaRepositoryFake
        {
            Despesa = new Despesa
            {
                Id = 2,
                UsuarioCadastroId = 1,
                Descricao = "Mensalidade",
                DataLancamento = new DateOnly(2026, 2, 1),
                DataVencimento = new DateOnly(2026, 2, 1),
                TipoDespesa = TipoDespesa.Servicos,
                TipoPagamento = TipoPagamento.Pix,
                Recorrencia = Recorrencia.Mensal,
                RecorrenciaFixa = true,
                QuantidadeRecorrencia = 100,
                ValorTotal = 100m,
                ValorLiquido = 100m,
                Status = StatusDespesa.Pendente
            },
            DespesasListadas =
            [
                new Despesa
                {
                    Id = 1,
                    UsuarioCadastroId = 1,
                    Descricao = "Mensalidade",
                    DataLancamento = new DateOnly(2026, 1, 1),
                    DataVencimento = new DateOnly(2026, 1, 1),
                    TipoDespesa = TipoDespesa.Servicos,
                    TipoPagamento = TipoPagamento.Pix,
                    Recorrencia = Recorrencia.Mensal,
                    RecorrenciaFixa = true,
                    QuantidadeRecorrencia = 100,
                    ValorTotal = 100m,
                    ValorLiquido = 100m,
                    Status = StatusDespesa.Efetivada
                },
                new Despesa
                {
                    Id = 2,
                    UsuarioCadastroId = 1,
                    Descricao = "Mensalidade",
                    DataLancamento = new DateOnly(2026, 2, 1),
                    DataVencimento = new DateOnly(2026, 2, 1),
                    TipoDespesa = TipoDespesa.Servicos,
                    TipoPagamento = TipoPagamento.Pix,
                    Recorrencia = Recorrencia.Mensal,
                    RecorrenciaFixa = true,
                    QuantidadeRecorrencia = 100,
                    ValorTotal = 100m,
                    ValorLiquido = 100m,
                    Status = StatusDespesa.Pendente
                },
                new Despesa
                {
                    Id = 3,
                    UsuarioCadastroId = 1,
                    Descricao = "Mensalidade",
                    DataLancamento = new DateOnly(2026, 3, 1),
                    DataVencimento = new DateOnly(2026, 3, 1),
                    TipoDespesa = TipoDespesa.Servicos,
                    TipoPagamento = TipoPagamento.Pix,
                    Recorrencia = Recorrencia.Mensal,
                    RecorrenciaFixa = true,
                    QuantidadeRecorrencia = 100,
                    ValorTotal = 100m,
                    ValorLiquido = 100m,
                    Status = StatusDespesa.Pendente
                }
            ]
        };
        var service = CriarService(repository, 1);

        await service.CancelarAsync(2, EscopoRecorrencia.TodasPendentes);

        var itemBase = repository.DespesasListadas.Single(x => x.Id == 2);
        var itemProximo = repository.DespesasListadas.Single(x => x.Id == 3);
        var itemEfetivado = repository.DespesasListadas.Single(x => x.Id == 1);

        Assert.Equal(StatusDespesa.Cancelada, itemBase.Status);
        Assert.Equal(StatusDespesa.Cancelada, itemProximo.Status);
        Assert.Equal(StatusDespesa.Efetivada, itemEfetivado.Status);
        Assert.All(repository.DespesasListadas, x => Assert.False(x.RecorrenciaFixa));
    }

    private static CriarDespesaRequest CriarRequestPadrao(
        string descricao = "Despesa",
        DateOnly? dataLancamento = null,
        DateOnly? dataVencimento = null,
        IReadOnlyCollection<DespesaAreaRateioRequest>? areasRateio = null,
        IReadOnlyCollection<AmigoRateioRequest>? amigos = null,
        TipoDespesa tipoDespesa = TipoDespesa.Alimentacao,
        TipoPagamento tipoPagamento = TipoPagamento.Pix,
        long? contaBancariaId = null,
        long? cartaoId = null,
        Recorrencia recorrencia = Recorrencia.Unica,
        int? quantidadeRecorrencia = null,
        int? quantidadeParcelas = null,
        bool recorrenciaFixa = false)
    {
        var possuiRateioAmigos = amigos is not null && amigos.Count > 0;
        var desconto = possuiRateioAmigos ? 1m : 0m;
        var valorTotalRateioAmigos = possuiRateioAmigos
            ? (amigos!.All(x => x.Valor.HasValue) ? amigos.Sum(x => x.Valor!.Value) : 100m)
            : (decimal?)null;

        return new CriarDespesaRequest(
            descricao,
            null,
            dataLancamento ?? new DateOnly(2026, 3, 1),
            dataVencimento ?? new DateOnly(2026, 3, 2),
            tipoDespesa,
            tipoPagamento,
            recorrencia,
            100m,
            desconto,
            0m,
            0m,
            0m,
            null,
            amigos,
            areasRateio,
            quantidadeRecorrencia,
            quantidadeParcelas,
            recorrenciaFixa,
            null,
            contaBancariaId,
            cartaoId,
            valorTotalRateioAmigos);
    }

    private static AtualizarDespesaRequest CriarAtualizacaoPadrao(
        string descricao = "Despesa Atualizada",
        DateOnly? dataLancamento = null,
        DateOnly? dataVencimento = null,
        IReadOnlyCollection<DespesaAreaRateioRequest>? areasRateio = null,
        IReadOnlyCollection<AmigoRateioRequest>? amigos = null,
        TipoPagamento tipoPagamento = TipoPagamento.Pix,
        Recorrencia recorrencia = Recorrencia.Unica,
        int? quantidadeRecorrencia = null,
        int? quantidadeParcelas = null,
        bool recorrenciaFixa = false)
    {
        var possuiRateioAmigos = amigos is not null && amigos.Count > 0;
        var desconto = possuiRateioAmigos ? 1m : 0m;
        var valorTotalRateioAmigos = possuiRateioAmigos
            ? (amigos!.All(x => x.Valor.HasValue) ? amigos.Sum(x => x.Valor!.Value) : 100m)
            : (decimal?)null;

        return new AtualizarDespesaRequest(
            descricao,
            null,
            dataLancamento ?? new DateOnly(2026, 3, 1),
            dataVencimento ?? new DateOnly(2026, 3, 2),
            TipoDespesa.Alimentacao,
            tipoPagamento,
            recorrencia,
            100m,
            desconto,
            0m,
            0m,
            0m,
            null,
            amigos,
            areasRateio,
            quantidadeRecorrencia,
            quantidadeParcelas,
            recorrenciaFixa,
            null,
            null,
            valorTotalRateioAmigos);
    }

    private static DespesaService CriarService(IDespesaRepository repository, int? usuarioId) =>
        CriarService(repository, new AreaRepoFake(), new RecorrenciaPublisherFake(), usuarioId);

    private static DespesaService CriarService(IDespesaRepository repository, IAreaRepository areaRepository, int? usuarioId) =>
        CriarService(repository, areaRepository, new RecorrenciaPublisherFake(), usuarioId);

    private static DespesaService CriarService(IDespesaRepository repository, IAreaRepository areaRepository, IRecorrenciaBackgroundPublisher publisher, int? usuarioId) =>
        new(
            repository,
            new ContaBancariaRepositoryFake(),
            new CartaoRepositoryFake(),
            areaRepository,
            new AmizadeRepositoryFake(),
            new UsuarioRepositoryFake(),
            new UsuarioAutenticadoProviderFake(usuarioId),
            new HistoricoTransacaoFinanceiraService(new HistoricoRepositoryFake()),
            new DocumentoStorageServiceFake(),
            publisher);

    private sealed class DespesaRepositoryFake : IDespesaRepository
    {
        public Despesa? Despesa { get; set; }
        public List<Despesa> DespesasListadas { get; set; } = [];
        public List<Despesa> DespesasCriadas { get; } = [];
        public int? UltimoUsuarioIdFiltro { get; private set; }
        public string? UltimoIdFiltro { get; private set; }
        public string? UltimaDescricaoFiltro { get; private set; }
        public string? UltimaCompetenciaFiltro { get; private set; }
        public DateOnly? UltimaDataInicioFiltro { get; private set; }
        public DateOnly? UltimaDataFimFiltro { get; private set; }
        public List<Despesa> EspelhosPorOrigem { get; set; } = [];
        public List<Despesa> DespesasAtualizadas { get; } = [];
        public bool ValidarUsuarioNoObter { get; set; }

        public Task<List<Despesa>> ListarAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(FiltrarListagem(null, null, null, null, null, null));

        public Task<List<Despesa>> ListarAsync(string? filtroId, string? descricao, string? competencia, DateOnly? dataInicio, DateOnly? dataFim, CancellationToken cancellationToken = default)
        {
            UltimoIdFiltro = filtroId;
            UltimaDescricaoFiltro = descricao;
            UltimaCompetenciaFiltro = competencia;
            UltimaDataInicioFiltro = dataInicio;
            UltimaDataFimFiltro = dataFim;
            return Task.FromResult(FiltrarListagem(null, filtroId, descricao, competencia, dataInicio, dataFim));
        }
        public Task<List<Despesa>> ListarPorUsuarioAsync(int usuarioCadastroId, string? filtroId, string? descricao, string? competencia, DateOnly? dataInicio, DateOnly? dataFim, CancellationToken cancellationToken = default) =>
            ListarPorUsuarioInternoAsync(usuarioCadastroId, filtroId, descricao, competencia, dataInicio, dataFim);
        public Task<List<Despesa>> ListarPendentesAprovacaoPorUsuarioAsync(int usuarioCadastroId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<Despesa>());
        public Task<List<Despesa>> ListarEspelhosPorOrigemAsync(long despesaOrigemId, CancellationToken cancellationToken = default) =>
            Task.FromResult(EspelhosPorOrigem.Where(x => x.DespesaOrigemId == despesaOrigemId).ToList());
        private Task<List<Despesa>> ListarPorUsuarioInternoAsync(int usuarioCadastroId, string? filtroId, string? descricao, string? competencia, DateOnly? dataInicio, DateOnly? dataFim)
        {
            UltimoUsuarioIdFiltro = usuarioCadastroId;
            UltimoIdFiltro = filtroId;
            UltimaDescricaoFiltro = descricao;
            UltimaCompetenciaFiltro = competencia;
            UltimaDataInicioFiltro = dataInicio;
            UltimaDataFimFiltro = dataFim;
            return Task.FromResult(FiltrarListagem(usuarioCadastroId, filtroId, descricao, competencia, dataInicio, dataFim));
        }

        private List<Despesa> FiltrarListagem(int? usuarioCadastroId, string? filtroId, string? descricao, string? competencia, DateOnly? dataInicio, DateOnly? dataFim)
        {
            var query = DespesasListadas.AsEnumerable();

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
        public Task<List<Despesa>> ObterPorIdsAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken = default) => Task.FromResult(new List<Despesa>());
        public Task<List<Despesa>> ObterPorIdsAsync(IReadOnlyCollection<long> ids, int usuarioCadastroId, CancellationToken cancellationToken = default) =>
            ObterPorIdsAsync(ids, cancellationToken);
        public Task<Despesa?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default) => Task.FromResult(Despesa);
        public Task<Despesa?> ObterPorIdAsync(long id, int usuarioCadastroId, CancellationToken cancellationToken = default) =>
            Task.FromResult(
                Despesa is not null &&
                Despesa.Id == id &&
                (!ValidarUsuarioNoObter || Despesa.UsuarioCadastroId == usuarioCadastroId)
                    ? Despesa
                    : null);
        public Task<Despesa> CriarAsync(Despesa despesa, CancellationToken cancellationToken = default)
        {
            DespesasCriadas.Add(despesa);
            return Task.FromResult(despesa);
        }
        public Task<Despesa> AtualizarAsync(Despesa despesa, CancellationToken cancellationToken = default)
        {
            DespesasAtualizadas.Add(despesa);

            var espelhoIndex = EspelhosPorOrigem.FindIndex(x => x.Id == despesa.Id);
            if (espelhoIndex >= 0)
            {
                EspelhosPorOrigem[espelhoIndex] = despesa;
            }

            return Task.FromResult(despesa);
        }
    }

    private sealed class UsuarioAutenticadoProviderFake(int? usuarioId) : IUsuarioAutenticadoProvider
    {
        public int? ObterUsuarioId() => usuarioId;
    }

    private sealed class HistoricoRepositoryFake : IHistoricoTransacaoFinanceiraRepository
    {
        public Task<HistoricoTransacaoFinanceira> CriarAsync(HistoricoTransacaoFinanceira historico, CancellationToken cancellationToken = default) =>
            Task.FromResult(historico);

        public Task<HistoricoTransacaoFinanceira?> ObterUltimoPorTransacaoAsync(TipoTransacaoFinanceira tipoTransacao, long transacaoId, CancellationToken cancellationToken = default) =>
            Task.FromResult<HistoricoTransacaoFinanceira?>(null);

        public Task<List<HistoricoTransacaoFinanceira>> ListarPorUsuarioAsync(int usuarioOperacaoId, int quantidadeRegistros, OrdemRegistrosHistoricoTransacaoFinanceira ordemRegistros, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<HistoricoTransacaoFinanceira>());

        public Task<List<HistoricoTransacaoFinanceira>> ListarPorUsuarioResumoAsync(int usuarioOperacaoId, int? ano, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<HistoricoTransacaoFinanceira>());

        public Task<List<HistoricoTransacaoFinanceira>> ListarPorContaBancariaCompetenciaAsync(long contaBancariaId, int usuarioOperacaoId, string? competencia, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<HistoricoTransacaoFinanceira>());

        public Task<List<HistoricoTransacaoFinanceira>> ListarPorCartaoCompetenciaAsync(long cartaoId, int usuarioOperacaoId, string? competencia, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<HistoricoTransacaoFinanceira>());
    }

    private sealed class AreaRepoFake : IAreaRepository
    {
        public List<Area> Areas { get; set; } = [];
        public List<SubArea> SubAreas { get; set; } = [];

        public Task<List<Area>> ListarComSubAreasAsync(TipoAreaFinanceira? tipo = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(Areas);

        public Task<List<SubArea>> ObterSubAreasPorIdsAsync(IReadOnlyCollection<long> subAreasIds, CancellationToken cancellationToken = default) =>
            Task.FromResult(SubAreas.Where(x => subAreasIds.Contains(x.Id)).ToList());

        public Task<List<AreaSubAreaRateioSoma>> ListarSomaRateioPorAreaSubAreaAsync(int usuarioId, TipoAreaFinanceira? tipo = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<AreaSubAreaRateioSoma>());
    }

    private sealed class ContaBancariaRepositoryFake : IContaBancariaRepository
    {
        public Task<List<ContaBancaria>> ListarAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<ContaBancaria>());

        public Task<List<ContaBancaria>> ListarAsync(int usuarioCadastroId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<ContaBancaria>());

        public Task<ContaBancaria?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default) =>
            Task.FromResult<ContaBancaria?>(new ContaBancaria { Id = id, UsuarioCadastroId = 1 });

        public Task<ContaBancaria?> ObterPorIdAsync(long id, int usuarioCadastroId, CancellationToken cancellationToken = default) =>
            Task.FromResult<ContaBancaria?>(new ContaBancaria { Id = id, UsuarioCadastroId = usuarioCadastroId });

        public Task<ContaBancaria> CriarAsync(ContaBancaria conta, CancellationToken cancellationToken = default) =>
            Task.FromResult(conta);

        public Task<ContaBancaria> AtualizarAsync(ContaBancaria conta, CancellationToken cancellationToken = default) =>
            Task.FromResult(conta);
    }

    private sealed class CartaoRepositoryFake : ICartaoRepository
    {
        public Task<List<Cartao>> ListarAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<Cartao>());

        public Task<List<Cartao>> ListarAsync(int usuarioCadastroId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<Cartao>());

        public Task<Cartao?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Cartao?>(new Cartao { Id = id, UsuarioCadastroId = 1 });

        public Task<Cartao?> ObterPorIdAsync(long id, int usuarioCadastroId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Cartao?>(new Cartao { Id = id, UsuarioCadastroId = usuarioCadastroId });

        public Task<Cartao> CriarAsync(Cartao cartao, CancellationToken cancellationToken = default) =>
            Task.FromResult(cartao);

        public Task<Cartao> AtualizarAsync(Cartao cartao, CancellationToken cancellationToken = default) =>
            Task.FromResult(cartao);
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

    private sealed class RecorrenciaPublisherFake : IRecorrenciaBackgroundPublisher
    {
        public DespesaRecorrenciaBackgroundMessage? DespesaMessage { get; private set; }

        public Task PublicarDespesaAsync(DespesaRecorrenciaBackgroundMessage message, CancellationToken cancellationToken = default)
        {
            DespesaMessage = message;
            return Task.CompletedTask;
        }

        public Task PublicarReceitaAsync(ReceitaRecorrenciaBackgroundMessage message, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class DocumentoStorageServiceFake : IDocumentoStorageService
    {
        public Task<IReadOnlyCollection<DocumentoDto>> SalvarAsync(IReadOnlyCollection<DocumentoRequest> documentos, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<DocumentoDto>>(
                documentos.Select(x => new DocumentoDto(x.NomeArquivo, $@"C:\temp\{x.NomeArquivo}", x.ContentType, 1)).ToArray());
    }
}







