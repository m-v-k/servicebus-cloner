using CommandLine;
using Microsoft.Extensions.Logging;

public class Options
{
    [Option('t', "tenant", Required = true, HelpText = "The tenant id. [SC_TENANT]")]
    public string TenantId { get; set; }

    [Option('s', "subscription", Required = true, HelpText = "The subscription id. [SC_SUBSCRIPTION]")]
    public string SubscriptionId { get; set; }

    [Option("clientid", Required = true, HelpText = "The clientid. [SC_CLIENTID]")]
    public string ClientId { get; set; }
    
    [Option("clientsecret", Required = true, HelpText = "The clientsecret. [SC_CLIENTSECRET]")]
    public string ClientSecret { get; set; }

    [Option("from", Required = true, HelpText = "Service Bus Name from which to clone. [SC_FROM]")]
    public string ServiceBusFrom { get; set; }
    
    [Option("to", Required = true, HelpText = "Service Bus Name for the temporary created one. [SC_TO]")]
    public string ServiceBusTo { get; set; }

    [Option("region", Required = false, HelpText = "The azure region for the new namespace. [SC_REGION]", Default = "westeurope")]
    public string AzureRegion { get; set; }

    [Option("resourcegroup", Required = false, HelpText = "The resource group where temporary namespace is created. [SC_RESOURCEGROUP]", Default = "servicebuscloner")]
    public string ResourceGroup { get; set; }

    [Option("sku", Required = false, HelpText = "The namespace sku. [SC_SKU]", Default = "Standard")]
    public string Sku { get; set; }

    [Option('a', "action", Required = false, HelpText = "'create', 'delete' or 'ephemeral'. Ephemeral does a create then keeps the app running and does a delete on SIGTERM. [SC_ACTION]", Default = Action.ephemeral)]
    public Action Action { get; set; }
    
    [Option('o',"out", Required = false, HelpText = "File path where the temporary servicebus connectionstring is saved to. [SC_OUT]", Default = "/app/out/connectionstring")]
    public string OutFile { get; set; }
    
    [Option('l',"loglevel", Required = false, HelpText = "Log level: 'Critical', 'Debug', 'Error', 'Information', 'None', 'Trace', 'Warning'. [SC_LOGLEVEL]", Default = LogLevel.Information)]
    public LogLevel LogLevel { get; set; }
    
    // Add other properties as needed...
}



