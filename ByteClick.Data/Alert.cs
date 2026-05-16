
namespace ByteClick.Data
{
    public class Alert
    {
        public int Id { get; set; }
        public string Symbol { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty; // BUY or SELL
        public decimal Price { get; set; }
        public decimal Lot { get; set; }
        public int SL { get; set; }
        public int TP { get; set; }
        public DateTime Timestamp { get; set; }
        public string Raw { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsProcessed { get; set; } = false; // MT5'e gönderildi mi?
    }
}
