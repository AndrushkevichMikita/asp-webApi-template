using ApiTemplate.Application.Interfaces;
using ApiTemplate.Application.Models;
using ApiTemplate.Application.Services;
using ApiTemplate.Domain.Entities;
using ApiTemplate.Domain.Interfaces;
using ApiTemplate.Domain.Services;
using ApiTemplate.SharedKernel.ExceptionHandler;
using ApiTemplate.SharedKernel.Extensions;
using AutoMapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace ApiTemplate.Application.Tests
{
    public class AccountServiceTests
    {
        private readonly Mock<IRepo<AccountTokenEntity>> _userTokenRepoMock;
        private readonly Mock<IEmailTemplateService> _emailTemplateServiceMock;
        private readonly ApplicationSignInManager _signInManager;
        private readonly Mock<IMapper> _mapperMock;
        private readonly AccountService _accountService;

        public AccountServiceTests()
        {
            _userTokenRepoMock = new Mock<IRepo<AccountTokenEntity>>();
            _emailTemplateServiceMock = new Mock<IEmailTemplateService>();
            _mapperMock = new Mock<IMapper>();

            // Setting up UserManager and dependencies
            var userManagerMock = new Mock<UserManager<AccountEntity>>(
                Mock.Of<IUserStore<AccountEntity>>(),
                Mock.Of<IOptions<IdentityOptions>>(),
                Mock.Of<IPasswordHasher<AccountEntity>>(),
                Array.Empty<IUserValidator<AccountEntity>>(),
                Array.Empty<IPasswordValidator<AccountEntity>>(),
                Mock.Of<ILookupNormalizer>(),
                Mock.Of<IdentityErrorDescriber>(),
                Mock.Of<IServiceProvider>(),
                Mock.Of<ILogger<UserManager<AccountEntity>>>()
            );

            var contextAccessorMock = new Mock<IHttpContextAccessor>();
            var claimsFactoryMock = new Mock<IUserClaimsPrincipalFactory<AccountEntity>>();
            var configurationMock = new Mock<IConfiguration>();
            var optionsAccessorMock = Options.Create(new IdentityOptions());
            var loggerMock = new Mock<ILogger<SignInManager<AccountEntity>>>();
            var schemesMock = new Mock<IAuthenticationSchemeProvider>();
            var confirmationMock = new Mock<IUserConfirmation<AccountEntity>>();

            var roleManagerMock = new Mock<RoleManager<IdentityRole<int>>>(
                Mock.Of<IRoleStore<IdentityRole<int>>>(),
                Array.Empty<IRoleValidator<IdentityRole<int>>>(),
                Mock.Of<ILookupNormalizer>(),
                Mock.Of<IdentityErrorDescriber>(),
                Mock.Of<ILogger<RoleManager<IdentityRole<int>>>>()
            );

            var applicationUserClaimsPrincipalFactory = new ApplicationUserClaimsPrincipalFactory(
                userManagerMock.Object,
                roleManagerMock.Object,
                optionsAccessorMock
            );

            _signInManager = new ApplicationSignInManager(
                userManagerMock.Object,
                contextAccessorMock.Object,
                claimsFactoryMock.Object,
                configurationMock.Object,
                optionsAccessorMock,
                loggerMock.Object,
                schemesMock.Object,
                confirmationMock.Object,
               applicationUserClaimsPrincipalFactory
            );

            _accountService = new AccountService(
                _emailTemplateServiceMock.Object,
                _userTokenRepoMock.Object,
                _signInManager,
                _mapperMock.Object);
        }

        [Fact]
        public async Task LoginAccount_UserNotFound_ThrowsException()
        {
            // Arrange
            var accountDto = new AccountDto { Email = "test@example.com", Password = "password123", RememberMe = true };
            var userManagerMock = Mock.Get(_signInManager.UserManager);
            userManagerMock.Setup(x => x.FindByEmailAsync(accountDto.Email))
                .ReturnsAsync((AccountEntity)null);

            // Act & Assert
            await Assert.ThrowsAsync<MyApplicationException>(() => _accountService.LoginAccount(accountDto));
        }

        [Fact]
        public async Task CreateAccount_UserCreatedSuccessfully()
        {
            // Arrange
            var accountDto = new AccountDto { Email = "test@example.com", Password = "password123", Role = RoleEnum.Admin };
            var accountEntity = new AccountEntity { Email = accountDto.Email, Role = accountDto.Role };
            _mapperMock.Setup(x => x.Map<AccountEntity>(accountDto)).Returns(accountEntity);

            var userManagerMock = Mock.Get(_signInManager.UserManager);
            userManagerMock.Setup(x => x.CreateAsync(accountEntity, accountDto.Password))
                .ReturnsAsync(IdentityResult.Success);
            userManagerMock.Setup(x => x.AddToRoleAsync(accountEntity, accountDto.Role.ToString()))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            await _accountService.CreateAccount(accountDto);

            // Assert
            _mapperMock.Verify(x => x.Map<AccountEntity>(accountDto), Times.Once);
            userManagerMock.Verify(x => x.CreateAsync(accountEntity, accountDto.Password), Times.Once);
            userManagerMock.Verify(x => x.AddToRoleAsync(accountEntity, accountDto.Role.ToString()), Times.Once);
        }

        [Fact]
        public async Task SendDigitCodeByEmail_UserNotFound_ThrowsException()
        {
            // Arrange
            var email = "test@example.com";
            var userManagerMock = Mock.Get(_signInManager.UserManager);
            userManagerMock.Setup(x => x.FindByEmailAsync(email))
                .ReturnsAsync((AccountEntity)null);

            // Act & Assert
            await Assert.ThrowsAsync<MyApplicationException>(() => _accountService.SendDigitCodeByEmail(email));
        }

        [Fact]
        public async Task SendDigitCodeByEmail_EmailSentSuccessfully()
        {
            // Arrange
            var email = "test@example.com";
            var accountEntity = new AccountEntity { Id = 1, Email = email, FirstName = "John", LastName = "Doe" };
            var userManagerMock = Mock.Get(_signInManager.UserManager);
            userManagerMock.Setup(x => x.FindByEmailAsync(email))
                .ReturnsAsync(accountEntity);

            var dummyQuery = IQueryableExtension.AsAsyncQueryable(new List<AccountTokenEntity>() { new() });
            _userTokenRepoMock.Setup(x => x.GetIQueryable()).Returns(dummyQuery);

            userManagerMock.Setup(x => x.GenerateEmailConfirmationTokenAsync(accountEntity))
                .ReturnsAsync("confirmation-token");

            // Act
            await _accountService.SendDigitCodeByEmail(email);

            // Assert
            _emailTemplateServiceMock.Verify(x => x.SendDigitCodeAsync(It.Is<EmailDto>(dto =>
                dto.UserEmail == email &&
                dto.DigitCode.Length == 4 &&
                dto.FirstName == "John" &&
                dto.LastName == "Doe")), Times.Once);
        }
    }
}