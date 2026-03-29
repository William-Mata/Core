using Core.Application.DTOs.Financeiro;
using Core.Application.Contracts.Financeiro;
using Core.Domain.Entities;
using Core.Domain.Entities.Financeiro;
using Core.Domain.Enums;
using Core.Domain.Exceptions;
using Core.Domain.Interfaces;
using Core.Domain.Interfaces.Financeiro;

namespace Core.Application.Services.Financeiro;

public sealed class DespesaService(
    IDespesaRepository repository,
    IAreaRepository areaRepository,
    IUsuarioAutenticadoProvider usuarioAutenticadoProvider,
    HistoricoTransacaoFinanceiraService historicoTransacaoFinanceiraService,
    IRecorrenciaBackgroundPublisher recorrenciaBackgroundPublisher)
{
    private static readonly HashSet<string> TiposDespesa = ["alimentacao","transporte","moradia","lazer","saude","educacao","servicos"];
    private static readonly HashSet<string> TiposPagamento = ["pix","cartaoCredito","cartaoDebito","boleto","transferencia","dinheiro"];

    public async Task<IReadOnlyCollection<DespesaDto>> ListarAsync(CancellationToken cancellationToken = default) =>
        (await repository.ListarAsync(cancellationToken)).Select(Map).ToArray();

    public async Task<DespesaDto> ObterAsync(long id, CancellationToken cancellationToken = default) =>
        Map(await repository.ObterPorIdAsync(id, cancellationToken) ?? throw new NotFoundException("despesa_nao_encontrada"));

    public async Task<DespesaDto> CriarAsync(CriarDespesaRequest req, CancellationToken cancellationToken = default)
    {
        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        var quantidadeRecorrencia = ResolverQuantidadeRecorrencia(req.TipoPagamento, req.QuantidadeRecorrencia, req.QuantidadeParcelas);
        var recorrencia = ResolverRecorrencia(req.TipoPagamento, req.Recorrencia);
        var recorrenciaFixa = ResolverRecorrenciaFixa(req.TipoPagamento, req.RecorrenciaFixa);
        ValidarComum(req.Descricao, req.DataLancamento, req.DataVencimento, req.TipoDespesa, req.TipoPagamento, recorrencia, recorrenciaFixa, quantidadeRecorrencia, req.ValorTotal);
        await ValidarAreasRateioAsync(req.AreasRateio ?? [], cancellationToken);
        var amigos = NormalizarAmigos(req.Amigos, req.AmigosRateio, req.RateioAmigosValores);
        var liquido = Liquido(req.ValorTotal, req.Desconto, req.Acrescimo, req.Imposto, req.Juros);

        var d = new Despesa
        {
            Descricao = req.Descricao.Trim(), Observacao = req.Observacao, DataLancamento = req.DataLancamento, DataVencimento = req.DataVencimento,
            TipoDespesa = req.TipoDespesa, TipoPagamento = req.TipoPagamento, Recorrencia = recorrencia,
            RecorrenciaFixa = recorrenciaFixa,
            QuantidadeRecorrencia = quantidadeRecorrencia,
            ValorTotal = req.ValorTotal, ValorLiquido = liquido, Desconto = req.Desconto, Acrescimo = req.Acrescimo, Imposto = req.Imposto, Juros = req.Juros,
            UsuarioCadastroId = usuarioAutenticadoId,
            Status = StatusDespesa.Pendente, AnexoDocumento = req.AnexoDocumento,
            AmigosRateio = amigos.Select(x => new DespesaAmigoRateio { UsuarioCadastroId = usuarioAutenticadoId, AmigoNome = x.Nome, Valor = x.Valor }).ToList(),
            AreasRateio = (req.AreasRateio ?? []).Select(x => new DespesaAreaRateio { UsuarioCadastroId = usuarioAutenticadoId, AreaId = x.AreaId, SubAreaId = x.SubAreaId, Valor = x.Valor }).ToList(),
            TiposRateio = req.TiposRateio.Select(x => new DespesaTipoRateio { UsuarioCadastroId = usuarioAutenticadoId, TipoRateio = x }).ToList(),
            Logs = [new DespesaLog { UsuarioCadastroId = usuarioAutenticadoId, Acao = AcaoLogs.Cadastro, Descricao = "Despesa criada com status pendente." }]
        };

        var despesaCriada = await repository.CriarAsync(d, cancellationToken);

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
                req.AnexoDocumento,
                req.TiposRateio.ToArray(),
                amigos.Select(x => new RateioAmigoBackgroundMessage(x.Nome, x.Valor)).ToArray(),
                (req.AreasRateio ?? []).Select(x => new RateioAreaBackgroundMessage(x.AreaId, x.SubAreaId, x.Valor)).ToArray());

            await recorrenciaBackgroundPublisher.PublicarDespesaAsync(mensagem, cancellationToken);
        }

        return Map(despesaCriada);
    }

    public async Task<DespesaDto> AtualizarAsync(long id, AtualizarDespesaRequest req, CancellationToken cancellationToken = default)
    {
        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        var d = await repository.ObterPorIdAsync(id, cancellationToken) ?? throw new NotFoundException("despesa_nao_encontrada");
        if (d.Status != StatusDespesa.Pendente) throw new DomainException("status_invalido");

        var quantidadeRecorrencia = ResolverQuantidadeRecorrencia(req.TipoPagamento, req.QuantidadeRecorrencia, req.QuantidadeParcelas);
        var recorrencia = ResolverRecorrencia(req.TipoPagamento, req.Recorrencia);
        var recorrenciaFixa = ResolverRecorrenciaFixa(req.TipoPagamento, req.RecorrenciaFixa);
        ValidarComum(req.Descricao, req.DataLancamento, req.DataVencimento, req.TipoDespesa, req.TipoPagamento, recorrencia, recorrenciaFixa, quantidadeRecorrencia, req.ValorTotal);
        await ValidarAreasRateioAsync(req.AreasRateio ?? [], cancellationToken);
        var amigos = NormalizarAmigos(req.Amigos, req.AmigosRateio, req.RateioAmigosValores);
        var liquido = Liquido(req.ValorTotal, req.Desconto, req.Acrescimo, req.Imposto, req.Juros);

        d.Descricao = req.Descricao.Trim(); d.Observacao = req.Observacao; d.DataLancamento = req.DataLancamento; d.DataVencimento = req.DataVencimento;
        d.TipoDespesa = req.TipoDespesa; d.TipoPagamento = req.TipoPagamento; d.Recorrencia = recorrencia; d.RecorrenciaFixa = recorrenciaFixa; d.QuantidadeRecorrencia = quantidadeRecorrencia;
        d.ValorTotal = req.ValorTotal; d.ValorLiquido = liquido; d.Desconto = req.Desconto; d.Acrescimo = req.Acrescimo; d.Imposto = req.Imposto; d.Juros = req.Juros;
        d.AnexoDocumento = req.AnexoDocumento;
        d.AmigosRateio = amigos.Select(x => new DespesaAmigoRateio { DespesaId = d.Id, UsuarioCadastroId = usuarioAutenticadoId, AmigoNome = x.Nome, Valor = x.Valor }).ToList();
        d.AreasRateio = (req.AreasRateio ?? []).Select(x => new DespesaAreaRateio { DespesaId = d.Id, UsuarioCadastroId = usuarioAutenticadoId, AreaId = x.AreaId, SubAreaId = x.SubAreaId, Valor = x.Valor }).ToList();
        d.TiposRateio = req.TiposRateio.Select(x => new DespesaTipoRateio { DespesaId = d.Id, UsuarioCadastroId = usuarioAutenticadoId, TipoRateio = x }).ToList();
        d.Logs.Add(new DespesaLog { DespesaId = d.Id, UsuarioCadastroId = usuarioAutenticadoId, Acao = AcaoLogs.Atualizacao, Descricao = "Despesa atualizada." });

        return Map(await repository.AtualizarAsync(d, cancellationToken));
    }

    public async Task<DespesaDto> EfetivarAsync(long id, EfetivarDespesaRequest req, CancellationToken cancellationToken = default)
    {
        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        var d = await repository.ObterPorIdAsync(id, cancellationToken) ?? throw new NotFoundException("despesa_nao_encontrada");
        if (d.Status != StatusDespesa.Pendente) throw new DomainException("status_invalido");
        if (string.IsNullOrWhiteSpace(req.TipoPagamento) || req.ValorTotal <= 0) throw new DomainException("dados_invalidos");
        if (req.DataEfetivacao < d.DataLancamento) throw new DomainException("periodo_invalido");
        if (req.ContaBancariaId.HasValue && req.CartaoId.HasValue) throw new DomainException("forma_pagamento_invalida");

        var liquido = Liquido(req.ValorTotal, req.Desconto, req.Acrescimo, req.Imposto, req.Juros);
        var valorAntesTransacao = d.ValorEfetivacao ?? 0m;
        d.DataEfetivacao = req.DataEfetivacao; d.TipoPagamento = req.TipoPagamento; d.ValorTotal = req.ValorTotal;
        d.Desconto = req.Desconto; d.Acrescimo = req.Acrescimo; d.Imposto = req.Imposto; d.Juros = req.Juros;
        d.ValorLiquido = liquido; d.ValorEfetivacao = liquido; d.Status = StatusDespesa.Efetivada; d.AnexoDocumento = req.AnexoDocumento;
        d.Logs.Add(new DespesaLog { DespesaId = d.Id, UsuarioCadastroId = usuarioAutenticadoId, Acao = AcaoLogs.Atualizacao, Descricao = "Despesa efetivada." });

        var despesaAtualizada = await repository.AtualizarAsync(d, cancellationToken);
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
            req.ContaBancariaId,
            req.CartaoId,
            cancellationToken);

        return Map(despesaAtualizada);
    }

    public async Task<DespesaDto> CancelarAsync(long id, CancellationToken cancellationToken = default)
    {
        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        var d = await repository.ObterPorIdAsync(id, cancellationToken) ?? throw new NotFoundException("despesa_nao_encontrada");
        if (d.Status != StatusDespesa.Pendente) throw new DomainException("status_invalido");
        d.Status = StatusDespesa.Cancelada;
        d.Logs.Add(new DespesaLog { DespesaId = d.Id, UsuarioCadastroId = usuarioAutenticadoId, Acao = AcaoLogs.Exclusao, Descricao = "Despesa cancelada." });
        return Map(await repository.AtualizarAsync(d, cancellationToken));
    }

    public async Task<DespesaDto> EstornarAsync(long id, CancellationToken cancellationToken = default)
    {
        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        var d = await repository.ObterPorIdAsync(id, cancellationToken) ?? throw new NotFoundException("despesa_nao_encontrada");
        if (d.Status != StatusDespesa.Efetivada) throw new DomainException("status_invalido");
        var valorAntesTransacao = d.ValorEfetivacao ?? d.ValorLiquido;
        d.Status = StatusDespesa.Pendente; d.DataEfetivacao = null; d.ValorEfetivacao = null;
        d.Logs.Add(new DespesaLog { DespesaId = d.Id, UsuarioCadastroId = usuarioAutenticadoId, Acao = AcaoLogs.Atualizacao, Descricao = "Despesa estornada." });
        var despesaAtualizada = await repository.AtualizarAsync(d, cancellationToken);
        await historicoTransacaoFinanceiraService.RegistrarEstornoAsync(
            TipoTransacaoFinanceira.Despesa,
            despesaAtualizada.Id,
            usuarioAutenticadoId,
            DateOnly.FromDateTime(DateTime.UtcNow),
            valorAntesTransacao,
            valorAntesTransacao,
            0m,
            "Estorno de despesa",
            d.TipoPagamento,
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
        if (recorrencia is not Recorrencia.Unica && (!quantidadeRecorrencia.HasValue || quantidadeRecorrencia <= 0))
            throw new DomainException("quantidade_recorrencia_invalida");
        if (!recorrenciaFixa && quantidadeRecorrencia.HasValue && quantidadeRecorrencia > 100)
            throw new DomainException("quantidade_recorrencia_invalida");
    }

    private static bool PagamentoCartao(string tipoPagamento) =>
        tipoPagamento is "cartaoCredito" or "cartaoDebito";

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

    private static decimal Liquido(decimal valorTotal, decimal desconto, decimal acrescimo, decimal imposto, decimal juros) => valorTotal - desconto + acrescimo + imposto + juros;
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

    private static IReadOnlyCollection<AmigoRateioRequest> NormalizarAmigos(
        IReadOnlyCollection<AmigoRateioRequest>? amigosObjetos,
        IReadOnlyCollection<string> amigosLegado,
        IReadOnlyDictionary<string, decimal>? rateioAmigosValoresLegado)
    {
        if (amigosObjetos is not null && amigosObjetos.Count > 0)
            return amigosObjetos.Where(x => !string.IsNullOrWhiteSpace(x.Nome)).ToArray();

        return amigosLegado
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => new AmigoRateioRequest(
                x,
                rateioAmigosValoresLegado is not null && rateioAmigosValoresLegado.TryGetValue(x, out var valor) ? valor : null))
            .ToArray();
    }

    private static DespesaDto Map(Despesa d) =>
        new(d.Id, d.Descricao, d.Observacao, d.DataLancamento, d.DataVencimento, d.DataEfetivacao, d.TipoDespesa, d.TipoPagamento, d.Recorrencia, d.QuantidadeRecorrencia, d.RecorrenciaFixa,
            d.ValorTotal, d.ValorLiquido, d.Desconto, d.Acrescimo, d.Imposto, d.Juros, d.ValorEfetivacao, d.Status.ToString().ToLowerInvariant(),
            d.AmigosRateio.Select(x => new AmigoRateioDto(x.AmigoNome, x.Valor)).ToArray(),
            d.AmigosRateio.Select(x => x.AmigoNome).ToArray(),
            d.AmigosRateio.Where(x => x.Valor.HasValue).ToDictionary(x => x.AmigoNome, x => x.Valor!.Value),
            d.AreasRateio.Select(x => new DespesaAreaRateioDto(
                x.AreaId,
                x.Area?.Nome ?? string.Empty,
                x.SubAreaId,
                x.SubArea?.Nome ?? string.Empty,
                x.Valor)).ToArray(),
            d.TiposRateio.Select(x => x.TipoRateio).ToArray(), d.AnexoDocumento,
            d.Logs.Select(x => new DespesaLogDto(x.Id, DateOnly.FromDateTime(x.DataHoraCadastro), x.Acao, x.Descricao)).ToArray());
}
