using ApiTemplate.Domain.Entities;
using ApiTemplate.Domain.Interfaces;
using ApiTemplate.Presentation.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace ApiTemplate.Presentation.Web.Tests.Integration.Account
{
    public class AccountTests : BaseIntegrationTest
    {
        public AccountTests(CustomWebApplicationFactory factory) : base(factory) { }

        private async Task<HttpResponseMessage> CreateAccount(CreateAccountModel model)
         => await HTTPClient.PostAsJsonAsync("api/account/signUp", model);

        private async Task<HttpResponseMessage> LoginAccount(LoginAccountModel model)
         => await HTTPClient.PostAsJsonAsync("api/account/signIn", model);

        private async Task<HttpResponseMessage> SendDigitCode(string email)
         => await HTTPClient.PostAsJsonAsync("api/account/digitCode", email);

        private async Task<HttpResponseMessage> ConfirmUserEmail(string code)
         => await HTTPClient.PutAsJsonAsync("api/account/digitCode", code);

        private IRepo<AccountEntity> AccountRepo
            => ServicesScope.ServiceProvider.GetRequiredService<IRepo<AccountEntity>>();

        private async Task UpdateUserEmailConfirm(string email, bool isConfirm)
        {
            var userRepo = AccountRepo;
            var user = await userRepo.GetIQueryable().FirstOrDefaultAsync(x => x.Email == email);
            if (user is null) return;

            user!.EmailConfirmed = isConfirm;
            await userRepo.UpdateAsync(user, true, CancellationToken.None, c => c.EmailConfirmed);
        }

        [Fact]
        public async Task Should_Delete_Same_Unconfirmed_User()
        {
            // Arrange
            var userModel = new CreateAccountModel()
            {
                LastName = "TestUp",
                FirstName = "TestUp",
                Password = "Vdvdrvrd65w!",
                Email = "andruskevicnikitaUp05@gmail.com",
                Role = RoleEnum.SuperAdmin,
            };
            // Act
            await UpdateUserEmailConfirm(userModel.Email, false);
            var first = await CreateAccount(userModel);
            var second = await CreateAccount(userModel);
            // Assert
            Assert.True(first.IsSuccessStatusCode && second.IsSuccessStatusCode);
        }

        [Fact]
        public async Task Should_SignOut_User()
        {
            // Arrange
            var createModel = new CreateAccountModel()
            {
                LastName = "TestSignOut",
                FirstName = "TestSignOut",
                Password = "Vdvdrvrd65w!",
                Email = "andruskevicnikitaSignOut05@gmail.com",
                Role = RoleEnum.SuperAdmin,
            };
            var loginModel = new LoginAccountModel()
            {
                Password = createModel.Password,
                Email = createModel.Email
            };
            // Act
            await UpdateUserEmailConfirm(createModel.Email, false);
            await CreateAccount(createModel);
            await UpdateUserEmailConfirm(createModel.Email, true);
            await LoginAccount(loginModel);
            var signOutRes1 = await HTTPClient.PostAsync("api/account/signOut", null);
            var signOutRes2 = await HTTPClient.PostAsync("api/account/signOut", null);
            // Assert
            Assert.True(signOutRes1.IsSuccessStatusCode && !signOutRes2.IsSuccessStatusCode);
        }

        [Fact]
        public async Task Should_Not_Reach_Secure_Endpoint_Besides_SuperAdmin()
        {
            // Arrange
            var createModel = new CreateAccountModel()
            {
                LastName = "TestSuper",
                FirstName = "TestSuper",
                Password = "Vdvdrvrd65w!",
                Email = "andruskevicnikitaSuper05@gmail.com",
                Role = RoleEnum.Admin,
            };
            var loginModel = new LoginAccountModel()
            {
                Password = createModel.Password,
                Email = createModel.Email
            };
            // Act
            await UpdateUserEmailConfirm(createModel.Email, false);
            var signUpR = await CreateAccount(createModel);
            await UpdateUserEmailConfirm(createModel.Email, true);
            var signInR = await LoginAccount(loginModel);
            var protectedByAuth = await HTTPClient.PostAsync("api/account/onlyForSupAdmin", null);
            // Assert
            Assert.True(signUpR.IsSuccessStatusCode && signInR.IsSuccessStatusCode && !protectedByAuth.IsSuccessStatusCode);
        }

        [Fact]
        public async Task Should_Reach_Secure_Endpoint_By_SuperAdmin()
        {
            // Arrange
            var createModel = new CreateAccountModel()
            {
                LastName = "TestSuper",
                FirstName = "TestSuper",
                Password = "Vdvdrvrd65w!",
                Email = "andruskevicnikitaSuper05@gmail.com",
                Role = RoleEnum.SuperAdmin,
            };
            var loginModel = new LoginAccountModel()
            {
                Password = createModel.Password,
                Email = createModel.Email
            };
            // Act
            await UpdateUserEmailConfirm(createModel.Email, false);
            var signUpR = await CreateAccount(createModel);
            await UpdateUserEmailConfirm(createModel.Email, true);
            var signInR = await LoginAccount(loginModel);
            var protectedByAuth = await HTTPClient.PostAsync("api/account/onlyForSupAdmin", null);
            // Assert
            Assert.True(signUpR.IsSuccessStatusCode && signInR.IsSuccessStatusCode && protectedByAuth.IsSuccessStatusCode);
        }

        [Fact]
        public async Task Should_Confrim_User_Email()
        {
            // Arrange
            var createModel = new CreateAccountModel()
            {
                LastName = "TestDigitCode",
                FirstName = "TestDigitCode",
                Password = "Vdvdrvrd65w!",
                Email = "andruskevicnikitaDigitCode05@gmail.com",
                Role = RoleEnum.SuperAdmin,
            };
            var userTokenRepo = ServicesScope.ServiceProvider.GetRequiredService<IRepo<AccountTokenEntity>>();
            // Act
            await UpdateUserEmailConfirm(createModel.Email, false);
            var signUpR = await CreateAccount(createModel);
            var sendRes = await SendDigitCode(createModel.Email);

            var isNewTokenAppears = await userTokenRepo.GetIQueryable().Where(x => x.User.Email == createModel.Email).FirstOrDefaultAsync();
            var confirmRes = await ConfirmUserEmail(isNewTokenAppears!.Name);
            // Assert
            Assert.True(sendRes.IsSuccessStatusCode && confirmRes.IsSuccessStatusCode);
        }

        [Fact]
        public async Task Should_Delete_User()
        {
            // Arrange
            var createModel = new CreateAccountModel()
            {
                LastName = "TestDelete",
                FirstName = "TestDelete",
                Password = "Vdvdrvrd65w!",
                Email = "andruskevicnikitadelete05@gmail.com",
                Role = RoleEnum.SuperAdmin,
            };
            var loginModel = new LoginAccountModel()
            {
                Password = createModel.Password,
                Email = createModel.Email
            };
            // Act
            await UpdateUserEmailConfirm(createModel.Email, false);
            var signUpR = await CreateAccount(createModel);
            await UpdateUserEmailConfirm(createModel.Email, true);
            var signInR = await LoginAccount(loginModel);
            using var requestMessage = new HttpRequestMessage(HttpMethod.Delete, "api/account");
            requestMessage.Content = new StringContent(JsonSerializer.Serialize(loginModel.Password), Encoding.UTF8, "application/json");
            var deleteR = await HTTPClient.SendAsync(requestMessage);
            // Assert
            Assert.True(signUpR.IsSuccessStatusCode && signInR.IsSuccessStatusCode && deleteR.IsSuccessStatusCode);
        }
    }
}