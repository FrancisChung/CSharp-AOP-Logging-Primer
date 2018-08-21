using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace AOPLogging
{
    public interface IRateCalculator
    {
        string Calculate(decimal notional, decimal rate, decimal spread, int days);
    }

    public class RateCalculator : IRateCalculator
    {
        public string Calculate(decimal notional, decimal rate, decimal spread, int days)
        {
            // You could move generic argument checking code into another interceptor. 
            // This is left as an exercise to the reader.
            if (rate == 0)
                Log.Logger.Warning("Rate is Zero");

            if (days == 0)
                throw new ArgumentException("Days can't be Zero");

            var interest = (notional * (rate + spread) /100 * days/360 );

            return interest == 0 ? "Zero" : interest.ToString("#.##");
        }
    }
}
