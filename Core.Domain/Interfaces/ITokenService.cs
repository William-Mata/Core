using Core.Domain.Entities;

namespace Core.Domain.Interfaces;

public interface ITokenService
{
    string GerarAccessToken(Usuario usuario, DateTime expiracaoUtc);
}
