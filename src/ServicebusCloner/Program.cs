
using CommandLine;

namespace ServiceBusCloner.Service;

public class Program
{
    // TODO: implement cancelation token and gracefull shutdown, static CancellationTokenSource cts = new CancellationTokenSource();

    static void Main(string[] args)
    {
        Parser.Default.ParseArguments<Options>(args)
            .WithParsed<Options>(options => {
                using (var client = new AzureClient(options))
                {
                    if (options.Create && !options.Delete)
                    {
                        var connectionstrting = client.CreateNamespaceAsync().GetAwaiter().GetResult();
                        client.CloneNamespaceAsync().GetAwaiter().GetResult();
                        Console.WriteLine(connectionstrting);
                    }
                    else if (options.Delete && !options.Create)
                    {
                        var result = client.DeleteNamespaceAsync().GetAwaiter().GetResult();
                        Console.WriteLine(result);
                    }
                    else
                    {
                        throw new Exception("The create and delete options are mutually exclusive.");
                    }
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

    }

}