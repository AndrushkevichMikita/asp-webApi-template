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
            CreateMap<UserEntity, AccountBaseDto>();
            CreateMap<AccountSignInDto, UserEntity>().ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.FirstName + " " + src.LastName));
        }
    }
}
