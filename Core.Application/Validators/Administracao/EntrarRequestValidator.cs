using Core.Application.DTOs.Administracao;
using FluentValidation;

namespace Core.Application.Validators.Administracao;

public sealed class EntrarRequestValidator : AbstractValidator<EntrarRequest>
{
    public EntrarRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("O email e obrigatorio.")
            .EmailAddress().WithMessage("O email informado e invalido.");

        RuleFor(x => x.Senha)
            .NotEmpty().WithMessage("A senha e obrigatoria.")
            .MinimumLength(10).WithMessage("A senha deve ter no minimo 10 caracteres.");
    }
}
