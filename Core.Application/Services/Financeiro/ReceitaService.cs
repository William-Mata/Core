using Core.Application.DTOs.Financeiro;
using Core.Application.Contracts.Financeiro;
using Core.Domain.Entities;
using Core.Domain.Entities.Financeiro;
using Core.Domain.Enums;
using Core.Domain.Exceptions;
using Core.Domain.Interfaces;
using Core.Domain.Interfaces.Financeiro;

namespace Core.Application.Services.Financeiro;

public sealed class ReceitaService(
    IReceitaRepository repository,
    IContaBancariaRepository contaRepository,
    IAreaRepository areaRepository,
    IUsuarioAutenticadoProvider usuarioAutenticadoProvider,
    HistoricoTransacaoFinanceiraService historicoTransacaoFinanceiraService,
    IRecorrenciaBackgroundPublisher recorrenciaBackgroundPublisher)
{
    private static readonly HashSet<string> TiposReceita = ["salario","freelance","reembolso","investimento","bonus","outros"];
    private static readonly HashSet<string> TiposRecebimento = ["pix","transferencia","contaCorrente","dinheiro","boleto"];

    public async Task<IReadOnlyCollection<ReceitaDto>> ListarAsync(CancellationToken cancellationToken = default) => (await repository.ListarAsync(cancellationToken)).Select(Map).ToArray();
    public async Task<ReceitaDto> ObterAsync(long id, CancellationToken cancellationToken = default) => Map(await repository.ObterPorIdAsync(id, cancellationToken) ?? throw new NotFoundException("receita_nao_encontrada"));

    public async Task<ReceitaDto> CriarAsync(CriarReceitaRequest req, CancellationToken cancellationToken = default)
    {
        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        await ValidarComumAsync(req.Descricao, req.DataLancamento, req.DataVencimento, req.TipoReceita, req.TipoRecebimento, req.Recorrencia, req.RecorrenciaFixa, req.QuantidadeRecorrencia, req.ValorTotal, req.ContaBancaria, cancellationToken);
        await ValidarAreasRateioAsync(req.AreasRateio, cancellationToken);
        var amigos = NormalizarAmigos(req.Amigos, req.AmigosRateio, req.RateioAmigosValores);
        ValidarRateioAmigos(amigos, req.ValorTotal);
        ValidarRateioAreas(req.AreasRateio, req.ValorTotal);
        var contaId = await ResolverContaIdAsync(req.ContaBancaria, cancellationToken);
        var liquido = Liquido(req.ValorTotal, req.Desconto, req.Acrescimo, req.Imposto, req.Juros);
        var r = new Receita
        {
            Descricao = req.Descricao.Trim(), Observacao = req.Observacao, DataLancamento = req.DataLancamento, DataVencimento = req.DataVencimento,
            TipoReceita = req.TipoReceita, TipoRecebimento = req.TipoRecebimento, Recorrencia = req.Recorrencia,
            RecorrenciaFixa = req.RecorrenciaFixa,
            QuantidadeRecorrencia = req.QuantidadeRecorrencia,
            ValorTotal = req.ValorTotal, ValorLiquido = liquido, Desconto = req.Desconto, Acrescimo = req.Acrescimo, Imposto = req.Imposto, Juros = req.Juros,
            UsuarioCadastroId = usuarioAutenticadoId,
            Status = StatusReceita.Pendente, ContaBancariaId = contaId, AnexoDocumento = req.AnexoDocumento,
            AmigosRateio = amigos.Select(x => new ReceitaAmigoRateio { UsuarioCadastroId = usuarioAutenticadoId, AmigoNome = x.Nome, Valor = x.Valor }).ToList(),
            AreasRateio = req.AreasRateio.Select(x => new ReceitaAreaRateio { UsuarioCadastroId = usuarioAutenticadoId, AreaId = x.AreaId, SubAreaId = x.SubAreaId, Valor = x.Valor }).ToList(),
            Logs = [new ReceitaLog { UsuarioCadastroId = usuarioAutenticadoId, Acao = AcaoLogs.Cadastro, Descricao = "Receita criada com status pendente." }]
        };
        var receitaCriada = await repository.CriarAsync(r, cancellationToken);

        var alvo = req.RecorrenciaFixa ? 100 : req.QuantidadeRecorrencia.GetValueOrDefault(1);
        if (alvo > 1)
        {
            var mensagem = new ReceitaRecorrenciaBackgroundMessage(
                usuarioAutenticadoId,
                req.Descricao.Trim(),
                req.Observacao,
                req.DataLancamento,
                req.DataVencimento,
                req.TipoReceita,
                req.TipoRecebimento,
                req.Recorrencia,
                req.RecorrenciaFixa,
                req.RecorrenciaFixa ? 100 : req.QuantidadeRecorrencia,
                req.ValorTotal,
                req.Desconto,
                req.Acrescimo,
                req.Imposto,
                req.Juros,
                req.ContaBancaria,
                req.AnexoDocumento,
                amigos.Select(x => new RateioAmigoBackgroundMessage(x.Nome, x.Valor)).ToArray(),
                req.AreasRateio.Select(x => new RateioAreaBackgroundMessage(x.AreaId, x.SubAreaId, x.Valor)).ToArray());

            await recorrenciaBackgroundPublisher.PublicarReceitaAsync(mensagem, cancellationToken);
        }

        return Map(receitaCriada);
    }

    public async Task<ReceitaDto> AtualizarAsync(long id, AtualizarReceitaRequest req, CancellationToken cancellationToken = default)
    {
        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        var r = await repository.ObterPorIdAsync(id, cancellationToken) ?? throw new NotFoundException("receita_nao_encontrada");
        if (r.Status != StatusReceita.Pendente) throw new DomainException("status_invalido");
        await ValidarComumAsync(req.Descricao, req.DataLancamento, req.DataVencimento, req.TipoReceita, req.TipoRecebimento, req.Recorrencia, req.RecorrenciaFixa, req.QuantidadeRecorrencia, req.ValorTotal, req.ContaBancaria, cancellationToken);
        await ValidarAreasRateioAsync(req.AreasRateio, cancellationToken);
        var amigos = NormalizarAmigos(req.Amigos, req.AmigosRateio, req.RateioAmigosValores);
        ValidarRateioAmigos(amigos, req.ValorTotal);
        ValidarRateioAreas(req.AreasRateio, req.ValorTotal);
        var contaId = await ResolverContaIdAsync(req.ContaBancaria, cancellationToken);
        var liquido = Liquido(req.ValorTotal, req.Desconto, req.Acrescimo, req.Imposto, req.Juros);

        r.Descricao = req.Descricao.Trim(); r.Observacao = req.Observacao; r.DataLancamento = req.DataLancamento; r.DataVencimento = req.DataVencimento;
        r.TipoReceita = req.TipoReceita; r.TipoRecebimento = req.TipoRecebimento; r.Recorrencia = req.Recorrencia; r.RecorrenciaFixa = req.RecorrenciaFixa; r.QuantidadeRecorrencia = req.QuantidadeRecorrencia;
        r.ValorTotal = req.ValorTotal; r.ValorLiquido = liquido; r.Desconto = req.Desconto; r.Acrescimo = req.Acrescimo; r.Imposto = req.Imposto; r.Juros = req.Juros;
        r.ContaBancariaId = contaId; r.AnexoDocumento = req.AnexoDocumento;
        r.AmigosRateio = amigos.Select(x => new ReceitaAmigoRateio { ReceitaId = r.Id, UsuarioCadastroId = usuarioAutenticadoId, AmigoNome = x.Nome, Valor = x.Valor }).ToList();
        r.AreasRateio = req.AreasRateio.Select(x => new ReceitaAreaRateio { ReceitaId = r.Id, UsuarioCadastroId = usuarioAutenticadoId, AreaId = x.AreaId, SubAreaId = x.SubAreaId, Valor = x.Valor }).ToList();
        r.Logs.Add(new ReceitaLog { ReceitaId = r.Id, UsuarioCadastroId = usuarioAutenticadoId, Acao = AcaoLogs.Atualizacao, Descricao = "Receita atualizada." });
        return Map(await repository.AtualizarAsync(r, cancellationToken));
    }

    public async Task<ReceitaDto> EfetivarAsync(long id, EfetivarReceitaRequest req, CancellationToken cancellationToken = default)
    {
        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        var r = await repository.ObterPorIdAsync(id, cancellationToken) ?? throw new NotFoundException("receita_nao_encontrada");
        if (r.Status != StatusReceita.Pendente) throw new DomainException("status_invalido");
        if (string.IsNullOrWhiteSpace(req.TipoRecebimento) || req.ValorTotal <= 0) throw new DomainException("dados_invalidos");
        if (req.DataEfetivacao < r.DataLancamento) throw new DomainException("periodo_invalido");
        if (ContaObrigatoria(req.TipoRecebimento) && string.IsNullOrWhiteSpace(req.ContaBancaria)) throw new DomainException("conta_bancaria_obrigatoria");

        var contaId = await ResolverContaIdAsync(req.ContaBancaria, cancellationToken);
        var liquido = Liquido(req.ValorTotal, req.Desconto, req.Acrescimo, req.Imposto, req.Juros);
        var valorAntesTransacao = r.ValorEfetivacao ?? 0m;
        r.DataEfetivacao = req.DataEfetivacao; r.TipoRecebimento = req.TipoRecebimento; r.ContaBancariaId = contaId;
        r.ValorTotal = req.ValorTotal; r.Desconto = req.Desconto; r.Acrescimo = req.Acrescimo; r.Imposto = req.Imposto; r.Juros = req.Juros;
        r.ValorLiquido = liquido; r.ValorEfetivacao = liquido; r.Status = StatusReceita.Efetivada; r.AnexoDocumento = req.AnexoDocumento;
        r.Logs.Add(new ReceitaLog { ReceitaId = r.Id, UsuarioCadastroId = usuarioAutenticadoId, Acao = AcaoLogs.Atualizacao, Descricao = "Receita efetivada." });
        var receitaAtualizada = await repository.AtualizarAsync(r, cancellationToken);
        await historicoTransacaoFinanceiraService.RegistrarEfetivacaoAsync(
            TipoTransacaoFinanceira.Receita,
            receitaAtualizada.Id,
            usuarioAutenticadoId,
            req.DataEfetivacao,
            valorAntesTransacao,
            receitaAtualizada.ValorEfetivacao ?? receitaAtualizada.ValorLiquido,
            receitaAtualizada.ValorEfetivacao ?? receitaAtualizada.ValorLiquido,
            "Efetivacao de receita",
            receitaAtualizada.TipoRecebimento,
            receitaAtualizada.ContaBancariaId,
            cancellationToken: cancellationToken);

        return Map(receitaAtualizada);
    }

    public async Task<ReceitaDto> CancelarAsync(long id, CancellationToken cancellationToken = default)
    {
        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        var r = await repository.ObterPorIdAsync(id, cancellationToken) ?? throw new NotFoundException("receita_nao_encontrada");
        if (r.Status != StatusReceita.Pendente) throw new DomainException("status_invalido");
        r.Status = StatusReceita.Cancelada;
        r.Logs.Add(new ReceitaLog { ReceitaId = r.Id, UsuarioCadastroId = usuarioAutenticadoId, Acao = AcaoLogs.Exclusao, Descricao = "Receita cancelada." });
        return Map(await repository.AtualizarAsync(r, cancellationToken));
    }

    public async Task<ReceitaDto> EstornarAsync(long id, CancellationToken cancellationToken = default)
    {
        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        var r = await repository.ObterPorIdAsync(id, cancellationToken) ?? throw new NotFoundException("receita_nao_encontrada");
        if (r.Status != StatusReceita.Efetivada) throw new DomainException("status_invalido");
        var valorAntesTransacao = r.ValorEfetivacao ?? r.ValorLiquido;
        r.Status = StatusReceita.Pendente; r.DataEfetivacao = null; r.ValorEfetivacao = null;
        r.Logs.Add(new ReceitaLog { ReceitaId = r.Id, UsuarioCadastroId = usuarioAutenticadoId, Acao = AcaoLogs.Atualizacao, Descricao = "Receita estornada." });
        var receitaAtualizada = await repository.AtualizarAsync(r, cancellationToken);
        await historicoTransacaoFinanceiraService.RegistrarEstornoAsync(
            TipoTransacaoFinanceira.Receita,
            receitaAtualizada.Id,
            usuarioAutenticadoId,
            DateOnly.FromDateTime(DateTime.UtcNow),
            valorAntesTransacao,
            valorAntesTransacao,
            0m,
            "Estorno de receita",
            r.TipoRecebimento,
            r.ContaBancariaId,
            cancellationToken: cancellationToken);

        return Map(receitaAtualizada);
    }

    private int ObterUsuarioAutenticadoId() =>
        usuarioAutenticadoProvider.ObterUsuarioId() ?? throw new DomainException("usuario_nao_autenticado");

    private async Task ValidarComumAsync(string descricao, DateOnly dataLanc, DateOnly dataVenc, string tipoReceita, string tipoRecebimento, Recorrencia recorrencia, bool recorrenciaFixa, int? quantidadeRecorrencia, decimal valorTotal, string? contaBancaria, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(descricao)) throw new DomainException("descricao_obrigatoria");
        if (valorTotal <= 0) throw new DomainException("valor_total_invalido");
        if (dataVenc < dataLanc) throw new DomainException("periodo_invalido");
        if (!TiposReceita.Contains(tipoReceita) || !TiposRecebimento.Contains(tipoRecebimento) || !Enum.IsDefined(recorrencia)) throw new DomainException("enum_invalida");
        if (recorrenciaFixa && recorrencia == Recorrencia.Unica) throw new DomainException("recorrencia_fixa_invalida");
        if (recorrencia is not Recorrencia.Unica && (!quantidadeRecorrencia.HasValue || quantidadeRecorrencia <= 0))
            throw new DomainException("quantidade_recorrencia_invalida");
        if (!recorrenciaFixa && quantidadeRecorrencia.HasValue && quantidadeRecorrencia > 100)
            throw new DomainException("quantidade_recorrencia_invalida");
        if (ContaObrigatoria(tipoRecebimento) && string.IsNullOrWhiteSpace(contaBancaria)) throw new DomainException("conta_bancaria_obrigatoria");
        if (!string.IsNullOrWhiteSpace(contaBancaria) && await ResolverContaIdAsync(contaBancaria, cancellationToken) is null) throw new DomainException("conta_bancaria_invalida");
    }

    private async Task ValidarAreasRateioAsync(IReadOnlyCollection<ReceitaAreaRateioRequest> areasRateio, CancellationToken cancellationToken)
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

            if (subArea.Area.Tipo != TipoAreaFinanceira.Receita)
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

    private static void ValidarRateioAreas(IReadOnlyCollection<ReceitaAreaRateioRequest> areasRateio, decimal valorTotal)
    {
        if (areasRateio.Count == 0) return;

        if (areasRateio.Any(x => !x.Valor.HasValue || x.Valor <= 0))
            throw new DomainException("rateio_area_invalido");

        if (areasRateio.Sum(x => x.Valor!.Value) != valorTotal)
            throw new DomainException("rateio_area_invalido");
    }

    private static bool ContaObrigatoria(string tipoRecebimento) => tipoRecebimento is "pix" or "transferencia";
    private static decimal Liquido(decimal valorTotal, decimal desconto, decimal acrescimo, decimal imposto, decimal juros) => valorTotal - desconto + acrescimo + imposto + juros;
    private static IReadOnlyCollection<AmigoRateioRequest> NormalizarAmigos(
        IReadOnlyCollection<AmigoRateioRequest>? amigosObjetos,
        IReadOnlyCollection<string> amigosLegado,
        IReadOnlyDictionary<string, decimal> rateioAmigosValoresLegado)
    {
        if (amigosObjetos is not null && amigosObjetos.Count > 0)
            return amigosObjetos.Where(x => !string.IsNullOrWhiteSpace(x.Nome)).ToArray();

        return amigosLegado
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => new AmigoRateioRequest(x, rateioAmigosValoresLegado.TryGetValue(x, out var valor) ? valor : null))
            .ToArray();
    }

    private async Task<long?> ResolverContaIdAsync(string? contaBancaria, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(contaBancaria)) return null;
        if (long.TryParse(contaBancaria, out var idNumerico)) return idNumerico;
        var contas = await contaRepository.ListarAsync(cancellationToken);
        return contas.FirstOrDefault(x => string.Equals(x.Descricao, contaBancaria, StringComparison.OrdinalIgnoreCase))?.Id;
    }

    private static ReceitaDto Map(Receita r) =>
        new(r.Id, r.Descricao, r.Observacao, r.DataLancamento, r.DataVencimento, r.DataEfetivacao, r.TipoReceita, r.TipoRecebimento, r.Recorrencia, r.QuantidadeRecorrencia, r.RecorrenciaFixa,
            r.ValorTotal, r.ValorLiquido, r.Desconto, r.Acrescimo, r.Imposto, r.Juros, r.ValorEfetivacao, r.Status.ToString().ToLowerInvariant(),
            r.AmigosRateio.Select(x => new AmigoRateioDto(x.AmigoNome, x.Valor)).ToArray(),
            r.AmigosRateio.Select(x => x.AmigoNome).ToArray(),
            r.AmigosRateio.Where(x => x.Valor.HasValue).ToDictionary(x => x.AmigoNome, x => x.Valor!.Value),
            r.AreasRateio.Select(x => new ReceitaAreaRateioDto(
                x.AreaId,
                x.Area?.Nome ?? string.Empty,
                x.SubAreaId,
                x.SubArea?.Nome ?? string.Empty,
                x.Valor)).ToArray(),
            r.ContaBancariaId?.ToString(), r.AnexoDocumento,
            r.Logs.Select(x => new ReceitaLogDto(x.Id, DateOnly.FromDateTime(x.DataHoraCadastro), x.Acao, x.Descricao)).ToArray());
}
