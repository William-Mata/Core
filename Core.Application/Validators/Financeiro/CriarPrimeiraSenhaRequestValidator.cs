using Core.Application.DTOs;
using FluentValidation;

namespace Core.Application.Validators.Financeiro;

public sealed class CriarPrimeiraSenhaRequestValidator : AbstractValidator<CriarPrimeiraSenhaRequest>
{
    public CriarPrimeiraSenhaRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("O email e obrigatorio.")
            .EmailAddress().WithMessage("O email informado e invalido.");

        RuleFor(x => x.Senha)
            .NotEmpty().WithMessage("A senha e obrigatoria.")
            .MinimumLength(10).WithMessage("A senha deve ter no minimo 10 caracteres.");

        RuleFor(x => x.ConfirmarSenha)
            .NotEmpty().WithMessage("A confirmacao de senha e obrigatoria.")
            .Equal(x => x.Senha).WithMessage("A confirmacao de senha deve ser igual a senha.");
    }
}
