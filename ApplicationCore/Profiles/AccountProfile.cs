using ApplicationCore.Entities;
using ApplicationCore.Models;
using AutoMapper;

namespace CommandsService.Profiles
{
    public class AccountProfile : Profile
    {
        public AccountProfile()
        {
            // Source => Target
            CreateMap<ApplicationUserEntity, AccountBaseDto>();
            CreateMap<AccountSignInDto, ApplicationUserEntity>().ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.FirstName + " " + src.LastName));
        }
    }
}
