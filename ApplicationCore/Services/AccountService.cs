using ApplicationCore.Entities;
using ApplicationCore.Interfaces;
using ApplicationCore.Models;
using ApplicationCore.Repository;
using AutoMapper;
using HelpersCommon.ExceptionHandler;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ApplicationCore.Services
{
    public class AccountService : IAccountService
    {
        private readonly IRepo<IdentityUserTokenEntity> _userTokenRepo;
        private readonly IEmailTemplateService _emailTemplateService;
        private readonly ApplicationSignInManager _signInManager;
        private readonly UserManager<ApplicationUserEntity> _manager;
        private readonly IRepo<ApplicationUserEntity> _userRepo;
        private readonly IMapper _mapper;

        public AccountService(IEmailTemplateService emailTemplateService,
                              IRepo<IdentityUserTokenEntity> userTokenRepo,
                              ApplicationSignInManager signManager,
                              UserManager<ApplicationUserEntity> manager,
                              IRepo<ApplicationUserEntity> userRepo,
                              IMapper mapper)
        {
            _emailTemplateService = emailTemplateService;
            _userTokenRepo = userTokenRepo;
            _signInManager = signManager;
            _userRepo = userRepo;
            _manager = manager;
            _mapper = mapper;
        }

        private static string GenerateCode()
        {
            var random = new Random();
            var code = random.Next(1111, 9999).ToString("D4");
            return code;
        }

        private async Task<ApplicationUserEntity> DeleteSameNotConfirmed(string email)
        {
            var existingUser = await _manager.FindByEmailAsync(email);
            if (existingUser == null) return null;

            if (existingUser.EmailConfirmed)
                throw new MyApplicationException(ErrorStatus.InvalidData, "User already exists");

            await _manager.DeleteAsync(existingUser);
            return existingUser;
        }

        public Task SignOut()
            => _signInManager.SignOutAsync();

        public async Task<(string token, string refreshToken)> SignIn(AccountSignInDto model)
        {
            var appUser = await _manager.FindByEmailAsync(model.Email) ?? throw new MyApplicationException(ErrorStatus.NotFound, "User not found");
            if (!await _manager.IsEmailConfirmedAsync(appUser)) throw new MyApplicationException(ErrorStatus.InvalidData, "Email unconfirmed");

            var res = await _signInManager.PasswordSignInAsync(appUser, model.Password, model.RememberMe ?? false, false);
            if (!res.Succeeded) throw new MyApplicationException(ErrorStatus.InvalidData, "Password or user invalid");

            var token = await _signInManager.GenerateJwtTokenAsync(appUser);
            var refreshToken = await _signInManager.GenerateRefreshTokenAsync(appUser);
            return (token, refreshToken);
        }

        public async Task SignUp(AccountSignInDto model)
        {
            await DeleteSameNotConfirmed(model.Email);

            var toInsert = _mapper.Map<ApplicationUserEntity>(model);
            var res = await _manager.CreateAsync(toInsert, model.Password);
            if (!res.Succeeded) throw new MyApplicationException(ErrorStatus.InvalidData, string.Join(" ", res.Errors.Select(c => c.Description)));
            await _manager.AddToRoleAsync(toInsert, toInsert.Role.ToString());
        }

        public async Task<AccountBaseDto> GetCurrent(int userId)
        {
            var user = await _userRepo.GetIQueryable().FirstOrDefaultAsync(x => x.Id == userId);
            if (user is null) return null;
            return _mapper.Map<AccountBaseDto>(user);
        }

        public async Task SendDigitCodeByEmail(string email)
        {
            var appUser = await _manager.FindByEmailAsync(email) ?? throw new MyApplicationException(ErrorStatus.NotFound, "User not found");

            var asString = TokenEnum.EmailToken.ToString();
            var userEmailTokens = await _userTokenRepo.GetIQueryable()
                                                      .Where(x => x.UserId == appUser.Id && x.LoginProvider == asString)
                                                      .ToListAsync();
            if (userEmailTokens != null)
                await _userTokenRepo.DeleteAsync(userEmailTokens);

            var digitCode = GenerateCode();
            await _userTokenRepo.InsertAsync(new IdentityUserTokenEntity
            {
                UserId = appUser.Id,
                Name = digitCode,
                LoginProvider = asString,
                Value = await _manager.GenerateEmailConfirmationTokenAsync(appUser)
            }, true);

            await _emailTemplateService.SendDigitCodeParallelAsync(new List<EmailDtoModel>{new() {
                UserEmail = appUser.Email,
                DigitCode = digitCode,
                FirstName = appUser.FirstName,
                LastName = appUser.LastName
            } });
        }

        public async Task ConfirmDigitCode(string digitCode)
        {
            var tokens = await _userTokenRepo.GetIQueryable()
                                             .Include(x => x.User)
                                             .Where(x => x.Name == digitCode && x.LoginProvider == TokenEnum.EmailToken.ToString())
                                             .ToListAsync();

            if (tokens.Count > 1 || tokens.Count == 0)
                throw new MyApplicationException(ErrorStatus.InvalidData, "This code is invalid, please request a new one");

            var token = tokens.First();
            var result = await _manager.ConfirmEmailAsync(token.User, token.Value);
            if (!result.Succeeded)
                throw new MyApplicationException(ErrorStatus.InvalidData, result.Errors.FirstOrDefault()!.Description);

            await _userTokenRepo.DeleteAsync(token, true);
        }

        public async Task Delete(string password, int accountId)
        {
            var account = await _manager.FindByIdAsync(accountId.ToString());
            var res = await _manager.CheckPasswordAsync(account, password);
            if (!res) throw new MyApplicationException(ErrorStatus.InvalidData, "Password invalid");
            await _manager.DeleteAsync(account);
        }
    }
}
