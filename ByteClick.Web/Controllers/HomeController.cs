using ByteClick.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ByteClick.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly TradingDbContext _context;

        public HomeController(TradingDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var alerts = await _context.Alerts
                .OrderByDescending(x => x.CreatedAt)
                .Take(100)
                .ToListAsync();

            var trades = await _context.TradeLogs
                .OrderByDescending(x => x.OpenTime)
                .Take(100)
                .ToListAsync();

            var model = new DashboardViewModel
            {
                TotalProfit = trades.Sum(x => x.Profit ?? 0),
                WinRate = trades.Count > 0
                    ? (double)trades.Count(x => x.Profit > 0) / trades.Count * 100
                    : 0,
                AvgExecutionDelay = alerts.Any() ? alerts.Average(x => x.DelayMs + x.ProcessDelayMs) : 0,
                TotalInvested = trades.Sum(x => x.Lot * x.OpenPrice),
                Alerts = alerts,
                TradeLogs = trades
            };

            return View(model);
        }
    }

    public class DashboardViewModel
    {
        public double TotalProfit { get; set; }
        public double WinRate { get; set; }
        public double AvgExecutionDelay { get; set; }
        public double TotalInvested { get; set; }
        public List<Alert> Alerts { get; set; } = new();
        public List<TradeLogs> TradeLogs { get; set; } = new();
    }
}