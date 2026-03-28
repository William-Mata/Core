using Core.Application.DTOs.Financeiro;
using FluentValidation;

namespace Core.Application.Validators.Financeiro;

public sealed class CriarContaBancariaRequestValidator : AbstractValidator<CriarContaBancariaRequest>
{
    public CriarContaBancariaRequestValidator()
    {
        RuleFor(x => x.Descricao).NotEmpty().WithMessage("A descricao e obrigatoria.");
        RuleFor(x => x.Banco).NotEmpty().WithMessage("O banco e obrigatorio.");
        RuleFor(x => x.Agencia).NotEmpty().WithMessage("A agencia e obrigatoria.");
        RuleFor(x => x.Numero).NotEmpty().WithMessage("O numero da conta e obrigatorio.");
        RuleFor(x => x.SaldoInicial).GreaterThan(0).WithMessage("O saldo inicial deve ser maior que zero.");
    }
}
