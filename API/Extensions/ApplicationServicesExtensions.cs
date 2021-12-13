using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using API.Data;
using Microsoft.EntityFrameworkCore;
using API.Interfaces;
using API.Services;
using API._Helpers;

namespace API.Extensions
{
    public static class ApplicationServicesExtensions
    {
        public static IServiceCollection AddApplicationService(this IServiceCollection services,IConfiguration config )
        {
        services.AddScoped<ITokenServices,TokenServices>();
        services.AddScoped<IUserRepstory,UserRepisotry>();
        services.AddAutoMapper(typeof(AutoMapperProfiles).Assembly);
            services.AddDbContext<DataContext>(
                options=>{
                    options.UseSqlite(config.GetConnectionString("DefaultConnection"));
                }
            );
            return services;
        }
    }
}