using Core.Domain.Entities.Administracao;

namespace Core.Domain.Interfaces;

public interface ITokenService
{
    string GerarAccessToken(Usuario usuario, DateTime expiracaoUtc);
}
