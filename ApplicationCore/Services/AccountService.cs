using ApplicationCore.Entities;
using ApplicationCore.Interfaces;
using ApplicationCore.Models;
using ApplicationCore.Repository;
using HelpersCommon.ExceptionHandler;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ApplicationCore.Services
{
    public class AccountService : IAccountService
    {
        private readonly IRepo<IdentityUserTokenEntity> _userTokenRepo;
        private readonly IEmailTemplateService _emailTemplateService;
        private readonly SignInManager<UserEntity> _signManager;
        private readonly UserManager<UserEntity> _manager;

        public AccountService(IEmailTemplateService emailTemplateService,
                              IRepo<IdentityUserTokenEntity> userTokenRepo,
                              SignInManager<UserEntity> signManager,
                              UserManager<UserEntity> manager)
        {
            _emailTemplateService = emailTemplateService;
            _userTokenRepo = userTokenRepo;
            _signManager = signManager;
            _manager = manager;
        }

        private static string GenerateCode()
        {
            var random = new Random();
            var code = random.Next(1111, 9999).ToString("D4");
            return code;
        }

        private async Task<UserEntity?> DeleteSameNotConfirmed(string email)
        {
            var existingUser = await _manager.FindByEmailAsync(email);
            if (existingUser == null) return null;

            if (existingUser.EmailConfirmed)
                throw new MyApplicationException(ErrorStatus.InvalidData, "User already exists");

            await _manager.DeleteAsync(existingUser);
            return existingUser;
        }

        public async Task SignIn(AccountModel model)
        {
            var appUser = await _manager.FindByEmailAsync(model.Email) ?? throw new MyApplicationException(ErrorStatus.NotFound, "User not found");
            var res = await _signManager.PasswordSignInAsync(appUser, model.Password, false, false);
            if (!res.Succeeded) throw new MyApplicationException(ErrorStatus.InvalidData, "Password or user invalid");
        }

        public async Task SignUp(AccountModel model)
        {
            await DeleteSameNotConfirmed(model.Email);

            var toInsert = new UserEntity
            {
                Role = model.Role,
                Email = model.Email,
                EmailConfirmed = true,
                UserName = model.UserName ?? model.Email,
            };

            var res = await _manager.CreateAsync(toInsert, model.Password);
            if (!res.Succeeded) throw new MyApplicationException(ErrorStatus.InvalidData, string.Join(" ", res.Errors.Select(c => c.Description)));
            await _manager.AddToRoleAsync(toInsert, toInsert.Role.ToString());
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

            await _emailTemplateService.SendDigitCodeAsync(new EmailModel
            {
                UserEmail = appUser.Email,
                DigitCode = digitCode,
                FirstName = appUser.FirstName,
                LastName = appUser.LastName
            });
        }
    }
}
