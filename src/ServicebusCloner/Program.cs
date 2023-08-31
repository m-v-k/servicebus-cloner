using System;
using System.IO;

using CommandLine;

namespace ServiceBusCloner.Service;

public class Program
{
    static CancellationTokenSource cts = new CancellationTokenSource();

    static void Main(string[] args)
    {
        var isEphemeral = args.Contains("--ephemeral");
        if (isEphemeral)
        {
            Console.CancelKeyPress += (sender, e) => {
                e.Cancel = true;  // Prevent the immediate exit.
                cts.Cancel();
            };
            
            // Handle SIGTERM for graceful shutdown in containers
            AppDomain.CurrentDomain.ProcessExit += (sender, e) => {
                Console.WriteLine("Starting delete.");
                Parser.Default.ParseArguments<Options>(args)
                    .WithParsed<Options>(options => {
                        using (var client = new AzureClient(options))
                        {
                            var result = client.DeleteNamespaceAsync().GetAwaiter().GetResult();
                            Console.WriteLine(result);
                        }
                });
                Console.WriteLine("Finished delete.");
                cts.Cancel();
            };
        }

        Parser.Default.ParseArguments<Options>(args)
            .WithParsed<Options>(options => {
                if (options.Ephemeral)
                {
                    Console.WriteLine("Starting clone.");
                }
                using (var client = new AzureClient(options))
                {
                    if (options.Create | options.Ephemeral)
                    {
                        var connectionstring = client.CreateNamespaceAsync().GetAwaiter().GetResult();
                        client.CloneNamespaceAsync().GetAwaiter().GetResult();
                        Console.WriteLine(connectionstring);
                        
                        SaveStringToFile(connectionstring, options.OutFile);
                        Directory.CreateDirectory("/health");
                        File.Create("/health/ready");

                        // TODO: save to file
                    }
                    else if (options.Delete)
                    {
                        var result = client.DeleteNamespaceAsync().GetAwaiter().GetResult();
                        Console.WriteLine(result);
                    }
                }
                if (options.Ephemeral)
                {
                    Console.WriteLine("Finished clone.");
                }
            })
            .WithNotParsed(errors => 
            {
                foreach(var error in errors)
                {
                    Console.WriteLine(error);
                }
                // TODO: Handle parsing errors if needed.
            });

        if (isEphemeral)
        {
            while (!cts.Token.IsCancellationRequested)
            {
                // Keeps the console running
                Thread.Sleep(100);
            }
        }
    }

    private static void SaveStringToFile(string content, string filePath)
    {
        // Ensure the directory exists
        string directoryPath = Path.GetDirectoryName(filePath);
        if (directoryPath != null && !Directory.Exists(directoryPath))
        {
            Console.WriteLine($"Creating path '{directoryPath}'");
            Directory.CreateDirectory(directoryPath);
            Console.WriteLine($"Created path '{directoryPath}'");
        }

        // Write the content to the file (This will create the file if it doesn't exist or overwrite if it does)
        Console.WriteLine($"Writing to file '{filePath}'");
        File.WriteAllText(filePath, content);
        Console.WriteLine($"Written to file '{filePath}'");
    }
}