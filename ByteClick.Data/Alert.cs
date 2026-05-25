public class Alert
{
    public int Id { get; set; }
    public string Symbol { get; set; }
    public string Action { get; set; }
    public decimal Price { get; set; }
    public decimal Lot { get; set; }
    public int SL { get; set; }
    public int TP { get; set; }
    public string Interval { get; set; } // Yeni
    public string Exchange { get; set; } // Yeni
    public string Volume { get; set; }   // Yeni
    public string Raw { get; set; }
    public bool IsProcessed { get; set; }

    // Zaman Takibi İçin Kritik Alanlar
    public DateTime TVTimestamp { get; set; } // TradingView'in gönderdiği an
    public DateTime CreatedAt { get; set; }   // Senin DB'ye kaydettiğin an
    public double DelayMs { get; set; }       // Aradaki milisaniye farkı

    public double ProcessDelayMs { get; set; } // işleme girdiği farkı
}