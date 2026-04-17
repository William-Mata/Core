using Core.Application.DTOs.Administracao;
using FluentValidation;

namespace Core.Application.Validators.Administracao;

public sealed class SalvarUsuarioRequestValidator : AbstractValidator<SalvarUsuarioRequest>
{
    public SalvarUsuarioRequestValidator()
    {
        RuleFor(x => x.DataNascimento)
            .Must(DataNascimentoValida)
            .WithMessage("A data de nascimento informada e invalida.");

        RuleFor(x => x.Nome)
            .NotEmpty()
            .WithMessage("O nome e obrigatorio.");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("O email e obrigatorio.")
            .Matches("^[^\\s@]+@[^\\s@]+\\.[^\\s@]+$")
            .WithMessage("O email informado e invalido.");

        RuleFor(x => x.Perfil)
            .NotEmpty()
            .WithMessage("O perfil e obrigatorio.")
            .Must(x => x is not null && (x.Equals("ADMIN", StringComparison.OrdinalIgnoreCase) || x.Equals("USER", StringComparison.OrdinalIgnoreCase)))
            .WithMessage("O perfil informado e invalido.");

        RuleFor(x => x.ModulosAtivos)
            .Must(ModulosValidos)
            .WithMessage("Os modulos informados sao invalidos.");
    }

    private static bool DataNascimentoValida(DateOnly? dataNascimento)
    {
        if (dataNascimento is null)
        {
            return true;
        }

        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var dataMinima = new DateOnly(1900, 1, 1);
        return dataNascimento.Value >= dataMinima && dataNascimento.Value <= hoje;
    }

    private static bool ModulosValidos(IReadOnlyCollection<SalvarModuloUsuarioRequest>? modulos)
    {
        if (modulos is null)
        {
            return true;
        }

        foreach (var modulo in modulos)
        {
            if (string.IsNullOrWhiteSpace(modulo.Id))
            {
                return false;
            }

            foreach (var tela in modulo.Telas ?? [])
            {
                if (string.IsNullOrWhiteSpace(tela.Id))
                {
                    return false;
                }

                foreach (var funcionalidade in tela.Funcionalidades ?? [])
                {
                    if (string.IsNullOrWhiteSpace(funcionalidade.Id))
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }
}
