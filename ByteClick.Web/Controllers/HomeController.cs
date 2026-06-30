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

            // Son hesap snapshot'ý
            var account = await _context.AccountSnapshots
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();


            _context.AccountSnapshots.RemoveRange(
               await _context.AccountSnapshots
                   .Where(x => x.CreatedAt.AddDays(3) >= DateTime.Now)
                   .ToListAsync());

            var model = new DashboardViewModel
            {
                TotalProfit = trades.Sum(x => x.Profit ?? 0),
                WinRate = trades.Count > 0
                                    ? (double)trades.Count(x => x.Profit > 0) / trades.Count * 100
                                    : 0,
                AvgExecutionDelay = alerts.Any() ? alerts.Average(x => x.DelayMs + x.ProcessDelayMs) : 0,
                Alerts = alerts,
                TradeLogs = trades,
                Account = account
            };

            return View(model);
        }


        [HttpPost]
        public async Task<IActionResult> DeleteAlerts(List<int> ids)
        {
            if (ids == null || !ids.Any()) return Json(new { success = false });

             
            var items = _context.Alerts.Where(x => ids.Contains(x.Id));
            _context.Alerts.RemoveRange(items);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteTradeLogs(List<int> ids)
        {
            if (ids == null || !ids.Any()) return Json(new { success = false });

             
            var items = _context.TradeLogs.Where(x => ids.Contains(x.Id));
            _context.TradeLogs.RemoveRange(items);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }
    }

    public class DashboardViewModel
    {
        public double TotalProfit { get; set; }
        public double WinRate { get; set; }
        public double AvgExecutionDelay { get; set; }
        public List<Alert> Alerts { get; set; } = new();
        public List<TradeLogs> TradeLogs { get; set; } = new();
        public AccountSnapshot? Account { get; set; }
    }
}