using Microsoft.AspNetCore.Mvc;

namespace Core.Api.Extensions;

internal static class ErroMensagemExtensions
{
    private static readonly Dictionary<string, string> Mensagens = new(StringComparer.OrdinalIgnoreCase)
    {
        ["receita_nao_encontrada"] = "Receita nao encontrada.",
        ["despesa_nao_encontrada"] = "Despesa nao encontrada.",
        ["conta_bancaria_nao_encontrada"] = "Conta bancaria nao encontrada.",
        ["cartao_nao_encontrado"] = "Cartao nao encontrado.",
        ["status_invalido"] = "A operacao nao pode ser realizada no status atual.",
        ["dados_invalidos"] = "Os dados informados sao invalidos.",
        ["usuario_nao_autenticado"] = "Usuario nao autenticado.",
        ["descricao_obrigatoria"] = "A descricao e obrigatoria.",
        ["valor_total_invalido"] = "O valor total deve ser maior que zero.",
        ["periodo_invalido"] = "A data de vencimento nao pode ser menor que a data de lancamento.",
        ["enum_invalida"] = "Um ou mais valores informados sao invalidos.",
        ["conta_bancaria_obrigatoria"] = "A conta bancaria e obrigatoria para esse tipo de recebimento.",
        ["conta_bancaria_invalida"] = "A conta bancaria informada e invalida.",
        ["area_subarea_invalida"] = "Area ou subarea informada e invalida.",
        ["relacao_area_subarea_invalida"] = "A subarea informada nao pertence a area selecionada.",
        ["tipo_area_invalido"] = "O tipo de area informado e invalido. Use despesa ou receita.",
        ["campo_obrigatorio"] = "Preencha todos os campos obrigatorios.",
        ["saldo_inicial_invalido"] = "O saldo inicial deve ser maior que zero.",
        ["conta_com_pendencias"] = "A conta bancaria possui pendencias e nao pode ser inativada.",
        ["cartao_com_pendencias"] = "O cartao possui pendencias e nao pode ser inativado.",
        ["tipo_invalido"] = "O tipo informado e invalido.",
        ["saldo_invalido"] = "O saldo informado e invalido.",
        ["dados_credito_obrigatorios"] = "Informe limite e datas obrigatorias para cartao de credito.",
        ["email_obrigatorio"] = "O email e obrigatorio.",
        ["senha_obrigatoria"] = "A senha e obrigatoria.",
        ["nome_obrigatorio"] = "O nome e obrigatorio.",
        ["email_invalido"] = "O email informado e invalido.",
        ["email_em_uso"] = "O email informado ja esta em uso.",
        ["perfil_invalido"] = "O perfil informado e invalido.",
        ["usuario_nao_encontrado"] = "Usuario nao encontrado.",
        ["usuario_admin_nao_pode_excluir_a_si_mesmo"] = "Voce nao pode remover o proprio usuario.",
        ["login_bloqueado"] = "Seu acesso foi bloqueado temporariamente por excesso de tentativas invalidas.",
        ["credenciais_invalidas"] = "Email ou senha invalidos.",
        ["primeiro_acesso_requer_criacao_senha"] = "No primeiro acesso, voce deve criar sua senha.",
        ["senha_atual_obrigatoria"] = "A senha atual e obrigatoria.",
        ["nova_senha_obrigatoria"] = "A nova senha e obrigatoria.",
        ["senha_fraca"] = "A senha deve ter no minimo 10 caracteres.",
        ["confirmacao_senha_diferente"] = "A confirmacao de senha deve ser igual a senha.",
        ["senha_atual_incorreta"] = "A senha atual informada esta incorreta.",
        ["primeira_senha_ja_definida"] = "A primeira senha ja foi definida para este usuario.",
        ["refresh_token_obrigatorio"] = "O refresh token e obrigatorio.",
        ["refresh_token_invalido"] = "O refresh token informado e invalido ou expirou.",
        ["usuario_inativo_ou_nao_encontrado"] = "Usuario inativo ou nao encontrado.",
        ["forma_pagamento_invalida"] = "Nao e permitido informar conta bancaria e cartao ao mesmo tempo.",
        ["conta_ou_cartao_obrigatorio"] = "Informe conta bancaria ou cartao para concluir a operacao.",
        ["data_efetivacao_obrigatoria"] = "A data de efetivacao e obrigatoria.",
        ["quantidade_parcelas_invalida"] = "Para pagamento com cartao, informe quantidade de parcelas maior que zero.",
        ["erro_interno"] = "Erro interno do servidor."
    };

    public static ProblemDetails ParaProblemDetails(this string codigo, int status, HttpContext httpContext, IReadOnlyCollection<string>? detalhes = null)
    {
        var mensagem = Mensagens.TryGetValue(codigo, out var valor)
            ? valor
            : "Nao foi possivel processar a solicitacao.";

        var problemDetails = new ProblemDetails
        {
            Status = status,
            Title = ObterTitulo(status),
            Detail = mensagem,
            Type = $"https://httpstatuses.com/{status}",
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions["code"] = codigo;
        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

        if (detalhes is { Count: > 0 })
        {
            problemDetails.Extensions["errors"] = detalhes;
        }

        return problemDetails;
    }

    private static string ObterTitulo(int status) =>
        status switch
        {
            StatusCodes.Status400BadRequest => "Requisicao invalida",
            StatusCodes.Status401Unauthorized => "Nao autorizado",
            StatusCodes.Status404NotFound => "Recurso nao encontrado",
            StatusCodes.Status500InternalServerError => "Erro interno do servidor",
            _ => "Erro na requisicao"
        };
}
