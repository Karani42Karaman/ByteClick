using ByteClick.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Globalization;

namespace ByteClick.Controllers
{
    [ApiController]
    [Route("webhook/tradingview")] // Doğru route yapısı
    public class TradingViewWebhookController : ControllerBase
    {
        private readonly TradingDbContext _context;
        private readonly ILogger<TradingViewWebhookController> _logger;

        public TradingViewWebhookController(TradingDbContext context, ILogger<TradingViewWebhookController> logger)
        {
            _context = context;
            _logger = logger;
        }

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
                _logger.LogError(ex, "Kayıt sırasında hata!");
                return StatusCode(500, new { error = ex.Message });
            }
        }
        /// <summary>
        /// MetaTrader'ın işlenmemiş en yeni alert'i alması için
        /// GET http://localhost:7021/webhook/tradingview/latest
        /// </summary>
        [HttpGet("latest")]
        public async Task<IActionResult> GetLatestUnprocessedAlert()
        {
            try
            {
                var alert = await _context.Alerts
                    .Where(a => !a.IsProcessed)
                    .OrderByDescending(a => a.CreatedAt)
                    .FirstOrDefaultAsync();

                if (alert == null)
                {
                    return Ok(new { status = "no_alerts", message = "Bekleyen sinyal yok" });
                }

                // İşlendi olarak işaretle
                alert.IsProcessed = true;
                alert.ProcessDelayMs = (DateTime.Now - alert.CreatedAt).TotalMilliseconds;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"📤 Alert MT5'e gönderildi: {alert.Symbol} {alert.Action}");

                return Ok(new
                {
                    status = "success",
                    alert = alert
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Alert alınırken hata oluştu");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Tüm alert'leri listeler (test amaçlı)
        /// GET http://localhost:7021/webhook/tradingview/all
        /// </summary>
        [HttpGet("all")]
        public async Task<IActionResult> GetAllAlerts([FromQuery] int take = 20)
        {
            var alerts = await _context.Alerts
                .OrderByDescending(a => a.CreatedAt)
                .Take(take)
                .ToListAsync();

            return Ok(new
            {
                total = alerts.Count,
                alerts = alerts
            });
        }

        /// <summary>
        /// Tüm alert'leri sil (test amaçlı)
        /// DELETE http://localhost:7021/webhook/tradingview/clear
        /// </summary>
        [HttpDelete("clear")]
        public async Task<IActionResult> ClearAllAlerts()
        {
            var count = await _context.Alerts.CountAsync();
            _context.Alerts.RemoveRange(_context.Alerts);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"{count} alert silindi" });
        }

        /// <summary>
        /// Sistem durumu kontrolü
        /// GET http://localhost:7021/webhook/tradingview/health
        /// </summary>
        [HttpGet("health")]
        public IActionResult HealthCheck()
        {
            return Ok(new
            {
                status = "healthy",
                timestamp = DateTime.Now,
                database = _context.Database.CanConnect() ? "connected" : "disconnected"
            });
        }

        [HttpPost("open")]
        public async Task<IActionResult> LogOpen([FromBody] TradeLogs log)
        {
            try
            {
                // Aynı ticket daha önce kaydedilmiş mi kontrol et
                if (await _context.TradeLogs.AnyAsync(x => x.Ticket == log.Ticket))
                    return BadRequest("Bu işlem zaten kayıtlı.");

                log.OpenTime = DateTime.Now;
                log.IsOpen = true;

                _context.TradeLogs.Add(log);
                await _context.SaveChangesAsync();

                return Ok(new { message = "İşlem açılışı kaydedildi." });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // POST: api/trade/close -> MT5 işlem kapandığında çağırır
        [HttpPost("close")]
        public async Task<IActionResult> LogClose([FromBody] TradeUpdateDTO update)
        {
            try
            {
                var existingLog = await _context.TradeLogs
                    .FirstOrDefaultAsync(x => x.Ticket == update.Ticket && x.IsOpen);

                if (existingLog == null)
                    return NotFound("Güncellenecek açık işlem bulunamadı.");

                existingLog.ClosePrice = update.ClosePrice;
                existingLog.Profit = update.Profit;
                existingLog.CloseTime = DateTime.Now;
                existingLog.IsOpen = false;

                await _context.SaveChangesAsync();
                return Ok(new { message = "İşlem kapanışı ve kâr/zarar kaydedildi." });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // Dashboard için tüm işlemleri getir
        [HttpGet("history")]
        public ActionResult<List<TradeLogs>> GetHistory()
        {
            return   _context.TradeLogs.OrderByDescending(x => x.OpenTime).ToList();
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
}