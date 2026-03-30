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

public sealed partial class ReceitaService(
    IReceitaRepository repository,
    IContaBancariaRepository contaRepository,
    IAreaRepository areaRepository,
    IAmizadeRepository amizadeRepository,
    IUsuarioRepository usuarioRepository,
    IUsuarioAutenticadoProvider usuarioAutenticadoProvider,
    HistoricoTransacaoFinanceiraService historicoTransacaoFinanceiraService,
    IDocumentoStorageService documentoStorageService,
    IRecorrenciaBackgroundPublisher recorrenciaBackgroundPublisher)
{
    private static readonly HashSet<string> TiposReceita = ["salario", "freelance", "reembolso", "investimento", "bonus", "outros"];
    private static readonly HashSet<string> TiposRecebimento = ["pix", "transferencia", "contaCorrente", "dinheiro", "boleto"];

    private sealed record AmigoRateioValidado(int AmigoId, string Nome, decimal Valor);

    public async Task<IReadOnlyCollection<ReceitaDto>> ListarAsync(ListarReceitasRequest request, CancellationToken cancellationToken = default)
    {
        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        var periodo = CompetenciaPeriodoHelper.Resolver(request.Competencia, request.DataInicio, request.DataFim);
        return (await repository.ListarPorUsuarioAsync(usuarioAutenticadoId, request.Id, request.Descricao, periodo.DataInicio, periodo.DataFim, cancellationToken))
            .Where(x => !(x.ReceitaOrigemId.HasValue && (x.Status == StatusReceita.PendenteAprovacao || x.Status == StatusReceita.Rejeitado)))
            .Select(Map)
            .ToArray();
    }

    public async Task<IReadOnlyCollection<ReceitaDto>> ListarPendentesAprovacaoAsync(CancellationToken cancellationToken = default)
    {
        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        return (await repository.ListarPendentesAprovacaoPorUsuarioAsync(usuarioAutenticadoId, cancellationToken))
            .Select(Map)
            .ToArray();
    }

    public async Task<ReceitaDto> ObterAsync(long id, CancellationToken cancellationToken = default) =>
        Map(await repository.ObterPorIdAsync(id, ObterUsuarioAutenticadoId(), cancellationToken) ?? throw new NotFoundException("receita_nao_encontrada"));

    public async Task<ReceitaDto> CriarAsync(CriarReceitaRequest req, CancellationToken cancellationToken = default)
    {
        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        await ValidarComumAsync(req.Descricao, req.DataLancamento, req.DataVencimento, req.TipoReceita, req.TipoRecebimento, req.Recorrencia, req.RecorrenciaFixa, req.QuantidadeRecorrencia, req.ValorTotal, req.ContaBancaria, usuarioAutenticadoId, cancellationToken);
        await ValidarAreasRateioAsync(req.AreasSubAreasRateio, cancellationToken);
        var amigos = NormalizarAmigos(req.AmigosRateio);
        ValidarRateioAmigos(amigos, req.ValorTotal);
        ValidarRateioAreas(req.AreasSubAreasRateio, req.ValorTotal);
        var amigosValidados = await ValidarAmigosAceitosAsync(amigos, usuarioAutenticadoId, cancellationToken);
        var contaId = await ResolverContaIdAsync(req.ContaBancaria, usuarioAutenticadoId, cancellationToken);
        var liquido = Liquido(req.ValorTotal, req.Desconto, req.Acrescimo, req.Imposto, req.Juros);
        var documentos = await SalvarDocumentosAsync(req.Documentos ?? [], usuarioAutenticadoId, cancellationToken: cancellationToken);

        var receita = new Receita
        {
            Descricao = req.Descricao.Trim(),
            Observacao = req.Observacao,
            DataLancamento = req.DataLancamento,
            DataVencimento = req.DataVencimento,
            TipoReceita = req.TipoReceita,
            TipoRecebimento = req.TipoRecebimento,
            Recorrencia = req.Recorrencia,
            RecorrenciaFixa = req.RecorrenciaFixa,
            QuantidadeRecorrencia = req.QuantidadeRecorrencia,
            ValorTotal = req.ValorTotal,
            ValorLiquido = liquido,
            Desconto = req.Desconto,
            Acrescimo = req.Acrescimo,
            Imposto = req.Imposto,
            Juros = req.Juros,
            UsuarioCadastroId = usuarioAutenticadoId,
            Status = StatusReceita.Pendente,
            ContaBancariaId = contaId,
            Documentos = documentos,
            AmigosRateio = amigosValidados.Select(x => new ReceitaAmigoRateio
            {
                UsuarioCadastroId = usuarioAutenticadoId,
                AmigoId = x.AmigoId,
                AmigoNome = x.Nome,
                Valor = x.Valor
            }).ToList(),
            AreasRateio = req.AreasSubAreasRateio.Select(x => new ReceitaAreaRateio
            {
                UsuarioCadastroId = usuarioAutenticadoId,
                AreaId = x.AreaId,
                SubAreaId = x.SubAreaId,
                Valor = x.Valor
            }).ToList(),
            Logs = [new ReceitaLog { UsuarioCadastroId = usuarioAutenticadoId, Acao = AcaoLogs.Cadastro, Descricao = "Receita criada com status pendente." }]
        };

        var receitaCriada = await repository.CriarAsync(receita, cancellationToken);
        await CriarEspelhosRateioAsync(receitaCriada, amigosValidados, req.AreasSubAreasRateio, cancellationToken);

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
                [],
                amigosValidados.Select(x => new RateioAmigoBackgroundMessage(x.AmigoId, x.Nome, x.Valor)).ToArray(),
                req.AreasSubAreasRateio.Select(x => new RateioAreaBackgroundMessage(x.AreaId, x.SubAreaId, x.Valor)).ToArray());

            await recorrenciaBackgroundPublisher.PublicarReceitaAsync(mensagem, cancellationToken);
        }

        return Map(receitaCriada);
    }

    public async Task<ReceitaDto> AtualizarAsync(long id, AtualizarReceitaRequest req, CancellationToken cancellationToken = default)
    {
        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        var receita = await repository.ObterPorIdAsync(id, usuarioAutenticadoId, cancellationToken) ?? throw new NotFoundException("receita_nao_encontrada");
        if (receita.Status != StatusReceita.Pendente) throw new DomainException("status_invalido");
        await ValidarComumAsync(req.Descricao, req.DataLancamento, req.DataVencimento, req.TipoReceita, req.TipoRecebimento, req.Recorrencia, req.RecorrenciaFixa, req.QuantidadeRecorrencia, req.ValorTotal, req.ContaBancaria, usuarioAutenticadoId, cancellationToken);
        await ValidarAreasRateioAsync(req.AreasSubAreasRateio, cancellationToken);
        var amigos = NormalizarAmigos(req.AmigosRateio);
        ValidarRateioAmigos(amigos, req.ValorTotal);
        ValidarRateioAreas(req.AreasSubAreasRateio, req.ValorTotal);
        var amigosValidados = await ValidarAmigosAceitosAsync(amigos, usuarioAutenticadoId, cancellationToken);
        var contaId = await ResolverContaIdAsync(req.ContaBancaria, usuarioAutenticadoId, cancellationToken);
        var liquido = Liquido(req.ValorTotal, req.Desconto, req.Acrescimo, req.Imposto, req.Juros);

        receita.Descricao = req.Descricao.Trim();
        receita.Observacao = req.Observacao;
        receita.DataLancamento = req.DataLancamento;
        receita.DataVencimento = req.DataVencimento;
        receita.TipoReceita = req.TipoReceita;
        receita.TipoRecebimento = req.TipoRecebimento;
        receita.Recorrencia = req.Recorrencia;
        receita.RecorrenciaFixa = req.RecorrenciaFixa;
        receita.QuantidadeRecorrencia = req.QuantidadeRecorrencia;
        receita.ValorTotal = req.ValorTotal;
        receita.ValorLiquido = liquido;
        receita.Desconto = req.Desconto;
        receita.Acrescimo = req.Acrescimo;
        receita.Imposto = req.Imposto;
        receita.Juros = req.Juros;
        receita.ContaBancariaId = contaId;
        if (req.Documentos is not null)
            receita.Documentos = await SalvarDocumentosAsync(req.Documentos, usuarioAutenticadoId, receitaId: receita.Id, cancellationToken: cancellationToken);
        receita.AmigosRateio = amigosValidados.Select(x => new ReceitaAmigoRateio
        {
            ReceitaId = receita.Id,
            UsuarioCadastroId = usuarioAutenticadoId,
            AmigoId = x.AmigoId,
            AmigoNome = x.Nome,
            Valor = x.Valor
        }).ToList();
        receita.AreasRateio = req.AreasSubAreasRateio.Select(x => new ReceitaAreaRateio
        {
            ReceitaId = receita.Id,
            UsuarioCadastroId = usuarioAutenticadoId,
            AreaId = x.AreaId,
            SubAreaId = x.SubAreaId,
            Valor = x.Valor
        }).ToList();
        receita.Logs.Add(new ReceitaLog { ReceitaId = receita.Id, UsuarioCadastroId = usuarioAutenticadoId, Acao = AcaoLogs.Atualizacao, Descricao = "Receita atualizada." });

        var receitaAtualizada = await repository.AtualizarAsync(receita, cancellationToken);
        await SincronizarEspelhosRateioAsync(receitaAtualizada, amigosValidados, req.AreasSubAreasRateio, cancellationToken);
        return Map(receitaAtualizada);
    }

    public async Task<ReceitaDto> AprovarRateioAsync(long id, CancellationToken cancellationToken = default)
    {
        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        var receita = await repository.ObterPorIdAsync(id, usuarioAutenticadoId, cancellationToken) ?? throw new NotFoundException("receita_nao_encontrada");
        if (!receita.ReceitaOrigemId.HasValue) throw new DomainException("aprovacao_invalida");
        if (receita.Status != StatusReceita.PendenteAprovacao) throw new DomainException("status_invalido");

        receita.Status = StatusReceita.Pendente;
        receita.Logs.Add(new ReceitaLog
        {
            ReceitaId = receita.Id,
            UsuarioCadastroId = usuarioAutenticadoId,
            Acao = AcaoLogs.Atualizacao,
            Descricao = "Rateio aprovado pelo amigo."
        });

        return Map(await repository.AtualizarAsync(receita, cancellationToken));
    }

    public async Task<ReceitaDto> RejeitarRateioAsync(long id, CancellationToken cancellationToken = default)
    {
        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        var receita = await repository.ObterPorIdAsync(id, usuarioAutenticadoId, cancellationToken) ?? throw new NotFoundException("receita_nao_encontrada");
        if (!receita.ReceitaOrigemId.HasValue) throw new DomainException("aprovacao_invalida");
        if (receita.Status != StatusReceita.PendenteAprovacao) throw new DomainException("status_invalido");

        receita.Status = StatusReceita.Rejeitado;
        receita.Logs.Add(new ReceitaLog
        {
            ReceitaId = receita.Id,
            UsuarioCadastroId = usuarioAutenticadoId,
            Acao = AcaoLogs.Atualizacao,
            Descricao = "Rateio rejeitado pelo amigo."
        });

        return Map(await repository.AtualizarAsync(receita, cancellationToken));
    }

    public async Task<ReceitaDto> EfetivarAsync(long id, EfetivarReceitaRequest req, CancellationToken cancellationToken = default)
    {
        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        var receita = await repository.ObterPorIdAsync(id, usuarioAutenticadoId, cancellationToken) ?? throw new NotFoundException("receita_nao_encontrada");
        if (receita.Status != StatusReceita.Pendente) throw new DomainException("status_invalido");
        if (string.IsNullOrWhiteSpace(req.TipoRecebimento) || req.ValorTotal <= 0) throw new DomainException("dados_invalidos");
        if (req.DataEfetivacao < receita.DataLancamento) throw new DomainException("periodo_invalido");
        if (ContaObrigatoria(req.TipoRecebimento) && string.IsNullOrWhiteSpace(req.ContaBancaria)) throw new DomainException("conta_bancaria_obrigatoria");

        var contaId = await ResolverContaIdAsync(req.ContaBancaria, usuarioAutenticadoId, cancellationToken);
        var liquido = Liquido(req.ValorTotal, req.Desconto, req.Acrescimo, req.Imposto, req.Juros);
        var valorAntesTransacao = receita.ValorEfetivacao ?? 0m;
        receita.DataEfetivacao = req.DataEfetivacao;
        receita.TipoRecebimento = req.TipoRecebimento;
        receita.ContaBancariaId = contaId;
        receita.ValorTotal = req.ValorTotal;
        receita.Desconto = req.Desconto;
        receita.Acrescimo = req.Acrescimo;
        receita.Imposto = req.Imposto;
        receita.Juros = req.Juros;
        receita.ValorLiquido = liquido;
        receita.ValorEfetivacao = liquido;
        receita.Status = StatusReceita.Efetivada;
        if (req.Documentos is not null)
            receita.Documentos = await SalvarDocumentosAsync(req.Documentos, usuarioAutenticadoId, receitaId: receita.Id, cancellationToken: cancellationToken);
        receita.Logs.Add(new ReceitaLog { ReceitaId = receita.Id, UsuarioCadastroId = usuarioAutenticadoId, Acao = AcaoLogs.Atualizacao, Descricao = "Receita efetivada." });
        var receitaAtualizada = await repository.AtualizarAsync(receita, cancellationToken);
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
        var receita = await repository.ObterPorIdAsync(id, usuarioAutenticadoId, cancellationToken) ?? throw new NotFoundException("receita_nao_encontrada");
        if (receita.Status != StatusReceita.Pendente) throw new DomainException("status_invalido");
        receita.Status = StatusReceita.Cancelada;
        receita.Logs.Add(new ReceitaLog { ReceitaId = receita.Id, UsuarioCadastroId = usuarioAutenticadoId, Acao = AcaoLogs.Exclusao, Descricao = "Receita cancelada." });
        return Map(await repository.AtualizarAsync(receita, cancellationToken));
    }

    public async Task<ReceitaDto> EstornarAsync(long id, CancellationToken cancellationToken = default)
    {
        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        var receita = await repository.ObterPorIdAsync(id, usuarioAutenticadoId, cancellationToken) ?? throw new NotFoundException("receita_nao_encontrada");
        if (receita.Status != StatusReceita.Efetivada) throw new DomainException("status_invalido");
        var valorAntesTransacao = receita.ValorEfetivacao ?? receita.ValorLiquido;
        receita.Status = StatusReceita.Pendente;
        receita.DataEfetivacao = null;
        receita.ValorEfetivacao = null;
        receita.Logs.Add(new ReceitaLog { ReceitaId = receita.Id, UsuarioCadastroId = usuarioAutenticadoId, Acao = AcaoLogs.Atualizacao, Descricao = "Receita estornada." });
        var receitaAtualizada = await repository.AtualizarAsync(receita, cancellationToken);
        await historicoTransacaoFinanceiraService.RegistrarEstornoAsync(
            TipoTransacaoFinanceira.Receita,
            receitaAtualizada.Id,
            usuarioAutenticadoId,
            DateOnly.FromDateTime(DateTime.UtcNow),
            valorAntesTransacao,
            valorAntesTransacao,
            0m,
            "Estorno de receita",
            receita.TipoRecebimento,
            receita.ContaBancariaId,
            cancellationToken: cancellationToken);

        return Map(receitaAtualizada);
    }

    private int ObterUsuarioAutenticadoId() =>
        usuarioAutenticadoProvider.ObterUsuarioId() ?? throw new DomainException("usuario_nao_autenticado");

    private async Task ValidarComumAsync(string descricao, DateOnly dataLanc, DateOnly dataVenc, string tipoReceita, string tipoRecebimento, Recorrencia recorrencia, bool recorrenciaFixa, int? quantidadeRecorrencia, decimal valorTotal, string? contaBancaria, int usuarioAutenticadoId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(descricao)) throw new DomainException("descricao_obrigatoria");
        if (valorTotal <= 0) throw new DomainException("valor_total_invalido");
        if (dataVenc < dataLanc) throw new DomainException("periodo_invalido");
        if (!TiposReceita.Contains(tipoReceita) || !TiposRecebimento.Contains(tipoRecebimento) || !Enum.IsDefined(recorrencia)) throw new DomainException("enum_invalida");
        if (recorrenciaFixa && recorrencia == Recorrencia.Unica) throw new DomainException("recorrencia_fixa_invalida");
        if (!recorrenciaFixa && recorrencia is not Recorrencia.Unica && (!quantidadeRecorrencia.HasValue || quantidadeRecorrencia <= 0))
            throw new DomainException("quantidade_recorrencia_invalida");
        if (recorrenciaFixa && quantidadeRecorrencia.HasValue && quantidadeRecorrencia <= 0)
            throw new DomainException("quantidade_recorrencia_invalida");
        if (!recorrenciaFixa && quantidadeRecorrencia.HasValue && quantidadeRecorrencia > 100)
            throw new DomainException("quantidade_recorrencia_invalida");
        if (ContaObrigatoria(tipoRecebimento) && string.IsNullOrWhiteSpace(contaBancaria)) throw new DomainException("conta_bancaria_obrigatoria");
        if (!string.IsNullOrWhiteSpace(contaBancaria) && await ResolverContaIdAsync(contaBancaria, usuarioAutenticadoId, cancellationToken) is null) throw new DomainException("conta_bancaria_invalida");
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
        Receita origem,
        IReadOnlyCollection<AmigoRateioValidado> amigos,
        IReadOnlyCollection<ReceitaAreaRateioRequest> areasRateioOrigem,
        CancellationToken cancellationToken)
    {
        if (amigos.Count == 0)
            return;

        foreach (var amigo in amigos)
        {
            var espelho = CriarEspelhoReceita(origem, amigo, areasRateioOrigem);
            await repository.CriarAsync(espelho, cancellationToken);
        }
    }

    private async Task SincronizarEspelhosRateioAsync(
        Receita origem,
        IReadOnlyCollection<AmigoRateioValidado> amigos,
        IReadOnlyCollection<ReceitaAreaRateioRequest> areasRateioOrigem,
        CancellationToken cancellationToken)
    {
        var espelhos = await repository.ListarEspelhosPorOrigemAsync(origem.Id, cancellationToken);
        if (espelhos.Count == 0 && amigos.Count == 0)
            return;

        var amigosIds = amigos.Select(x => x.AmigoId).ToHashSet();

        foreach (var espelho in espelhos.Where(x => !amigosIds.Contains(x.UsuarioCadastroId) && x.Status != StatusReceita.Cancelada))
        {
            espelho.Status = StatusReceita.Cancelada;
            espelho.DataEfetivacao = null;
            espelho.ValorEfetivacao = null;
            espelho.Logs.Add(new ReceitaLog
            {
                ReceitaId = espelho.Id,
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

            if (espelho is null || espelho.Status == StatusReceita.Cancelada)
            {
                await repository.CriarAsync(CriarEspelhoReceita(origem, amigo, areasRateioOrigem), cancellationToken);
                continue;
            }

            if (espelho.Status == StatusReceita.PendenteAprovacao || espelho.Status == StatusReceita.Rejeitado)
            {
                AplicarSnapshotNoEspelho(espelho, origem, amigo, areasRateioOrigem);
                espelho.Status = StatusReceita.PendenteAprovacao;
                espelho.Logs.Add(new ReceitaLog
                {
                    ReceitaId = espelho.Id,
                    UsuarioCadastroId = origem.UsuarioCadastroId,
                    Acao = AcaoLogs.Atualizacao,
                    Descricao = "Rateio reenviado para aprovacao."
                });
                await repository.AtualizarAsync(espelho, cancellationToken);
            }
        }
    }

    private static Receita CriarEspelhoReceita(
        Receita origem,
        AmigoRateioValidado amigo,
        IReadOnlyCollection<ReceitaAreaRateioRequest> areasRateioOrigem)
    {
        return new Receita
        {
            ReceitaOrigemId = origem.Id,
            UsuarioCadastroId = amigo.AmigoId,
            Descricao = origem.Descricao,
            Observacao = origem.Observacao,
            DataLancamento = origem.DataLancamento,
            DataVencimento = origem.DataVencimento,
            TipoReceita = origem.TipoReceita,
            TipoRecebimento = origem.TipoRecebimento,
            Recorrencia = origem.Recorrencia,
            RecorrenciaFixa = origem.RecorrenciaFixa,
            QuantidadeRecorrencia = origem.QuantidadeRecorrencia,
            ValorTotal = amigo.Valor,
            ValorLiquido = amigo.Valor,
            Desconto = 0m,
            Acrescimo = 0m,
            Imposto = 0m,
            Juros = 0m,
            Status = StatusReceita.PendenteAprovacao,
            ContaBancariaId = origem.ContaBancariaId,
            Documentos = [],
            AmigosRateio = [],
            AreasRateio = DistribuirAreasProporcionalmente(areasRateioOrigem, origem.ValorTotal, amigo.Valor, amigo.AmigoId),
            Logs =
            [
                new ReceitaLog
                {
                    UsuarioCadastroId = origem.UsuarioCadastroId,
                    Acao = AcaoLogs.Cadastro,
                    Descricao = "Receita compartilhada aguardando aprovacao."
                }
            ]
        };
    }

    private static void AplicarSnapshotNoEspelho(
        Receita espelho,
        Receita origem,
        AmigoRateioValidado amigo,
        IReadOnlyCollection<ReceitaAreaRateioRequest> areasRateioOrigem)
    {
        espelho.Descricao = origem.Descricao;
        espelho.Observacao = origem.Observacao;
        espelho.DataLancamento = origem.DataLancamento;
        espelho.DataVencimento = origem.DataVencimento;
        espelho.TipoReceita = origem.TipoReceita;
        espelho.TipoRecebimento = origem.TipoRecebimento;
        espelho.Recorrencia = origem.Recorrencia;
        espelho.RecorrenciaFixa = origem.RecorrenciaFixa;
        espelho.QuantidadeRecorrencia = origem.QuantidadeRecorrencia;
        espelho.ContaBancariaId = origem.ContaBancariaId;
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

    private static List<ReceitaAreaRateio> DistribuirAreasProporcionalmente(
        IReadOnlyCollection<ReceitaAreaRateioRequest> areasRateioOrigem,
        decimal valorTotalOrigem,
        decimal valorEspelho,
        int usuarioCadastroId,
        long? receitaId = null)
    {
        if (areasRateioOrigem.Count == 0 || valorTotalOrigem <= 0 || valorEspelho <= 0)
            return [];

        var areas = areasRateioOrigem.Where(x => x.Valor.HasValue && x.Valor > 0).ToArray();
        if (areas.Length == 0)
            return [];

        var resultado = new List<ReceitaAreaRateio>(areas.Length);
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
            resultado.Add(new ReceitaAreaRateio
            {
                ReceitaId = receitaId ?? 0,
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

    private async Task<long?> ResolverContaIdAsync(string? contaBancaria, int usuarioAutenticadoId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(contaBancaria)) return null;
        if (long.TryParse(contaBancaria, out var idNumerico)) return idNumerico;
        var contas = await contaRepository.ListarAsync(usuarioAutenticadoId, cancellationToken);
        return contas.FirstOrDefault(x => string.Equals(x.Descricao, contaBancaria, StringComparison.OrdinalIgnoreCase))?.Id;
    }

    private static ReceitaDto Map(Receita receita) =>
        new(
            receita.Id,
            receita.Descricao,
            receita.Observacao,
            receita.DataLancamento,
            receita.DataVencimento,
            receita.DataEfetivacao,
            receita.TipoReceita,
            receita.TipoRecebimento,
            receita.Recorrencia,
            receita.QuantidadeRecorrencia,
            receita.RecorrenciaFixa,
            receita.ValorTotal,
            receita.ValorLiquido,
            receita.Desconto,
            receita.Acrescimo,
            receita.Imposto,
            receita.Juros,
            receita.ValorEfetivacao,
            receita.Status.ToString().ToLowerInvariant(),
            receita.AmigosRateio.Select(x => new AmigoRateioDto(x.AmigoId, x.AmigoNome, x.Valor)).ToArray(),
            receita.AreasRateio.Select(x => new ReceitaAreaRateioDto(
                x.AreaId,
                x.Area?.Nome ?? string.Empty,
                x.SubAreaId,
                x.SubArea?.Nome ?? string.Empty,
                x.Valor)).ToArray(),
            receita.ContaBancariaId?.ToString(),
            receita.Documentos.Select(x => new DocumentoDto(x.NomeArquivo, x.CaminhoArquivo, x.ContentType, x.TamanhoBytes)).ToArray(),
            receita.Logs.Select(x => new ReceitaLogDto(x.Id, DateOnly.FromDateTime(x.DataHoraCadastro), x.Acao, x.Descricao)).ToArray());
}
