using Core.Application.Contracts.Financeiro;
using Core.Application.DTOs.Financeiro;
using Core.Domain.Entities;
using Core.Domain.Entities.Financeiro;
using Core.Domain.Enums;
using Core.Domain.Exceptions;
using Core.Domain.Interfaces;
using Core.Domain.Interfaces.Administracao;
using Core.Domain.Interfaces.Financeiro;

namespace Core.Application.Services.Financeiro;

public sealed partial class DespesaService(
    IDespesaRepository repository,
    IContaBancariaRepository contaRepository,
    ICartaoRepository cartaoRepository,
    IAreaRepository areaRepository,
    IAmizadeRepository amizadeRepository,
    IUsuarioRepository usuarioRepository,
    IUsuarioAutenticadoProvider usuarioAutenticadoProvider,
    HistoricoTransacaoFinanceiraService historicoTransacaoFinanceiraService,
    IDocumentoStorageService documentoStorageService,
    IRecorrenciaBackgroundPublisher recorrenciaBackgroundPublisher)
{
    private static readonly HashSet<string> TiposDespesa = ["alimentacao", "transporte", "moradia", "lazer", "saude", "educacao", "servicos"];
    private static readonly HashSet<string> TiposPagamento = ["pix", "cartaoCredito", "cartaoDebito", "boleto", "transferencia", "dinheiro"];

    private sealed record AmigoRateioValidado(int AmigoId, string Nome, decimal Valor);

    public async Task<IReadOnlyCollection<DespesaListaDto>> ListarAsync(ListarDespesasRequest request, CancellationToken cancellationToken = default)
    {
        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        var dataInicio = request.DataInicio;
        var dataFim = request.DataFim;

        if (dataInicio.HasValue && dataFim.HasValue && dataFim.Value < dataInicio.Value)
            throw new DomainException("periodo_invalido");

        if (string.IsNullOrWhiteSpace(request.Competencia) && !dataInicio.HasValue && !dataFim.HasValue)
        {
            var periodoAtual = CompetenciaPeriodoHelper.Resolver(null, null, null);
            dataInicio = periodoAtual.DataInicio;
            dataFim = periodoAtual.DataFim;
        }

        return (await repository.ListarPorUsuarioAsync(
                usuarioAutenticadoId,
                request.Id,
                request.Descricao,
                request.Competencia,
                dataInicio,
                dataFim,
                cancellationToken))
            .Where(x => !(x.DespesaOrigemId.HasValue && (x.Status == StatusDespesa.PendenteAprovacao || x.Status == StatusDespesa.Rejeitado)))
            .Select(MapLista)
            .ToArray();
    }

    public async Task<IReadOnlyCollection<DespesaDto>> ListarPendentesAprovacaoAsync(CancellationToken cancellationToken = default)
    {
        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        return (await repository.ListarPendentesAprovacaoPorUsuarioAsync(usuarioAutenticadoId, cancellationToken))
            .Select(Map)
            .ToArray();
    }

    public async Task<DespesaDto> ObterAsync(long id, CancellationToken cancellationToken = default) =>
        Map(await repository.ObterPorIdAsync(id, ObterUsuarioAutenticadoId(), cancellationToken) ?? throw new NotFoundException("despesa_nao_encontrada"));

    public async Task<DespesaDto> CriarAsync(CriarDespesaRequest req, CancellationToken cancellationToken = default)
    {
        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        var quantidadeRecorrencia = ResolverQuantidadeRecorrencia(req.TipoPagamento, req.QuantidadeRecorrencia, req.QuantidadeParcelas);
        var recorrencia = ResolverRecorrencia(req.TipoPagamento, req.Recorrencia);
        var recorrenciaFixa = ResolverRecorrenciaFixa(req.TipoPagamento, req.RecorrenciaFixa);
        ValidarComum(req.Descricao, req.DataLancamento, req.DataVencimento, req.TipoDespesa, req.TipoPagamento, recorrencia, recorrenciaFixa, quantidadeRecorrencia, req.ValorTotal);
        await ValidarAreasRateioAsync(req.AreasSubAreasRateio ?? [], cancellationToken);
        var amigos = NormalizarAmigos(req.AmigosRateio);
        ValidarRateioAmigos(amigos, req.ValorTotal);
        ValidarRateioAreas(req.AreasSubAreasRateio ?? [], req.ValorTotal);
        var amigosValidados = await ValidarAmigosAceitosAsync(amigos, usuarioAutenticadoId, cancellationToken);
        var liquido = Liquido(req.ValorTotal, req.Desconto, req.Acrescimo, req.Imposto, req.Juros);
        var vinculo = await ResolverVinculoPagamentoAsync(req.TipoPagamento, req.Vinculo, null, null, usuarioAutenticadoId, cancellationToken);
        var documentos = await SalvarDocumentosAsync(req.Documentos ?? [], usuarioAutenticadoId, cancellationToken: cancellationToken);

        var despesa = new Despesa
        {
            Descricao = req.Descricao.Trim(),
            Observacao = req.Observacao,
            DataLancamento = req.DataLancamento,
            DataVencimento = req.DataVencimento,
            TipoDespesa = req.TipoDespesa,
            TipoPagamento = req.TipoPagamento,
            Recorrencia = recorrencia,
            RecorrenciaFixa = recorrenciaFixa,
            QuantidadeRecorrencia = quantidadeRecorrencia,
            ValorTotal = req.ValorTotal,
            ValorLiquido = liquido,
            Desconto = req.Desconto,
            Acrescimo = req.Acrescimo,
            Imposto = req.Imposto,
            Juros = req.Juros,
            ContaBancariaId = vinculo.ContaBancariaId,
            CartaoId = vinculo.CartaoId,
            UsuarioCadastroId = usuarioAutenticadoId,
            Status = StatusDespesa.Pendente,
            Documentos = documentos,
            AmigosRateio = amigosValidados.Select(x => new DespesaAmigoRateio
            {
                UsuarioCadastroId = usuarioAutenticadoId,
                AmigoId = x.AmigoId,
                AmigoNome = x.Nome,
                Valor = x.Valor
            }).ToList(),
            AreasRateio = (req.AreasSubAreasRateio ?? []).Select(x => new DespesaAreaRateio
            {
                UsuarioCadastroId = usuarioAutenticadoId,
                AreaId = x.AreaId,
                SubAreaId = x.SubAreaId,
                Valor = x.Valor
            }).ToList(),
            Logs = [new DespesaLog { UsuarioCadastroId = usuarioAutenticadoId, Acao = AcaoLogs.Cadastro, Descricao = "Despesa criada com status pendente." }]
        };

        var despesaCriada = await repository.CriarAsync(despesa, cancellationToken);
        await CriarEspelhosRateioAsync(despesaCriada, amigosValidados, req.AreasSubAreasRateio ?? [], cancellationToken);

        var alvo = recorrenciaFixa ? 100 : quantidadeRecorrencia.GetValueOrDefault(1);
        if (alvo > 1)
        {
            var mensagem = new DespesaRecorrenciaBackgroundMessage(
                usuarioAutenticadoId,
                req.Descricao.Trim(),
                req.Observacao,
                req.DataLancamento,
                req.DataVencimento,
                req.TipoDespesa,
                req.TipoPagamento,
                recorrencia,
                recorrenciaFixa,
                recorrenciaFixa ? 100 : quantidadeRecorrencia,
                req.ValorTotal,
                req.Desconto,
                req.Acrescimo,
                req.Imposto,
                req.Juros,
                vinculo.ContaBancariaId,
                vinculo.CartaoId,
                [],
                amigosValidados.Select(x => new RateioAmigoBackgroundMessage(x.AmigoId, x.Nome, x.Valor)).ToArray(),
                (req.AreasSubAreasRateio ?? []).Select(x => new RateioAreaBackgroundMessage(x.AreaId, x.SubAreaId, x.Valor)).ToArray());

            await recorrenciaBackgroundPublisher.PublicarDespesaAsync(mensagem, cancellationToken);
        }

        return Map(despesaCriada);
    }

    public async Task<DespesaDto> AtualizarAsync(long id, AtualizarDespesaRequest req, CancellationToken cancellationToken = default)
    {
        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        var despesa = await repository.ObterPorIdAsync(id, usuarioAutenticadoId, cancellationToken) ?? throw new NotFoundException("despesa_nao_encontrada");
        if (despesa.Status != StatusDespesa.Pendente) throw new DomainException("status_invalido");

        var quantidadeRecorrencia = ResolverQuantidadeRecorrencia(req.TipoPagamento, req.QuantidadeRecorrencia, req.QuantidadeParcelas);
        var recorrencia = ResolverRecorrencia(req.TipoPagamento, req.Recorrencia);
        var recorrenciaFixa = ResolverRecorrenciaFixa(req.TipoPagamento, req.RecorrenciaFixa);
        ValidarComum(req.Descricao, req.DataLancamento, req.DataVencimento, req.TipoDespesa, req.TipoPagamento, recorrencia, recorrenciaFixa, quantidadeRecorrencia, req.ValorTotal);
        await ValidarAreasRateioAsync(req.AreasSubAreasRateio ?? [], cancellationToken);
        var amigos = NormalizarAmigos(req.AmigosRateio);
        ValidarRateioAmigos(amigos, req.ValorTotal);
        ValidarRateioAreas(req.AreasSubAreasRateio ?? [], req.ValorTotal);
        var amigosValidados = await ValidarAmigosAceitosAsync(amigos, usuarioAutenticadoId, cancellationToken);
        var liquido = Liquido(req.ValorTotal, req.Desconto, req.Acrescimo, req.Imposto, req.Juros);
        var vinculo = await ResolverVinculoPagamentoAsync(req.TipoPagamento, req.Vinculo, despesa.ContaBancariaId, despesa.CartaoId, usuarioAutenticadoId, cancellationToken);

        despesa.Descricao = req.Descricao.Trim();
        despesa.Observacao = req.Observacao;
        despesa.DataLancamento = req.DataLancamento;
        despesa.DataVencimento = req.DataVencimento;
        despesa.TipoDespesa = req.TipoDespesa;
        despesa.TipoPagamento = req.TipoPagamento;
        despesa.Recorrencia = recorrencia;
        despesa.RecorrenciaFixa = recorrenciaFixa;
        despesa.QuantidadeRecorrencia = quantidadeRecorrencia;
        despesa.ValorTotal = req.ValorTotal;
        despesa.ValorLiquido = liquido;
        despesa.Desconto = req.Desconto;
        despesa.Acrescimo = req.Acrescimo;
        despesa.Imposto = req.Imposto;
        despesa.Juros = req.Juros;
        despesa.ContaBancariaId = vinculo.ContaBancariaId;
        despesa.CartaoId = vinculo.CartaoId;
        if (req.Documentos is not null)
            despesa.Documentos = await SalvarDocumentosAsync(req.Documentos, usuarioAutenticadoId, despesa.Id, cancellationToken: cancellationToken);

        despesa.AmigosRateio = amigosValidados.Select(x => new DespesaAmigoRateio
        {
            DespesaId = despesa.Id,
            UsuarioCadastroId = usuarioAutenticadoId,
            AmigoId = x.AmigoId,
            AmigoNome = x.Nome,
            Valor = x.Valor
        }).ToList();
        despesa.AreasRateio = (req.AreasSubAreasRateio ?? []).Select(x => new DespesaAreaRateio
        {
            DespesaId = despesa.Id,
            UsuarioCadastroId = usuarioAutenticadoId,
            AreaId = x.AreaId,
            SubAreaId = x.SubAreaId,
            Valor = x.Valor
        }).ToList();
        despesa.Logs.Add(new DespesaLog { DespesaId = despesa.Id, UsuarioCadastroId = usuarioAutenticadoId, Acao = AcaoLogs.Atualizacao, Descricao = "Despesa atualizada." });

        var despesaAtualizada = await repository.AtualizarAsync(despesa, cancellationToken);
        await SincronizarEspelhosRateioAsync(despesaAtualizada, amigosValidados, req.AreasSubAreasRateio ?? [], cancellationToken);
        return Map(despesaAtualizada);
    }

    public async Task<DespesaDto> AprovarRateioAsync(long id, CancellationToken cancellationToken = default)
    {
        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        var despesa = await repository.ObterPorIdAsync(id, usuarioAutenticadoId, cancellationToken) ?? throw new NotFoundException("despesa_nao_encontrada");
        if (!despesa.DespesaOrigemId.HasValue) throw new DomainException("aprovacao_invalida");
        if (despesa.Status != StatusDespesa.PendenteAprovacao) throw new DomainException("status_invalido");

        despesa.Status = StatusDespesa.Pendente;
        despesa.Logs.Add(new DespesaLog
        {
            DespesaId = despesa.Id,
            UsuarioCadastroId = usuarioAutenticadoId,
            Acao = AcaoLogs.Atualizacao,
            Descricao = "Rateio aprovado pelo amigo."
        });

        return Map(await repository.AtualizarAsync(despesa, cancellationToken));
    }

    public async Task<DespesaDto> RejeitarRateioAsync(long id, CancellationToken cancellationToken = default)
    {
        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        var despesa = await repository.ObterPorIdAsync(id, usuarioAutenticadoId, cancellationToken) ?? throw new NotFoundException("despesa_nao_encontrada");
        if (!despesa.DespesaOrigemId.HasValue) throw new DomainException("aprovacao_invalida");
        if (despesa.Status != StatusDespesa.PendenteAprovacao) throw new DomainException("status_invalido");

        despesa.Status = StatusDespesa.Rejeitado;
        despesa.Logs.Add(new DespesaLog
        {
            DespesaId = despesa.Id,
            UsuarioCadastroId = usuarioAutenticadoId,
            Acao = AcaoLogs.Atualizacao,
            Descricao = "Rateio rejeitado pelo amigo."
        });

        return Map(await repository.AtualizarAsync(despesa, cancellationToken));
    }

    public async Task<DespesaDto> EfetivarAsync(long id, EfetivarDespesaRequest req, CancellationToken cancellationToken = default)
    {
        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        var despesa = await repository.ObterPorIdAsync(id, usuarioAutenticadoId, cancellationToken) ?? throw new NotFoundException("despesa_nao_encontrada");
        if (despesa.Status != StatusDespesa.Pendente) throw new DomainException("status_invalido");
        if (string.IsNullOrWhiteSpace(req.TipoPagamento) || req.ValorTotal <= 0) throw new DomainException("dados_invalidos");
        if (req.DataEfetivacao < despesa.DataLancamento) throw new DomainException("periodo_invalido");
        if (req.ContaBancariaId.HasValue && req.CartaoId.HasValue) throw new DomainException("forma_pagamento_invalida");
        var vinculo = await ResolverVinculoPagamentoAsync(
            req.TipoPagamento,
            req.Vinculo,
            req.ContaBancariaId ?? despesa.ContaBancariaId,
            req.CartaoId ?? despesa.CartaoId,
            usuarioAutenticadoId,
            cancellationToken);

        var liquido = Liquido(req.ValorTotal, req.Desconto, req.Acrescimo, req.Imposto, req.Juros);
        var valorAntesTransacao = despesa.ValorEfetivacao ?? 0m;
        despesa.DataEfetivacao = req.DataEfetivacao;
        despesa.TipoPagamento = req.TipoPagamento;
        despesa.ValorTotal = req.ValorTotal;
        despesa.Desconto = req.Desconto;
        despesa.Acrescimo = req.Acrescimo;
        despesa.Imposto = req.Imposto;
        despesa.Juros = req.Juros;
        despesa.ContaBancariaId = vinculo.ContaBancariaId;
        despesa.CartaoId = vinculo.CartaoId;
        despesa.ValorLiquido = liquido;
        despesa.ValorEfetivacao = liquido;
        despesa.Status = StatusDespesa.Efetivada;
        if (req.Documentos is not null)
            despesa.Documentos = await SalvarDocumentosAsync(req.Documentos, usuarioAutenticadoId, despesa.Id, cancellationToken: cancellationToken);
        despesa.Logs.Add(new DespesaLog { DespesaId = despesa.Id, UsuarioCadastroId = usuarioAutenticadoId, Acao = AcaoLogs.Atualizacao, Descricao = "Despesa efetivada." });

        var despesaAtualizada = await repository.AtualizarAsync(despesa, cancellationToken);
        await historicoTransacaoFinanceiraService.RegistrarEfetivacaoAsync(
            TipoTransacaoFinanceira.Despesa,
            despesaAtualizada.Id,
            usuarioAutenticadoId,
            req.DataEfetivacao,
            valorAntesTransacao,
            despesaAtualizada.ValorEfetivacao ?? despesaAtualizada.ValorLiquido,
            despesaAtualizada.ValorEfetivacao ?? despesaAtualizada.ValorLiquido,
            "Efetivacao de despesa",
            despesaAtualizada.TipoPagamento,
            despesaAtualizada.ContaBancariaId,
            despesaAtualizada.CartaoId,
            cancellationToken);

        return Map(despesaAtualizada);
    }

    public async Task<DespesaDto> CancelarAsync(long id, CancellationToken cancellationToken = default)
    {
        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        var despesa = await repository.ObterPorIdAsync(id, usuarioAutenticadoId, cancellationToken) ?? throw new NotFoundException("despesa_nao_encontrada");
        if (despesa.Status != StatusDespesa.Pendente) throw new DomainException("status_invalido");
        despesa.Status = StatusDespesa.Cancelada;
        despesa.Logs.Add(new DespesaLog { DespesaId = despesa.Id, UsuarioCadastroId = usuarioAutenticadoId, Acao = AcaoLogs.Exclusao, Descricao = "Despesa cancelada." });
        return Map(await repository.AtualizarAsync(despesa, cancellationToken));
    }

    public async Task<DespesaDto> EstornarAsync(long id, CancellationToken cancellationToken = default)
    {
        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        var despesa = await repository.ObterPorIdAsync(id, usuarioAutenticadoId, cancellationToken) ?? throw new NotFoundException("despesa_nao_encontrada");
        if (despesa.Status != StatusDespesa.Efetivada) throw new DomainException("status_invalido");
        var valorAntesTransacao = despesa.ValorEfetivacao ?? despesa.ValorLiquido;
        despesa.Status = StatusDespesa.Pendente;
        despesa.DataEfetivacao = null;
        despesa.ValorEfetivacao = null;
        despesa.Logs.Add(new DespesaLog { DespesaId = despesa.Id, UsuarioCadastroId = usuarioAutenticadoId, Acao = AcaoLogs.Atualizacao, Descricao = "Despesa estornada." });
        var despesaAtualizada = await repository.AtualizarAsync(despesa, cancellationToken);
        await historicoTransacaoFinanceiraService.RegistrarEstornoAsync(
            TipoTransacaoFinanceira.Despesa,
            despesaAtualizada.Id,
            usuarioAutenticadoId,
            DateOnly.FromDateTime(DateTime.UtcNow),
            valorAntesTransacao,
            valorAntesTransacao,
            0m,
            "Estorno de despesa",
            despesa.TipoPagamento,
            cancellationToken: cancellationToken);

        return Map(despesaAtualizada);
    }

    private int ObterUsuarioAutenticadoId() =>
        usuarioAutenticadoProvider.ObterUsuarioId() ?? throw new DomainException("usuario_nao_autenticado");

    private static void ValidarComum(string descricao, DateOnly dataLanc, DateOnly dataVenc, string tipoDespesa, string tipoPagamento, Recorrencia recorrencia, bool recorrenciaFixa, int? quantidadeRecorrencia, decimal valorTotal)
    {
        if (string.IsNullOrWhiteSpace(descricao)) throw new DomainException("descricao_obrigatoria");
        if (valorTotal <= 0) throw new DomainException("valor_total_invalido");
        if (dataVenc < dataLanc) throw new DomainException("periodo_invalido");
        if (!TiposDespesa.Contains(tipoDespesa) || !TiposPagamento.Contains(tipoPagamento) || !Enum.IsDefined(recorrencia)) throw new DomainException("enum_invalida");
        if (PagamentoCartao(tipoPagamento) && recorrenciaFixa) throw new DomainException("recorrencia_fixa_invalida");
        if (recorrenciaFixa && recorrencia == Recorrencia.Unica) throw new DomainException("recorrencia_fixa_invalida");
        if (!recorrenciaFixa && recorrencia is not Recorrencia.Unica && (!quantidadeRecorrencia.HasValue || quantidadeRecorrencia <= 0))
            throw new DomainException("quantidade_recorrencia_invalida");
        if (recorrenciaFixa && quantidadeRecorrencia.HasValue && quantidadeRecorrencia <= 0)
            throw new DomainException("quantidade_recorrencia_invalida");
        if (!recorrenciaFixa && quantidadeRecorrencia.HasValue && quantidadeRecorrencia > 100)
            throw new DomainException("quantidade_recorrencia_invalida");
    }

    private static bool PagamentoCartao(string tipoPagamento) =>
        tipoPagamento is "cartaoCredito" or "cartaoDebito";

    private static bool ContaObrigatoria(string tipoPagamento) =>
        tipoPagamento is "pix" or "transferencia";

    private static int? ResolverQuantidadeRecorrencia(string tipoPagamento, int? quantidadeRecorrencia, int? quantidadeParcelas)
    {
        if (!PagamentoCartao(tipoPagamento))
            return quantidadeRecorrencia;

        var parcelas = quantidadeParcelas ?? quantidadeRecorrencia;
        if (!parcelas.HasValue || parcelas <= 0)
            throw new DomainException("quantidade_parcelas_invalida");

        return parcelas;
    }

    private static Recorrencia ResolverRecorrencia(string tipoPagamento, Recorrencia recorrencia) =>
        PagamentoCartao(tipoPagamento) ? Recorrencia.Mensal : recorrencia;

    private static bool ResolverRecorrenciaFixa(string tipoPagamento, bool recorrenciaFixa) =>
        PagamentoCartao(tipoPagamento) ? false : recorrenciaFixa;

    private static decimal Liquido(decimal valorTotal, decimal desconto, decimal acrescimo, decimal imposto, decimal juros) =>
        valorTotal - desconto + acrescimo + imposto + juros;

    private async Task<(long? ContaBancariaId, long? CartaoId)> ResolverVinculoPagamentoAsync(
        string tipoPagamento,
        MeioFinanceiroVinculoRequest? vinculo,
        long? contaBancariaIdLegado,
        long? cartaoIdLegado,
        int usuarioAutenticadoId,
        CancellationToken cancellationToken)
    {
        var contaBancariaId = vinculo?.ContaBancariaId ?? contaBancariaIdLegado;
        var cartaoId = vinculo?.CartaoId ?? cartaoIdLegado;

        if (contaBancariaId.HasValue && cartaoId.HasValue)
            throw new DomainException("forma_pagamento_invalida");

        if (PagamentoCartao(tipoPagamento) && contaBancariaId.HasValue)
            throw new DomainException("forma_pagamento_invalida");

        if (PagamentoCartao(tipoPagamento) && !cartaoId.HasValue)
            throw new DomainException("cartao_obrigatorio");

        if (!PagamentoCartao(tipoPagamento) && cartaoId.HasValue)
            throw new DomainException("forma_pagamento_invalida");

        if (ContaObrigatoria(tipoPagamento) && !contaBancariaId.HasValue)
            throw new DomainException("conta_bancaria_obrigatoria");

        if (contaBancariaId.HasValue &&
            await contaRepository.ObterPorIdAsync(contaBancariaId.Value, usuarioAutenticadoId, cancellationToken) is null)
            throw new DomainException("conta_bancaria_invalida");

        if (cartaoId.HasValue &&
            await cartaoRepository.ObterPorIdAsync(cartaoId.Value, usuarioAutenticadoId, cancellationToken) is null)
            throw new DomainException("cartao_invalido");

        return (contaBancariaId, cartaoId);
    }

    private async Task ValidarAreasRateioAsync(IReadOnlyCollection<DespesaAreaRateioRequest> areasRateio, CancellationToken cancellationToken)
    {
        if (areasRateio.Count == 0) return;

        if (areasRateio.Any(x => x.AreaId <= 0 || x.SubAreaId <= 0))
            throw new DomainException("area_subarea_invalida");

        var subAreas = await areaRepository.ObterSubAreasPorIdsAsync(areasRateio.Select(x => x.SubAreaId).Distinct().ToArray(), cancellationToken);
        var subAreasById = subAreas.ToDictionary(x => x.Id);

        foreach (var item in areasRateio)
        {
            if (!subAreasById.TryGetValue(item.SubAreaId, out var subArea) || subArea.AreaId != item.AreaId)
                throw new DomainException("relacao_area_subarea_invalida");

            if (subArea.Area.Tipo != TipoAreaFinanceira.Despesa)
                throw new DomainException("area_subarea_invalida");
        }
    }

    private static void ValidarRateioAmigos(IReadOnlyCollection<AmigoRateioRequest> amigos, decimal valorTotal)
    {
        if (amigos.Count == 0) return;

        if (amigos.Any(x => !x.Valor.HasValue || x.Valor <= 0))
            throw new DomainException("rateio_amigos_invalido");

        if (amigos.Sum(x => x.Valor!.Value) != valorTotal)
            throw new DomainException("rateio_amigos_invalido");
    }

    private static void ValidarRateioAreas(IReadOnlyCollection<DespesaAreaRateioRequest> areasRateio, decimal valorTotal)
    {
        if (areasRateio.Count == 0) return;

        if (areasRateio.Any(x => !x.Valor.HasValue || x.Valor <= 0))
            throw new DomainException("rateio_area_invalido");

        if (areasRateio.Sum(x => x.Valor!.Value) != valorTotal)
            throw new DomainException("rateio_area_invalido");
    }

    private static IReadOnlyCollection<AmigoRateioRequest> NormalizarAmigos(IReadOnlyCollection<AmigoRateioRequest>? amigosRateio)
    {
        if (amigosRateio is null || amigosRateio.Count == 0)
            return [];

        var normalizados = amigosRateio.Where(x => x.AmigoId > 0).ToArray();

        if (normalizados.Length != amigosRateio.Count || normalizados.Select(x => x.AmigoId).Distinct().Count() != normalizados.Length)
            throw new DomainException("rateio_amigos_invalido");

        return normalizados;
    }

    private async Task<IReadOnlyCollection<AmigoRateioValidado>> ValidarAmigosAceitosAsync(
        IReadOnlyCollection<AmigoRateioRequest> amigos,
        int usuarioAutenticadoId,
        CancellationToken cancellationToken)
    {
        if (amigos.Count == 0)
            return [];

        var amigosIdsAceitos = await amizadeRepository.ListarIdsAmigosAceitosAsync(usuarioAutenticadoId, cancellationToken);
        var amigosAceitos = amigosIdsAceitos.ToHashSet();

        if (amigos.Any(x => !amigosAceitos.Contains(x.AmigoId)))
            throw new DomainException("amigo_rateio_invalido");

        var usuariosAtivos = await usuarioRepository.ListarAtivosAsync(cancellationToken);
        var usuariosPorId = usuariosAtivos.ToDictionary(x => x.Id);

        if (amigos.Any(x => !usuariosPorId.ContainsKey(x.AmigoId)))
            throw new DomainException("amigo_rateio_invalido");

        return amigos
            .Select(x => new AmigoRateioValidado(x.AmigoId, usuariosPorId[x.AmigoId].Nome, x.Valor!.Value))
            .ToArray();
    }

    private async Task CriarEspelhosRateioAsync(
        Despesa origem,
        IReadOnlyCollection<AmigoRateioValidado> amigos,
        IReadOnlyCollection<DespesaAreaRateioRequest> areasRateioOrigem,
        CancellationToken cancellationToken)
    {
        if (amigos.Count == 0)
            return;

        foreach (var amigo in amigos)
        {
            var espelho = CriarEspelhoDespesa(origem, amigo, areasRateioOrigem);
            await repository.CriarAsync(espelho, cancellationToken);
        }
    }

    private async Task SincronizarEspelhosRateioAsync(
        Despesa origem,
        IReadOnlyCollection<AmigoRateioValidado> amigos,
        IReadOnlyCollection<DespesaAreaRateioRequest> areasRateioOrigem,
        CancellationToken cancellationToken)
    {
        var espelhos = await repository.ListarEspelhosPorOrigemAsync(origem.Id, cancellationToken);
        if (espelhos.Count == 0 && amigos.Count == 0)
            return;

        var amigosIds = amigos.Select(x => x.AmigoId).ToHashSet();

        foreach (var espelho in espelhos.Where(x => !amigosIds.Contains(x.UsuarioCadastroId) && x.Status != StatusDespesa.Cancelada))
        {
            espelho.Status = StatusDespesa.Cancelada;
            espelho.DataEfetivacao = null;
            espelho.ValorEfetivacao = null;
            espelho.Logs.Add(new DespesaLog
            {
                DespesaId = espelho.Id,
                UsuarioCadastroId = origem.UsuarioCadastroId,
                Acao = AcaoLogs.Atualizacao,
                Descricao = "Rateio removido pelo autor e espelho cancelado."
            });
            await repository.AtualizarAsync(espelho, cancellationToken);
        }

        foreach (var amigo in amigos)
        {
            var espelho = espelhos
                .Where(x => x.UsuarioCadastroId == amigo.AmigoId)
                .OrderByDescending(x => x.Id)
                .FirstOrDefault();

            if (espelho is null || espelho.Status == StatusDespesa.Cancelada)
            {
                await repository.CriarAsync(CriarEspelhoDespesa(origem, amigo, areasRateioOrigem), cancellationToken);
                continue;
            }

            if (espelho.Status == StatusDespesa.PendenteAprovacao || espelho.Status == StatusDespesa.Rejeitado)
            {
                AplicarSnapshotNoEspelho(espelho, origem, amigo, areasRateioOrigem);
                espelho.Status = StatusDespesa.PendenteAprovacao;
                espelho.Logs.Add(new DespesaLog
                {
                    DespesaId = espelho.Id,
                    UsuarioCadastroId = origem.UsuarioCadastroId,
                    Acao = AcaoLogs.Atualizacao,
                    Descricao = "Rateio reenviado para aprovacao."
                });
                await repository.AtualizarAsync(espelho, cancellationToken);
            }
        }
    }

    private static Despesa CriarEspelhoDespesa(
        Despesa origem,
        AmigoRateioValidado amigo,
        IReadOnlyCollection<DespesaAreaRateioRequest> areasRateioOrigem)
    {
        return new Despesa
        {
            DespesaOrigemId = origem.Id,
            UsuarioCadastroId = amigo.AmigoId,
            Descricao = origem.Descricao,
            Observacao = origem.Observacao,
            DataLancamento = origem.DataLancamento,
            DataVencimento = origem.DataVencimento,
            TipoDespesa = origem.TipoDespesa,
            TipoPagamento = origem.TipoPagamento,
            Recorrencia = origem.Recorrencia,
            RecorrenciaFixa = origem.RecorrenciaFixa,
            QuantidadeRecorrencia = origem.QuantidadeRecorrencia,
            ContaBancariaId = origem.ContaBancariaId,
            CartaoId = origem.CartaoId,
            ValorTotal = amigo.Valor,
            ValorLiquido = amigo.Valor,
            Desconto = 0m,
            Acrescimo = 0m,
            Imposto = 0m,
            Juros = 0m,
            Status = StatusDespesa.PendenteAprovacao,
            Documentos = [],
            AmigosRateio = [],
            AreasRateio = DistribuirAreasProporcionalmente(areasRateioOrigem, origem.ValorTotal, amigo.Valor, amigo.AmigoId),
            Logs =
            [
                new DespesaLog
                {
                    UsuarioCadastroId = origem.UsuarioCadastroId,
                    Acao = AcaoLogs.Cadastro,
                    Descricao = "Despesa compartilhada aguardando aprovacao."
                }
            ]
        };
    }

    private static void AplicarSnapshotNoEspelho(
        Despesa espelho,
        Despesa origem,
        AmigoRateioValidado amigo,
        IReadOnlyCollection<DespesaAreaRateioRequest> areasRateioOrigem)
    {
        espelho.Descricao = origem.Descricao;
        espelho.Observacao = origem.Observacao;
        espelho.DataLancamento = origem.DataLancamento;
        espelho.DataVencimento = origem.DataVencimento;
        espelho.TipoDespesa = origem.TipoDespesa;
        espelho.TipoPagamento = origem.TipoPagamento;
        espelho.Recorrencia = origem.Recorrencia;
        espelho.RecorrenciaFixa = origem.RecorrenciaFixa;
        espelho.QuantidadeRecorrencia = origem.QuantidadeRecorrencia;
        espelho.ContaBancariaId = origem.ContaBancariaId;
        espelho.CartaoId = origem.CartaoId;
        espelho.ValorTotal = amigo.Valor;
        espelho.ValorLiquido = amigo.Valor;
        espelho.Desconto = 0m;
        espelho.Acrescimo = 0m;
        espelho.Imposto = 0m;
        espelho.Juros = 0m;
        espelho.DataEfetivacao = null;
        espelho.ValorEfetivacao = null;
        espelho.AreasRateio = DistribuirAreasProporcionalmente(areasRateioOrigem, origem.ValorTotal, amigo.Valor, espelho.UsuarioCadastroId, espelho.Id);
    }

    private static List<DespesaAreaRateio> DistribuirAreasProporcionalmente(
        IReadOnlyCollection<DespesaAreaRateioRequest> areasRateioOrigem,
        decimal valorTotalOrigem,
        decimal valorEspelho,
        int usuarioCadastroId,
        long? despesaId = null)
    {
        if (areasRateioOrigem.Count == 0 || valorTotalOrigem <= 0 || valorEspelho <= 0)
            return [];

        var areas = areasRateioOrigem
            .Where(x => x.Valor.HasValue && x.Valor > 0)
            .ToArray();

        if (areas.Length == 0)
            return [];

        var resultado = new List<DespesaAreaRateio>(areas.Length);
        var acumulado = 0m;
        var fator = valorEspelho / valorTotalOrigem;

        for (var i = 0; i < areas.Length; i++)
        {
            var item = areas[i];
            var valor = i == areas.Length - 1
                ? Math.Round(valorEspelho - acumulado, 2, MidpointRounding.AwayFromZero)
                : Math.Round(item.Valor!.Value * fator, 2, MidpointRounding.AwayFromZero);

            if (valor < 0)
                valor = 0;

            acumulado += valor;
            resultado.Add(new DespesaAreaRateio
            {
                DespesaId = despesaId ?? 0,
                UsuarioCadastroId = usuarioCadastroId,
                AreaId = item.AreaId,
                SubAreaId = item.SubAreaId,
                Valor = valor
            });
        }

        return resultado;
    }

    private async Task<List<Documento>> SalvarDocumentosAsync(
        IReadOnlyCollection<DocumentoRequest> documentos,
        int usuarioAutenticadoId,
        long? despesaId = null,
        long? receitaId = null,
        long? reembolsoId = null,
        CancellationToken cancellationToken = default)
    {
        if (documentos.Count == 0)
            return [];

        var salvos = await documentoStorageService.SalvarAsync(documentos, cancellationToken);
        return salvos.Select(x => new Documento
        {
            UsuarioCadastroId = usuarioAutenticadoId,
            NomeArquivo = x.NomeArquivo,
            CaminhoArquivo = x.Caminho,
            ContentType = x.ContentType,
            TamanhoBytes = x.TamanhoBytes,
            DespesaId = despesaId,
            ReceitaId = receitaId,
            ReembolsoId = reembolsoId
        }).ToList();
    }

    private static DespesaListaDto MapLista(Despesa despesa) =>
        new(
            despesa.Id,
            despesa.Descricao,
            despesa.DataLancamento,
            despesa.DataVencimento,
            despesa.DataEfetivacao,
            despesa.TipoDespesa,
            despesa.TipoPagamento,
            despesa.ValorTotal,
            despesa.ValorLiquido,
            despesa.ValorEfetivacao,
            despesa.Status.ToString().ToLowerInvariant(),
            new MeioFinanceiroVinculoDto(despesa.ContaBancariaId, despesa.CartaoId));

    private static DespesaDto Map(Despesa despesa) =>
        new(
            despesa.Id,
            despesa.Descricao,
            despesa.Observacao,
            despesa.DataLancamento,
            despesa.DataVencimento,
            despesa.DataEfetivacao,
            despesa.TipoDespesa,
            despesa.TipoPagamento,
            despesa.Recorrencia,
            despesa.QuantidadeRecorrencia,
            despesa.RecorrenciaFixa,
            despesa.ValorTotal,
            despesa.ValorLiquido,
            despesa.Desconto,
            despesa.Acrescimo,
            despesa.Imposto,
            despesa.Juros,
            despesa.ValorEfetivacao,
            despesa.Status.ToString().ToLowerInvariant(),
            despesa.AmigosRateio.Select(x => new AmigoRateioDto(x.AmigoId, x.AmigoNome, x.Valor)).ToArray(),
            despesa.AreasRateio.Select(x => new DespesaAreaRateioDto(
                x.AreaId,
                x.Area?.Nome ?? string.Empty,
                x.SubAreaId,
                x.SubArea?.Nome ?? string.Empty,
                x.Valor)).ToArray(),
            despesa.Documentos.Select(x => new DocumentoDto(x.NomeArquivo, x.CaminhoArquivo, x.ContentType, x.TamanhoBytes)).ToArray(),
            new MeioFinanceiroVinculoDto(despesa.ContaBancariaId, despesa.CartaoId),
            despesa.Logs.Select(x => new DespesaLogDto(x.Id, DateOnly.FromDateTime(x.DataHoraCadastro), x.Acao, x.Descricao)).ToArray());
}
