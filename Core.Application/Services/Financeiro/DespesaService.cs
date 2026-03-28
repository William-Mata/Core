using Core.Application.DTOs;
using Core.Domain.Entities;
using Core.Domain.Entities.Financeiro;
using Core.Domain.Enums;
using Core.Domain.Exceptions;
using Core.Domain.Interfaces;
using Core.Domain.Interfaces.Financeiro;

namespace Core.Application.Services.Financeiro;

public sealed class DespesaService(IDespesaRepository repository, IUsuarioAutenticadoProvider usuarioAutenticadoProvider)
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
        ValidarComum(req.Descricao, req.DataLancamento, req.DataVencimento, req.TipoDespesa, req.TipoPagamento, req.Recorrencia, req.ValorTotal);
        var liquido = Liquido(req.ValorTotal, req.Desconto, req.Acrescimo, req.Imposto, req.Juros);

        var d = new Despesa
        {
            Descricao = req.Descricao.Trim(), Observacao = req.Observacao, DataLancamento = req.DataLancamento, DataVencimento = req.DataVencimento,
            TipoDespesa = req.TipoDespesa, TipoPagamento = req.TipoPagamento, Recorrencia = req.Recorrencia,
            ValorTotal = req.ValorTotal, ValorLiquido = liquido, Desconto = req.Desconto, Acrescimo = req.Acrescimo, Imposto = req.Imposto, Juros = req.Juros,
            UsuarioCadastroId = usuarioAutenticadoId,
            Status = StatusDespesa.Pendente, AnexoDocumento = req.AnexoDocumento,
            AmigosRateio = req.AmigosRateio.Select(x => new DespesaAmigoRateio { UsuarioCadastroId = usuarioAutenticadoId, AmigoNome = x }).ToList(),
            TiposRateio = req.TiposRateio.Select(x => new DespesaTipoRateio { UsuarioCadastroId = usuarioAutenticadoId, TipoRateio = x }).ToList(),
            Logs = [new DespesaLog { UsuarioCadastroId = usuarioAutenticadoId, Acao = AcaoLogs.Cadastro, Descricao = "Despesa criada com status pendente." }]
        };

        return Map(await repository.CriarAsync(d, cancellationToken));
    }

    public async Task<DespesaDto> AtualizarAsync(long id, AtualizarDespesaRequest req, CancellationToken cancellationToken = default)
    {
        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        var d = await repository.ObterPorIdAsync(id, cancellationToken) ?? throw new NotFoundException("despesa_nao_encontrada");
        if (d.Status != StatusDespesa.Pendente) throw new DomainException("status_invalido");

        ValidarComum(req.Descricao, req.DataLancamento, req.DataVencimento, req.TipoDespesa, req.TipoPagamento, req.Recorrencia, req.ValorTotal);
        var liquido = Liquido(req.ValorTotal, req.Desconto, req.Acrescimo, req.Imposto, req.Juros);

        d.Descricao = req.Descricao.Trim(); d.Observacao = req.Observacao; d.DataLancamento = req.DataLancamento; d.DataVencimento = req.DataVencimento;
        d.TipoDespesa = req.TipoDespesa; d.TipoPagamento = req.TipoPagamento; d.Recorrencia = req.Recorrencia;
        d.ValorTotal = req.ValorTotal; d.ValorLiquido = liquido; d.Desconto = req.Desconto; d.Acrescimo = req.Acrescimo; d.Imposto = req.Imposto; d.Juros = req.Juros;
        d.AnexoDocumento = req.AnexoDocumento;
        d.AmigosRateio = req.AmigosRateio.Select(x => new DespesaAmigoRateio { DespesaId = d.Id, UsuarioCadastroId = usuarioAutenticadoId, AmigoNome = x }).ToList();
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

        var liquido = Liquido(req.ValorTotal, req.Desconto, req.Acrescimo, req.Imposto, req.Juros);
        d.DataEfetivacao = req.DataEfetivacao; d.TipoPagamento = req.TipoPagamento; d.ValorTotal = req.ValorTotal;
        d.Desconto = req.Desconto; d.Acrescimo = req.Acrescimo; d.Imposto = req.Imposto; d.Juros = req.Juros;
        d.ValorLiquido = liquido; d.ValorEfetivacao = liquido; d.Status = StatusDespesa.Efetivada; d.AnexoDocumento = req.AnexoDocumento;
        d.Logs.Add(new DespesaLog { DespesaId = d.Id, UsuarioCadastroId = usuarioAutenticadoId, Acao = AcaoLogs.Atualizacao, Descricao = "Despesa efetivada." });

        return Map(await repository.AtualizarAsync(d, cancellationToken));
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
        d.Status = StatusDespesa.Pendente; d.DataEfetivacao = null; d.ValorEfetivacao = null;
        d.Logs.Add(new DespesaLog { DespesaId = d.Id, UsuarioCadastroId = usuarioAutenticadoId, Acao = AcaoLogs.Atualizacao, Descricao = "Despesa estornada." });
        return Map(await repository.AtualizarAsync(d, cancellationToken));
    }

    private int ObterUsuarioAutenticadoId() =>
        usuarioAutenticadoProvider.ObterUsuarioId() ?? throw new DomainException("usuario_nao_autenticado");

    private static void ValidarComum(string descricao, DateOnly dataLanc, DateOnly dataVenc, string tipoDespesa, string tipoPagamento, Recorrencia recorrencia, decimal valorTotal)
    {
        if (string.IsNullOrWhiteSpace(descricao)) throw new DomainException("descricao_obrigatoria");
        if (valorTotal <= 0) throw new DomainException("valor_total_invalido");
        if (dataVenc < dataLanc) throw new DomainException("periodo_invalido");
        if (!TiposDespesa.Contains(tipoDespesa) || !TiposPagamento.Contains(tipoPagamento) || !Enum.IsDefined(recorrencia)) throw new DomainException("enum_invalida");
    }

    private static decimal Liquido(decimal valorTotal, decimal desconto, decimal acrescimo, decimal imposto, decimal juros) => valorTotal - desconto + acrescimo + imposto + juros;

    private static DespesaDto Map(Despesa d) =>
        new(d.Id, d.Descricao, d.Observacao, d.DataLancamento, d.DataVencimento, d.DataEfetivacao, d.TipoDespesa, d.TipoPagamento, d.Recorrencia,
            d.ValorTotal, d.ValorLiquido, d.Desconto, d.Acrescimo, d.Imposto, d.Juros, d.ValorEfetivacao, d.Status.ToString().ToLowerInvariant(),
            d.AmigosRateio.Select(x => x.AmigoNome).ToArray(), d.TiposRateio.Select(x => x.TipoRateio).ToArray(), d.AnexoDocumento,
            d.Logs.Select(x => new DespesaLogDto(x.Id, DateOnly.FromDateTime(x.DataHoraCadastro), x.Acao, x.Descricao)).ToArray());
}
