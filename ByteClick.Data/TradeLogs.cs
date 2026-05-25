

namespace ByteClick.Data
{
    public class TradeLogs
    {
        public int Id { get; set; }
        public long Ticket { get; set; } // MT5'ten gelen benzersiz işlem ID'si
        public string Symbol { get; set; }
        public string Type { get; set; } // BUY veya SELL
        public double Lot { get; set; }
        public double OpenPrice { get; set; }
        public double? ClosePrice { get; set; }
        public double? Profit { get; set; }
        public DateTime OpenTime { get; set; } = DateTime.Now;
        public DateTime? CloseTime { get; set; }
        public bool IsOpen { get; set; } = true;
    }
}
