using ApiTemplate.Application.Models;

namespace ApiTemplate.Application.Interfaces
{
    public interface IAccountService
    {
        Task CreateAccount(AccountDto model);

        Task<RefreshTokenDto> LoginAccount(AccountDto model);

        Task<RefreshTokenDto> CreateNewJwtPair(RefreshTokenDto model);

        Task SignOut();

        Task ConfirmDigitCode(string digitCode);

        Task SendDigitCodeByEmail(string email);

        Task Delete(string password, int accountId);

        Task<AccountDto> GetCurrent(int userId);
    }
}
