# CSharp-AOP-Logging-Primer
A Quick Logging Primer using AOP in C#

## Intro

I was looking for a quick C# code sample on how to write an Aspect Orientated Logger using a structured event-driven logger such as Serilog.
But I couldn't find anything, so I thought I'd write a quick primer on how to write one.

I've chosen AutoFac, Serilog & C# for my example but in theory you can interchange them with other tools and apply the same principles.

## Pre-Requisites

Basic understanding of what Aspect Orientated Programming (AOP) is and a basic working knowledge for AutoFac, Serilog & C# is assumed.

If you need to brush up on it, here are some links to get you going: 

[AOP Wikipedia](https://www.wikiwand.com/en/Aspect-oriented_programming)
[What is AOP?](https://stackoverflow.com/questions/242177/what-is-aspect-oriented-programming)
[AutoFac Getting Started](https://autofac.readthedocs.io/en/latest/getting-started/index.html)
[Serilog Tutorial](https://blog.getseq.net/serilog-tutorial/) 


## Problem  

Imagine you had to write an Interest Rate Calculator as part of a project.
Traditonally, it would look something like this, with a combination of business logic & other logic such as parameter validation & error logging.

```c#
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
```
If you look at this code, the actual business logic is arguably just 2 lines of the code and the rest is just plumbing for argument validation, error handling & logging. Madness!

And if you had to write dozens of other similar functions, you would have to repeat (copy&paste) the plumbing code which is not ideal or fun.

This is where AOP can come in and handle the so called cross cutting concerns I've mentioned above. 
You can write code for cross cutting concerns once and reuse them as you see fit.

We are going to use method interception capabilities of AutoFac to do our logging..
We can take advantage of various interception points (before/after) 

### Step 1 : New Project + NuGet Packages

Start a new Console App Project with the .NET Framework of your choice (Standard/Core)

Grab the following NuGet Packages:

![Dependencies](https://github.com/FrancisChung/CSharp-AOP-Logging-Primer/blob/master/AOPLogging/Pics/AOPLoggingDirectory.PNG "Dependencies")

The CLI way:

```
Install-Package Autofac.Extras.DynamicProxy -Version 4.4.0
Install-Package Serilog -Version 2.7.1	
Install-Package AutofacSerilogIntegration
Install-Package Serilog.Sinks.RollingFile -Version 3.3.1-dev-00771
```

### Step 2 : The RateCalculator Class & IRateCalculator Interface

```c#
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
```

### Step 3: The File Logger

In my example, I've gone with a Rolling File appender which is a popular method for persistent logging.
In case you're wondering why I'm using Castle Windsor here, [Autofac leverages Castle's Dynamic Proxy](https://autofaccn.readthedocs.io/en/latest/advanced/interceptors.html) to enables method calls on Autofac components to be intercepted by other components

In the file logger below, I have added code to:
* log the name of the method called & its arguments
* log the execution time
* log any errors

```c#
using Serilog;
using Castle.DynamicProxy;

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

            var watch = System.Diagnostics.Stopwatch.StartNew();
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
            var executionTime = watch.ElapsedMilliseconds;

            logger.Information($"Done: result was {invocation.ReturnValue}");
            logger.Debug($"Execution Time: {executionTime} ms.");
        }
    }
}
```
### Step 4: Wiring up the Dependencies in Main()

If you've used any [Dependency Injection Frameworks](https://weblogs.asp.net/jhallal/list-of-net-dependency-injection-containers-ioc) or ASP.NET, you will be familiar with the concept of wiring it all up using the [Builder pattern](https://www.oodesign.com/builder-pattern.html).
AutoFac is no different, so you'll need to register the classes first before you can use it.

As per the [AutoFac documentation](https://autofaccn.readthedocs.io/en/latest/advanced/interceptors.html), we need to :
* Create Interceptors
* Register Interceptors with Autofac
* Enable Interception on Types
* Associate Interceptors with Types to be Intercepted

So we've already created the Interceptor in the previous step.
We'll do the rest in this step.

Translating the above requirements to our simple example means:

* We will need to register the FileLogger & the RateCalculator class.
* We will also need to configure the RateCalculator class to enable InterfaceInterceptors, so 

In main(), we will register the logger & intialiase the RateCalculator class before running some tests

```c#
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
                    .WriteTo.RollingFile("RateCalculator.log").CreateLogger();
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
```

### Step 5: Results & Analysis

If you run the project, you will see a Console Window with an error message (as expected)

![Console](https://github.com/FrancisChung/CSharp-AOP-Logging-Primer/raw/master/AOPLogging/Pics/AOPLoggingDependencies.PNG "Console")

Let's see what was logged in our log file. Goto the debug folder

![LogDirectory](https://github.com/FrancisChung/CSharp-AOP-Logging-Primer/raw/master/AOPLogging/Pics/AOPLoggingDirectory.PNG "LogDirectory")

Let's take a look at the file.
First of all, you can see it logged the method info + arguments, error log & results correctly for the first 3 tests.

![File1](https://github.com/FrancisChung/CSharp-AOP-Logging-Primer/raw/master/AOPLogging/Pics/AOPLoggingFile1.PNG "LogFile 1")

Next, you can see the warning logged for the zero rate case.

![File2](https://github.com/FrancisChung/CSharp-AOP-Logging-Primer/raw/master/AOPLogging/Pics/AOPLoggingFile2.PNG "LogFile 2")

And finally, you can see the warning & error logged for the zero rate and zero day.

![File3](https://github.com/FrancisChung/CSharp-AOP-Logging-Primer/raw/master/AOPLogging/Pics/AOPLoggingFile3.PNG "LogFile 3")

### Step 6: Closing Words & Further Reading

I hope this was a straight forward introduction to using AOP for Logging and other possibilities to reduce code

Like any technology solution, it won't be all smooth sailing and expect some teething issues if you decide to utilise it.

Some of the criticisms of AOP are :
* Learning Curve (I've only covered 1 aspect of Aspect Orientated Programming!)
* Difficult to Debug / test if not fully understood
* Can easily introduce bugs across the system, if not judiciously deployed

Like any prudent technologist, you should consider all the pros & cons of AOP, expertise at hann and the requirements before making a decision.

Good Luck and thanks for taking your time to read this!
