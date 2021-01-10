using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Scada.Models;
using ScadaUserService.Contracts;

namespace ScadaUserService
{
    public class MappingUser : Profile
    {
        public MappingUser()
        {
            CreateMap<User, UserAuthenticateOutContract>();
            CreateMap<UserAuthenticateOutContract, User>();
            CreateMap<UserRegistrationInContract, User>();
        }
    }

    public static class MapperServiceCollectionExtension
    {
        public static IServiceCollection ConfigureUserMapping(this IServiceCollection serviceCollection)
        {
            var mapperConfiguration = new MapperConfiguration(mc =>
            {
                mc.AddProfile(new MappingUser());
            });

            IMapper mapper = mapperConfiguration.CreateMapper();
            serviceCollection.AddSingleton(mapper);

            return serviceCollection;
        }
    }
}