using Core.Application.DTOs.Financeiro;
using FluentValidation;

namespace Core.Application.Validators.Financeiro;

public sealed class CriarCartaoRequestValidator : AbstractValidator<CriarCartaoRequest>
{
    public CriarCartaoRequestValidator()
    {
        RuleFor(x => x.Descricao).NotEmpty().WithMessage("A descricao e obrigatoria.");
        RuleFor(x => x.Bandeira).NotEmpty().WithMessage("A bandeira e obrigatoria.");
        RuleFor(x => x.Tipo).NotEmpty().WithMessage("O tipo do cartao e obrigatorio.");
    }
}
