using ByteClick.Data;
using ByteClick.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Net.Http;

namespace ByteClick.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly TradingDbContext _context;
        public HomeController(ILogger<HomeController> logger,TradingDbContext tradingDbContext)
        {
            _logger = logger;
            _context = tradingDbContext;
        }

        public async Task<IActionResult> Index()
        {
            var model = new DashboardViewModel();

            // 1. Verileri Çek
            var alerts = await _context.Alerts.OrderByDescending(a => a.CreatedAt).Take(50).ToListAsync();
            var logs = await _context.TradeLogs.OrderByDescending(t => t.OpenTime).Take(50).ToListAsync();

            model.Alerts = alerts;
            model.TradeLogs = logs;

            // 2. Analiz Hesaplamalarý
            if (logs.Any())
            {
                model.TotalProfit = logs.Sum(t => t.Profit ?? 0);
                model.TotalInvested = logs.Sum(t => t.OpenPrice * t.Lot * 100); // Yaklaþýk margin/yatýrýlan tutar (USD)

                var closedTrades = logs.Where(t => !t.IsOpen).ToList();
                model.WinRate = closedTrades.Any() ?
                    (double)closedTrades.Count(t => t.Profit > 0) / closedTrades.Count * 100 : 0;

                // Ortalama Gecikme (TV -> DB -> MT5)
                model.AvgExecutionDelay = alerts.Where(a => a.ProcessDelayMs > 0).Average(a => a.ProcessDelayMs);
            }

            return View(model);
        }
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
    public class DashboardViewModel
    {
        public List<Alert> Alerts { get; set; } = new();
        public List<TradeLogs> TradeLogs { get; set; } = new();
        public double TotalProfit { get; set; }
        public double WinRate { get; set; }
        public double AvgExecutionDelay { get; set; }
        public double TotalInvested { get; set; }
    }

}
