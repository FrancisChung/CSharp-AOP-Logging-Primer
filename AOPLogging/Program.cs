using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extras.DynamicProxy;
using Serilog;
using AutofacSerilogIntegration;

namespace AOPLogging
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("Hello AOPLogger!");
                Console.WriteLine();

                var b = new ContainerBuilder();

                // Logger Setup

                Log.Logger = new LoggerConfiguration()
                    .WriteTo.RollingFile("RateCalculator.log")
                    .CreateLogger();


                /** DebugLevel Logging Enabled 
                 
                Log.Logger = new LoggerConfiguration()
                    .WriteTo.RollingFile("RateCalculator.log")
                    .MinimumLevel.Debug()
                    .CreateLogger();
                **/

                b.RegisterLogger();
                b.RegisterType<FileLogger>();

                // Register & Configure Interceptor for RateCalculator
                b.RegisterType<RateCalculator>()
                    .As<IRateCalculator>()
                    .EnableInterfaceInterceptors()
                    .InterceptedBy(typeof(FileLogger));

                //Test Code for Console (for debugging)
                //b.Register(i => new Logger(Console.Out));

                var container = b.Build();

                var calc = container.Resolve<IRateCalculator>();

                //Let's do some calculations!
                calc.Calculate(100000000, 0.312m, 0, 85);   //expecting 73,666.67;
                calc.Calculate(100000000, 0.32078m, 0, 91);   //expecting 81,085.18;
                calc.Calculate(100000000, 0.31115m, 0, 92);   //expecting 79,515.01;
                calc.Calculate(100000000, 0, 0, 92);   //expecting 0 & warning logged;
                calc.Calculate(100000000, 0, 0, 0);   //expecting Error Thrown & logged;
                Console.WriteLine("Test Finished");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine("Test Failed (As expected). Press any key to end the test");
                Console.ReadKey();
            }

        }
    }
}
