using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ByteClick.Web.Services
{
    public class WeeklyCleanupService : IHostedService, IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<WeeklyCleanupService> _logger;
        private Timer _timer;

        public WeeklyCleanupService(IServiceProvider serviceProvider, ILogger<WeeklyCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Haftalık Veri Temizleme Servisi başlatıldı.");

            // İlk çalıştırma zamanını sonraki Pazar gecesi 00:00'a ayarla
            DateTime now = DateTime.Now;
            DateTime nextSunday = now.AddDays(((int)DayOfWeek.Sunday - (int)now.DayOfWeek + 7) % 7).Date;
            if (nextSunday == now.Date) nextSunday = nextSunday.AddDays(7); // Eğer bugün pazarsa haftaya ertelenir

            TimeSpan firstDelay = nextSunday - now;
            TimeSpan weeklyInterval = TimeSpan.FromDays(7);

            // Zamanlayıcıyı kur
            _timer = new Timer(DoCleanupWork, null, firstDelay, weeklyInterval);

            return Task.CompletedTask;
        }

        private async void DoCleanupWork(object state)
        {
            _logger.LogInformation("Haftalık temizleme işlemi başladı...");

            using (var scope = _serviceProvider.CreateScope())
            {
                // Buraya kendi DbContext'ini enjekte etmelisin
                // var context = scope.ServiceProvider.GetRequiredService<YourDbContext>();

                try
                {
                    var boundaryDate = DateTime.Now.AddDays(-7);

                    // Örnek Silme Operasyonu (Ef Core):
                    // var oldAlerts = context.Alerts.Where(a => a.CreatedAt < boundaryDate);
                    // context.Alerts.RemoveRange(oldAlerts);
                    //
                    // var oldLogs = context.TradeLogs.Where(t => t.OpenTime < boundaryDate);
                    // context.TradeLogs.RemoveRange(oldLogs);
                    //
                    // await context.SaveChangesAsync();

                    _logger.LogInformation("7 günden eski eski sinyal ve loglar başarıyla temizlendi.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Haftalık temizlik işlemi sırasında bir hata oluştu.");
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Haftalık Veri Temizleme Servisi durduruluyor.");
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}