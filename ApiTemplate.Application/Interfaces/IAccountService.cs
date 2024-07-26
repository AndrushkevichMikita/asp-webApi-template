using ApiTemplate.Application.Models;

namespace ApiTemplate.Application.Interfaces
{
    public interface IAccountService
    {
        Task CreateAccount(CreateAccountDto model);

        Task<RefreshTokenDto> LoginAccount(LoginAccountDto model);

        Task<RefreshTokenDto> CreateNewJwtPair(RefreshTokenDto model, int userId);

        Task SignOut();

        Task ConfirmDigitCode(string digitCode);

        Task SendDigitCodeByEmail(string email);

        Task Delete(string password, int accountId);

        Task<AccountDto> GetCurrent(int userId);
    }
}
