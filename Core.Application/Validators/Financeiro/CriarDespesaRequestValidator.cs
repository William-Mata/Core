using Core.Application.DTOs.Financeiro;
using Core.Domain.Enums;
using FluentValidation;

namespace Core.Application.Validators.Financeiro;

public sealed class CriarDespesaRequestValidator : AbstractValidator<CriarDespesaRequest>
{
    public CriarDespesaRequestValidator()
    {
        RuleFor(x => x.Descricao).NotEmpty().WithMessage("A descricao e obrigatoria.");
        RuleFor(x => x.ValorTotal).GreaterThan(0).WithMessage("O valor total deve ser maior que zero.");
        RuleFor(x => x.DataVencimento)
            .Must((request, dataVencimento) => dataVencimento >= DateOnly.FromDateTime(request.DataLancamento))
            .When(x => x.TipoPagamento != TipoPagamento.CartaoCredito || x.DataVencimento != default)
            .WithMessage("A data de vencimento nao pode ser menor que a data de lancamento.");
    }
}
