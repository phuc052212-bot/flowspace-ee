using FlowSpace.Domain.Interfaces;
using FlowSpace.Persistence.Contexts;
using FlowSpace.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowSpace.Persistence
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
        {
            var rawConnectionString = configuration.GetConnectionString("DefaultConnection");
            
            // Fallback đọc DATABASE_URL từ Render nếu DefaultConnection trống
            if (string.IsNullOrEmpty(rawConnectionString))
            {
                rawConnectionString = Environment.GetEnvironmentVariable("DATABASE_URL");
            }

            var connectionString = ParseConnectionString(rawConnectionString);

            services.AddDbContext<FlowSpaceDbContext>(options =>
                options.UseNpgsql(connectionString,
                    b => b.MigrationsAssembly(typeof(FlowSpaceDbContext).Assembly.FullName)));

            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            return services;
        }

        private static string ParseConnectionString(string? rawUrl)
        {
            if (string.IsNullOrEmpty(rawUrl)) return string.Empty;

            // Nếu không phải định dạng URI postgres:// thì trả về nguyên bản
            if (!rawUrl.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase))
            {
                return rawUrl;
            }

            try
            {
                var uri = new Uri(rawUrl);
                var userInfo = uri.UserInfo.Split(':');
                var username = userInfo[0];
                var password = userInfo.Length > 1 ? userInfo[1] : string.Empty;
                var host = uri.Host;
                var port = uri.Port;
                var database = uri.AbsolutePath.TrimStart('/');

                // Render hoặc Supabase đôi khi yêu cầu SSL
                return $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true;";
            }
            catch
            {
                return rawUrl;
            }
        }
    }
}
