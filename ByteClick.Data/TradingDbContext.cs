using Microsoft.EntityFrameworkCore;


namespace ByteClick.Data
{
    public class TradingDbContext : DbContext
    {
        public TradingDbContext(DbContextOptions<TradingDbContext> options) : base(options) { }
        public DbSet<Alert> Alerts { get; set; }
        public DbSet<TradeLogs> TradeLogs { get; set; }
        public DbSet<AccountSnapshot> AccountSnapshots { get; set; }
        public DbSet<ApplicationSettings> ApplicationSettings { get; set; }
    }
}
