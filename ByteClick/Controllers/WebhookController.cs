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

        public TradingViewWebhookController(TradingDbContext context, ILogger<TradingViewWebhookController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // 1. SİNYAL ALICI (TradingView -> API)
        [HttpPost]
        public async Task<IActionResult> ReceiveAlert([FromBody] TradingViewAlertRequest request)
        {
            // Hız ölçümü için kronometreyi başlat
            var sw = Stopwatch.StartNew();
            var receiveTime = DateTime.Now;

            try
            {
                if (request == null) return BadRequest(new { error = "Veri boş" });

                // 1. Fiyat Parse (Nokta/Virgül derdine son)
                decimal price = 0;
                if (!string.IsNullOrEmpty(request.Price))
                {
                    var priceStr = request.Price.Replace(",", ".");
                    decimal.TryParse(priceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out price);
                }

                // 2. Zaman Takibi (TradingView ne zaman gönderdi?)
                DateTime tvTime = receiveTime;
                if (DateTime.TryParse(request.Time, out var parsedTime))
                {
                    tvTime = parsedTime.ToUniversalTime();
                }

                // 3. Veritabanı Nesnesini Oluştur
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
                    CreatedAt = DateTime.Now,
                    IsProcessed = false,
                    // TV ile Bizim Kayıt Arasındaki Fark (Gecikme)
                    DelayMs = (DateTime.Now - tvTime).TotalMilliseconds
                };

                _context.Alerts.Add(alert);
                await _context.SaveChangesAsync();

                sw.Stop();

                _logger.LogInformation($"🚀 [HIZLI KAYIT] {alert.Symbol} {alert.Action} | Gecikme: {alert.DelayMs:F2}ms | İşlem: {sw.ElapsedMilliseconds}ms");

                return Ok(new { status = "success", delay_ms = alert.DelayMs });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Alert kaydı patladı!");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // 2. MT5 İŞLEM AÇILIŞ KAYDI (MT5 -> API)
        [HttpPost("open")]
        public async Task<IActionResult> LogOpen([FromBody] TradeLogs log)
        {
            try
            {
                if (await _context.TradeLogs.AnyAsync(x => x.Ticket == log.Ticket))
                    return BadRequest("Bu ticket zaten sistemde var.");

                log.OpenTime = DateTime.Now;
                log.IsOpen = true;
                // Dolar bazlı işlem yapıldığı için kur çevrimi gerekmez, gelen veri direkt USD kabul edilir.

                _context.TradeLogs.Add(log);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"🔵 MT5 İŞLEM AÇILDI: Ticket:{log.Ticket} | {log.Symbol} | {log.OpenPrice}$");
                return Ok(new { status = "success" });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // 3. MT5 İŞLEM KAPANIŞ KAYDI (MT5 -> API)
        [HttpPost("close")]
        public async Task<IActionResult> LogClose([FromBody] TradeUpdateDTO update)
        {
            try
            {
                var existingLog = await _context.TradeLogs
                    .FirstOrDefaultAsync(x => x.Ticket == update.Ticket && x.IsOpen);

                if (existingLog == null) return NotFound("Güncellenecek açık işlem bulunamadı.");

                existingLog.ClosePrice = update.ClosePrice;
                existingLog.Profit = update.Profit; // USD bazlı kâr/zarar
                existingLog.CloseTime = DateTime.Now;
                existingLog.IsOpen = false;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"🔴 MT5 İŞLEM KAPANDI: Ticket:{update.Ticket} | Kar:{update.Profit}$");
                return Ok(new { status = "success" });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // 4. ANALİZ SAYFASI İÇİN VERİ (WEB -> API)
        [HttpGet("stats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            var logs = await _context.TradeLogs.ToListAsync();
            var alerts = await _context.Alerts.ToListAsync();

            var stats = new
            {
                TotalProfit = logs.Sum(x => x.Profit), // Toplam USD Kâr
                TotalTrades = logs.Count,
                WinRate = logs.Count > 0 ? (double)logs.Count(x => x.Profit > 0) / logs.Count * 100 : 0,
                AverageLatency = alerts.Any() ? alerts.Average(x => x.DelayMs) : 0,
                LastTrades = logs.OrderByDescending(x => x.OpenTime).Take(10).ToList()
            };

            return Ok(stats);
        }

        // MT5'in emri alması için gereken endpoint (Dokunmadık, stabil)
        [HttpGet("latest")]
        public async Task<IActionResult> GetLatestUnprocessedAlert()
        {
            var alert = await _context.Alerts
                .Where(a => !a.IsProcessed)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();

            if (alert == null) return Ok(new { status = "no_alerts" });

            alert.IsProcessed = true;
            alert.ProcessDelayMs = (DateTime.Now - alert.CreatedAt).TotalMilliseconds;
            await _context.SaveChangesAsync();

            return Ok(new { status = "success", alert = alert });
        }
    }
}
public class TradeUpdateDTO
{
    public long Ticket { get; set; }
    public double ClosePrice { get; set; }
    public double Profit { get; set; }
}
public class TradingViewAlertRequest
{
    public string? Ticker { get; set; }     // {{ticker}}
    public string? Action { get; set; }     // {{strategy.order.action}}
    public string? Price { get; set; }      // {{close}}
    public string? Time { get; set; }       // {{timenow}}
    public string? Interval { get; set; }   // {{interval}}
    public string? Exchange { get; set; }   // {{exchange}}
    public string? Volume { get; set; }     // {{volume}}
    public string? Comment { get; set; }    // {{strategy.order.comment}}
    public decimal? Lot { get; set; }
    public int? SL { get; set; }
    public int? TP { get; set; }
}
