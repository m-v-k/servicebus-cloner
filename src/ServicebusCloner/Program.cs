using CommandLine;
using Microsoft.Extensions.Logging;

namespace ServiceBusCloner.Service;

public class Program
{
    static CancellationTokenSource cts = new CancellationTokenSource();

    static void Main(string[] args)
    {
        ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole().SetMinimumLevel(Utilities.GetLogLevel(args));
        });
        ILogger logger = loggerFactory.CreateLogger<Program>();
        logger.LogDebug("Logger Factory initialized.");

        args = Utilities.AppendEnvironmentVariables(args, logger);
        var result = Parser.Default.ParseArguments<Options>(args);
        if (result.Errors.Count() > 0)
        {
            return;
        }
        var options = result.Value;

        var isCreate = options.Action == Action.create;
        var isEphemeral = options.Action == Action.ephemeral;

        if (isEphemeral)
        {
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;  // Prevent the immediate exit.
                cts.Cancel();
            };

            // Handle SIGTERM for graceful shutdown in containers
            AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
            {
                logger.LogDebug("Start delete.");
                Parser.Default.ParseArguments<Options>(args)
                    .WithParsed<Options>(options =>
                    {
                        using (var client = new AzureClient(options, loggerFactory))
                        {
                            var result = client.DeleteNamespaceAsync().GetAwaiter().GetResult();
                            Console.WriteLine(result);
                        }
                    });
                logger.LogDebug("Finish delete.");
                cts.Cancel();
            };
        }

        logger.LogDebug("Start Cloning.");
        using (var client = new AzureClient(options, loggerFactory))
        {
            if (isCreate | isEphemeral)
            {
                var connectionstring = client.CreateNamespaceAsync().GetAwaiter().GetResult();
                client.CloneNamespaceAsync().GetAwaiter().GetResult();

                Utilities.SaveStringToFile(connectionstring, options.OutFile, logger);
                Utilities.SaveStringToFile(string.Empty, "/health/ready", logger); // if in a container, signals that the container is healthy (ready with startup)
            }
            else
            {
                var deleteResult = client.DeleteNamespaceAsync().GetAwaiter().GetResult();
                Console.WriteLine(deleteResult);
            }
        }
        logger.LogDebug("Finish Cloning.");

        if (isEphemeral)
        {
            while (!cts.Token.IsCancellationRequested)
            {
                // Keeps the console running
                Thread.Sleep(100);
            }
        }
        loggerFactory.Dispose();
    }

}