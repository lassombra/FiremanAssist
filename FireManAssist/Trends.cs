using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FireManAssist
{
    public class Trends
    {
        public Trend immediateTrend { get; set; }
        public Trend longtermTrend { get; set; }

        public Trends(Trend immediateTrend, Trend longtermTrend)
        {
            this.immediateTrend = immediateTrend;
            this.longtermTrend = longtermTrend;
        }
    }
}
