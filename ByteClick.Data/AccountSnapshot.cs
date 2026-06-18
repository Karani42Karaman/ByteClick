using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ByteClick.Data
{
    public class AccountSnapshot
    {
        public int Id { get; set; }
        public double Balance { get; set; }
        public double Equity { get; set; }
        public double Margin { get; set; }
        public double FreeMargin { get; set; }
        public double MarginLevel { get; set; } // %
        public int OpenPositions { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
