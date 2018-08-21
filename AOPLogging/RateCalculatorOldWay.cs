using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace AOPLogging
{
    public class RateCalculatorOldWay : IRateCalculator
    {
        public string Calculate(decimal notional, decimal rate, decimal spread, int days)
        {
            try
            {
                //Log Method Call + Arguments 
                Log.Logger.Information($"Called : Calculate, Arguments : Notional:${notional}, rate:${rate}, spread:${spread}, days:${days}");

                //Argument Validation
                if (rate == 0)
                    Log.Logger.Warning("Rate is Zero");

                //Argument Validation
                if (days == 0)
                    throw new ArgumentException("Days can't be Zero");

                var interest = (notional * (rate + spread) / 100 * days / 360);

                return interest == 0 ? "Zero" : interest.ToString("#.##");
            }
            catch (Exception ex)
            {   
                //Error Logging
                Log.Logger.Error(ex, $"Error occured in Calculate: {ex.Message}");
                return string.Empty;
            }
        }
    }
}
