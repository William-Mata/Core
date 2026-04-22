using Core.Application.DTOs.Financeiro;
using Core.Domain.Enums.Financeiro;
using FluentValidation;

namespace Core.Application.Validators.Financeiro;

public sealed class CriarReceitaRequestValidator : AbstractValidator<CriarReceitaRequest>
{
    public CriarReceitaRequestValidator()
    {
        RuleFor(x => x.Descricao).NotEmpty().WithMessage("A descricao e obrigatoria.");
        RuleFor(x => x.ValorTotal).GreaterThan(0).WithMessage("O valor total deve ser maior que zero.");
        RuleFor(x => x.DataVencimento)
            .Must((request, dataVencimento) => dataVencimento >= DateOnly.FromDateTime(request.DataLancamento))
            .When(x => x.TipoRecebimento != TipoRecebimento.CartaoCredito || x.DataVencimento != default)
            .WithMessage("A data de vencimento nao pode ser menor que a data de lancamento.");
    }
}
