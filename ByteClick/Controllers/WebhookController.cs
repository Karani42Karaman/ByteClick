using ByteClick.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Globalization;

namespace ByteClick.Controllers
{
    [ApiController]
    [Route("webhook/tradingview")]
    public class TradingViewWebhookController : ControllerBase
    {
        private readonly TradingDbContext _context;
        private readonly ILogger<TradingViewWebhookController> _logger;

        private static readonly TimeZoneInfo TR_ZONE =
            TimeZoneInfo.CreateCustomTimeZone("Turkey", TimeSpan.FromHours(3), "Turkey", "Turkey");

        public TradingViewWebhookController(TradingDbContext context, ILogger<TradingViewWebhookController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ─── TradingView Sinyali ───────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> ReceiveAlert([FromBody] TradingViewAlertRequest request)
        {
             
            if (DateTime.Now >= new DateTime(2026, 7, 19))
            {
                return BadRequest(new { error = "Bi hata var " });
            }

            var sw = Stopwatch.StartNew();
            var receiveTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TR_ZONE);

            try
            {
                if (request == null) return BadRequest(new { error = "Veri boş" });

                decimal price = 0;
                if (!string.IsNullOrEmpty(request.Price))
                {
                    var priceStr = request.Price.Replace(",", ".");
                    decimal.TryParse(priceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out price);
                }

                DateTime tvTime = receiveTime;
                if (DateTime.TryParse(request.Time, out var parsedTime))
                    tvTime = parsedTime;

                var alert = new Alert
                {
                    Symbol = request.Ticker ?? "Bilinmiyor",
                    Action = request.Action?.ToUpper() ?? "WAIT",
                    Price = price,
                    Lot = request.Lot ?? 0.01m,
                    SL = request.SL ?? 0,
                    TP = request.TP ?? 0,
                    Interval = request.Interval ?? "",
                    Exchange = request.Exchange ?? "",
                    Volume = request.Volume ?? "0",
                    Raw = request.Comment ?? "",
                    TVTimestamp = tvTime,
                    CreatedAt = receiveTime,
                    IsProcessed = false,
                    DelayMs = tvTime.Millisecond
                };

                _context.Alerts.Add(alert);
                await _context.SaveChangesAsync();
                sw.Stop();

                _logger.LogInformation($"🚀 {alert.Symbol} {alert.Action} | Gecikme: {alert.DelayMs:F2}ms");
                return Ok(new { status = "success", delay_ms = alert.DelayMs });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Alert kaydı patladı!");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // ─── MT5 Emir Açılışı ─────────────────────────────────────────────────
        [HttpPost("open")]
        public async Task<IActionResult> LogOpen([FromBody] TradeLogs log)
        {
            try
            {
                if (log == null) return BadRequest("JSON parse edilemedi.");
                if (await _context.TradeLogs.AnyAsync(x => x.Ticket == log.Ticket))
                    return BadRequest("Bu ticket zaten sistemde var.");

                log.OpenTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TR_ZONE);
                log.IsOpen = true;

                _context.TradeLogs.Add(log);
                await _context.SaveChangesAsync();
                return Ok(new { status = "success" });
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        // ─── MT5 Emir Kapanışı ────────────────────────────────────────────────
        [HttpPost("close")]
        public async Task<IActionResult> LogClose([FromBody] TradeUpdateDTO update)
        {
            try
            {
                var existingLog = await _context.TradeLogs.FirstOrDefaultAsync(x => x.Ticket == update.Ticket);
                if (existingLog == null) return NotFound("Güncellenecek açık işlem bulunamadı.");

                existingLog.ClosePrice = update.ClosePrice;
                existingLog.Profit = update.Profit;
                existingLog.CloseTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TR_ZONE);
                existingLog.IsOpen = false;

                await _context.SaveChangesAsync();
                return Ok(new { status = "success" });
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        // ─── MT5 Hesap Durumu (Her Saniye Gelir) ─────────────────────────────
        [HttpPost("account")]
        public async Task<IActionResult> UpdateAccount([FromBody] AccountSnapshotDTO dto)
        {
            try
            {
                if (dto == null) return BadRequest("Veri boş");

                var snap = new AccountSnapshot
                {
                    Balance = dto.Balance,
                    Equity = dto.Equity,
                    Margin = dto.Margin,
                    FreeMargin = dto.FreeMargin,
                    MarginLevel = dto.MarginLevel,
                    OpenPositions = dto.OpenPositions,
                    CreatedAt = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TR_ZONE)
                };

                _context.AccountSnapshots.Add(snap);

                // Eski kayıtları temizle – sadece son 500 snapshot'ı tut
                var count = await _context.AccountSnapshots.CountAsync();
                if (count > 500)
                {
                    var old = await _context.AccountSnapshots
                        .OrderBy(x => x.CreatedAt)
                        .Take(count - 500)
                        .ToListAsync();
                    _context.AccountSnapshots.RemoveRange(old);
                }

                await _context.SaveChangesAsync();
                return Ok(new { status = "success" });
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        // ─── Son Hesap Durumunu Getir ─────────────────────────────────────────
        [HttpGet("account")]
        public async Task<IActionResult> GetAccount()
        {
            var snap = await _context.AccountSnapshots
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();

            if (snap == null)
                return Ok(new { status = "no_data" });

            return Ok(snap);
        }

        // ─── Son İşlenmemiş Sinyal ────────────────────────────────────────────
        [HttpGet("latest")]
        public async Task<IActionResult> GetLatestUnprocessedAlert()
        {
            var alert = await _context.Alerts
                .Where(a => !a.IsProcessed)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();

            if (alert == null) return Ok(new { status = "no_alerts" });

            alert.IsProcessed = true;
            alert.ProcessDelayMs = (TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TR_ZONE) - alert.CreatedAt).TotalMilliseconds;
            await _context.SaveChangesAsync();

            return Ok(new { status = "success", alert = alert });
        }

        // ─── İstatistikler ────────────────────────────────────────────────────
        [HttpGet("stats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            var logs = await _context.TradeLogs.ToListAsync();
            var alerts = await _context.Alerts.ToListAsync();
            return Ok(new
            {
                TotalProfit = logs.Sum(x => x.Profit),
                TotalTrades = logs.Count,
                WinRate = logs.Count > 0 ? (double)logs.Count(x => x.Profit > 0) / logs.Count * 100 : 0,
                AverageLatency = alerts.Any() ? alerts.Average(x => x.DelayMs) : 0,
                LastTrades = logs.OrderByDescending(x => x.OpenTime).Take(10).ToList()
            });
        }
    }
}

// ─── DTO'lar ──────────────────────────────────────────────────────────────────
public class TradeUpdateDTO
{
    public long Ticket { get; set; }
    public double ClosePrice { get; set; }
    public double Profit { get; set; }
}

public class AccountSnapshotDTO
{
    public double Balance { get; set; }
    public double Equity { get; set; }
    public double Margin { get; set; }
    public double FreeMargin { get; set; }
    public double MarginLevel { get; set; }
    public int OpenPositions { get; set; }
}

public class TradingViewAlertRequest
{
    public string? Ticker { get; set; }
    public string? Action { get; set; }
    public string? Price { get; set; }
    public string? Time { get; set; }
    public string? Interval { get; set; }
    public string? Exchange { get; set; }
    public string? Volume { get; set; }
    public string? Comment { get; set; }
    public decimal? Lot { get; set; }
    public int? SL { get; set; }
    public int? TP { get; set; }
}