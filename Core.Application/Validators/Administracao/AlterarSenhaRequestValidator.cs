using Core.Application.DTOs.Administracao;
using FluentValidation;

namespace Core.Application.Validators.Administracao;

public sealed class AlterarSenhaRequestValidator : AbstractValidator<AlterarSenhaRequest>
{
    public AlterarSenhaRequestValidator()
    {
        RuleFor(x => x.SenhaAtual)
            .NotEmpty()
            .WithMessage("A senha atual e obrigatoria.");

        RuleFor(x => x.NovaSenha)
            .NotEmpty()
            .WithMessage("A nova senha e obrigatoria.")
            .MinimumLength(10)
            .WithMessage("A senha deve ter no minimo 10 caracteres.");

        RuleFor(x => x.ConfirmarSenha)
            .NotEmpty()
            .WithMessage("A confirmacao de senha e obrigatoria.")
            .Equal(x => x.NovaSenha)
            .WithMessage("A confirmacao de senha deve ser igual a senha.");
    }
}
