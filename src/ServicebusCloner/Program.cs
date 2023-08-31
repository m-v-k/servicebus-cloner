using System.Reflection;
using CommandLine;

namespace ServiceBusCloner.Service;

public class Program
{
    static CancellationTokenSource cts = new CancellationTokenSource();

    static void Main(string[] args)
    {
        args = AppendEnvironmentVariables(args);

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

        var result = Parser.Default.ParseArguments<Options>(args)
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

    private static string[] AppendEnvironmentVariables(string[] args)
    {
        var options = GetOptions();

        var newArgs = new List<string>(args);
        foreach (var unusedOption in FilterOptions(args, options))
        {
            // repalced this 'Assembly.GetExecutingAssembly().GetName().Name.ToUpperInvariant()' with a static prefix of 'SC_'
            // ignore verbs since this solution has only 1
            var value = Environment.GetEnvironmentVariable($"SC_{unusedOption.LongName.ToUpperInvariant()}");
            
            // try reading a docker secret, ignore errors
            if (string.IsNullOrWhiteSpace(value))
            {
                Console.WriteLine($"Trying to read docker secret for {unusedOption.LongName} at /run/secrets/SC_{unusedOption.LongName.ToUpperInvariant()}");
                try
                {
                    value = File.ReadAllText($"/run/secrets/SC_{unusedOption.LongName.ToUpperInvariant()}");
                }
                catch{}
            }

            if (value != null)
            {
                newArgs.Add($"--{unusedOption.LongName}={value}");
            }
        }
        return newArgs.ToArray();
    }

    private static OptionAttribute[] GetOptions()
    {
        return typeof(Options).GetProperties()
            .Select(property => property.GetCustomAttributes<OptionAttribute>().FirstOrDefault())
            .Where(option => option != null).ToArray();
    }

    private static OptionAttribute[] FilterOptions(string[] args, OptionAttribute[] options)
    {
        var usedLongNames = new HashSet<string>();
        var usedShortNames = new HashSet<string>();

        foreach (var arg in args)
        {
            if (arg.StartsWith("--"))
            {
                var longName = arg.Substring(2);
                if (longName.Contains('='))
                {
                    longName = longName.Substring(0, longName.IndexOf('='));
                }
                usedLongNames.Add(longName);
            }
            else if (arg.StartsWith("-"))
            {
                var shortName = arg.Substring(1);
                if (shortName.Contains('='))
                {
                    shortName = shortName.Substring(0, shortName.IndexOf('='));
                }
                usedShortNames.Add(shortName);
            }
        }

        return options.Where(option => !usedLongNames.Contains(option.LongName) && !usedShortNames.Contains(option.ShortName)).ToArray();
    }
}