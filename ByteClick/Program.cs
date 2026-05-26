using ByteClick.Data;
using Microsoft.EntityFrameworkCore;

namespace ByteClick
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();


            // 2. SQL Server ba�lant�s�
            builder.Services.AddDbContext<TradingDbContext>(options =>
             options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.WebHost.ConfigureKestrel(serverOptions =>
            {
                serverOptions.ListenAnyIP(5001);
                serverOptions.ListenAnyIP(7021);
            });

            // Program.cs içinde AddControllers kısmını şu şekilde güncelle:
            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true; // Harf duyarlılığını kapatır
                    options.JsonSerializerOptions.PropertyNamingPolicy = null; // PascalCase'e izin verir
                });

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TradingDbContext>();
                db.Database.Migrate();
            }
            app.UseCors(x => x
    .AllowAnyMethod()
    .AllowAnyHeader()
    .SetIsOriginAllowed(origin => true) // T�m originlere izin ver
    .AllowCredentials());
            // Configure the HTTP request pipeline.

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.MapGet("/", () => new
            {
                service = "ByteClick Trading API",
                version = "1.0",
                status = "online",
                timestamp = DateTime.Now,
                endpoints = new
                {
                    health = "GET /webhook/tradingview/health",
                    post_alert = "POST /webhook/tradingview",
                    get_latest = "GET /webhook/tradingview/latest",
                    get_all = "GET /webhook/tradingview/all",
                    clear = "DELETE /webhook/tradingview/clear"
                }
            });

            app.Run();
        }
    }
}
