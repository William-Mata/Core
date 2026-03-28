using Core.Application.DTOs;
using FluentValidation;

namespace Core.Application.Validators.Financeiro;

public sealed class CriarReceitaRequestValidator : AbstractValidator<CriarReceitaRequest>
{
    public CriarReceitaRequestValidator()
    {
        RuleFor(x => x.Descricao).NotEmpty().WithMessage("A descricao e obrigatoria.");
        RuleFor(x => x.ValorTotal).GreaterThan(0).WithMessage("O valor total deve ser maior que zero.");
        RuleFor(x => x.DataVencimento)
            .GreaterThanOrEqualTo(x => x.DataLancamento)
            .WithMessage("A data de vencimento nao pode ser menor que a data de lancamento.");
    }
}
