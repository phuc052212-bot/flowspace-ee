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
            // 1. Thử lấy từ configuration mặc định
            var rawConnectionString = configuration.GetConnectionString("DefaultConnection");

            // 2. Thử lấy trực tiếp từ biến môi trường ConnectionStrings__DefaultConnection (Render tự sinh)
            if (string.IsNullOrEmpty(rawConnectionString))
            {
                rawConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
            }

            // 3. Thử lấy từ biến DATABASE_URL (Render database URL)
            if (string.IsNullOrEmpty(rawConnectionString))
            {
                rawConnectionString = Environment.GetEnvironmentVariable("DATABASE_URL");
            }

            // 4. Thử lấy từ configuration key trực tiếp
            if (string.IsNullOrEmpty(rawConnectionString))
            {
                rawConnectionString = configuration["ConnectionStrings:DefaultConnection"];
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
                // Loại bỏ tiền tố postgres://
                var trimmed = rawUrl.Substring("postgres://".Length);

                // Cú pháp: username:password@host:port/database
                var atIndex = trimmed.IndexOf('@');
                if (atIndex == -1) return rawUrl;

                var credentialsPart = trimmed.Substring(0, atIndex);
                var serverPart = trimmed.Substring(atIndex + 1);

                var colonIndex = credentialsPart.IndexOf(':');
                if (colonIndex == -1) return rawUrl;

                var username = credentialsPart.Substring(0, colonIndex);
                var password = credentialsPart.Substring(colonIndex + 1);

                var slashIndex = serverPart.IndexOf('/');
                if (slashIndex == -1) return rawUrl;

                var hostPortPart = serverPart.Substring(0, slashIndex);
                var database = serverPart.Substring(slashIndex + 1);

                // Tách host và port
                var host = hostPortPart;
                var port = "5432"; // mặc định của postgres

                var hostColonIndex = hostPortPart.IndexOf(':');
                if (hostColonIndex != -1)
                {
                    host = hostPortPart.Substring(0, hostColonIndex);
                    port = hostPortPart.Substring(hostColonIndex + 1);
                }

                // Loại bỏ phần query parameters nếu có (ví dụ ?sslmode=require)
                var questionMarkIndex = database.IndexOf('?');
                if (questionMarkIndex != -1)
                {
                    database = database.Substring(0, questionMarkIndex);
                }

                return $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true;";
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Lỗi phân tích chuỗi kết nối: {ex.Message}");
                return rawUrl;
            }
        }
    }
}
