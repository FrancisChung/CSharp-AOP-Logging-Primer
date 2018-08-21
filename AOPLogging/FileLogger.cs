using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;
using Castle.DynamicProxy;
using System.Diagnostics;

namespace AOPLogging
{
    public class FileLogger : IInterceptor
    {
        ILogger logger;

        public FileLogger()
        {
            this.logger = Log.Logger;
        }

        public void Intercept(IInvocation invocation)
        {
            var name = $"{invocation.Method.DeclaringType}.{invocation.Method.Name}";
            var args = string.Join(",", invocation.Arguments.Select(a => (a ?? "").ToString()));

            logger.Information($"Call: {name}");
            logger.Information($"Args: {args}");

            var watch = Stopwatch.StartNew();
            try
            {
                invocation.Proceed();
            }

            catch (Exception ex)
            {
                logger.Error(ex.Message);
                throw;
            }
            watch.Stop();

            var ticks = (double) watch.ElapsedTicks;    // Can't use ElapsedMilliseconds as it is a long and our tests execute too fast for it not to be zero
            double milliseconds = (ticks / Stopwatch.Frequency) * 1000;


            logger.Information($"Done: result was {invocation.ReturnValue}");
            logger.Debug($"Execution Time: {milliseconds} ms");
        }
    }
}
