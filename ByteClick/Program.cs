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


            // 2. SQL Server baðlantýsý
            builder.Services.AddDbContext<TradingDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // 3. Tarayýcýdan (TradingView) gelen istekleri engellememesi için CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("TradingViewPolicy", policy =>
                {
                    policy.WithOrigins("https://www.tradingview.com") // Sadece TradingView'a izin ver
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials(); // Eðer cookie/auth gerekiyorsa
                });
            });

             
            var app = builder.Build();


            app.UseCors(x => x
    .AllowAnyMethod()
    .AllowAnyHeader()
    .SetIsOriginAllowed(origin => true) // Tüm originlere izin ver
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
