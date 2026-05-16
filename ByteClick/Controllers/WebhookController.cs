using ByteClick.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ByteClick.Controllers
{
     



    [ApiController]
    [Route("webhook/[controller]")] // URL: http://localhost:5000/webhook/tradingview
    public class WebhookController : ControllerBase
    {
        private readonly TradingDbContext _context;

        public WebhookController(TradingDbContext context)
        {
            _context = context;
        }

        // 1. JS'den veriyi alıp SQL'e kaydeden metod
        [HttpPost("/tradingview")]
        public async Task<IActionResult> PostAlert([FromBody] Alert alert)
        {
            if (alert == null) return BadRequest();

            alert.CreatedAt = DateTime.Now;
            alert.IsProcessed = false;

            _context.Alerts.Add(alert);
            await _context.SaveChangesAsync();

            Console.WriteLine($"[SQL KAYIT] {alert.Action} sinyali alındı.");
            return Ok(new { status = "success", id = alert.Id });
        }

        // 2. MetaTrader'ın "Yeni işlem var mı?" diye soracağı metod
        [HttpGet("latest")]
        public async Task<IActionResult> GetLatestAlert()
        {
            var alert = await _context.Alerts
                .Where(a => !a.IsProcessed)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();

            if (alert == null) return NotFound();

            // Sinyali MetaTrader'a verdikten sonra "işlendi" olarak işaretle 
            // ki aynı işlemi defalarca açmasın.
            alert.IsProcessed = true;
            await _context.SaveChangesAsync();

            return Ok(alert);
        }
    }

    
}
