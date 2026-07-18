using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowSpace.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // In-memory distributed cache setup (No Redis server required)
            services.AddDistributedMemoryCache();

            services.AddTransient<FlowSpace.Application.Interfaces.IJwtTokenGenerator, FlowSpace.Infrastructure.Services.JwtTokenGenerator>();

            return services;
        }
    }
}
