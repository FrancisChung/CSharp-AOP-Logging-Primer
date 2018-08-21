using Castle.DynamicProxy;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AOPLogging
{
    public class ConsoleLogger : IInterceptor
    {
        TextWriter writer;

        public ConsoleLogger(TextWriter writer)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            this.writer = writer;
        }

        public void Intercept(IInvocation invocation)
        {
            var name = $"{invocation.Method.DeclaringType}.{invocation.Method.Name}";
            var args = string.Join(",", invocation.Arguments.Select(a => (a ?? "").ToString()));

            writer.WriteLine($"Call: {name}");
            writer.WriteLine($"Args: {args}");

            var watch = System.Diagnostics.Stopwatch.StartNew();
            invocation.Proceed();
            watch.Stop();
            var executionTime = watch.ElapsedMilliseconds;

            writer.WriteLine($"Done: result was {invocation.ReturnValue}");
            writer.WriteLine($"Execution Time: {executionTime} ms.");
            writer.WriteLine();
        }
    }
}
