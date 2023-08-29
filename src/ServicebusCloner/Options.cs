using CommandLine;

public class Options
{
    [Option('t', "tenant", Required = true, HelpText = "The tenant id.")]
    public string TenantId { get; set; }

    [Option('s', "subscription", Required = true, HelpText = "The subscription id.")]
    public string SubscriptionId { get; set; }

    [Option("clientid", Required = true, HelpText = "The clientid.")]
    public string ClientId { get; set; }
    
    [Option("clientsecret", Required = true, HelpText = "The clientsecret.")]
    public string ClientSecret { get; set; }

    [Option("sbFrom", Required = true, HelpText = "Service Bus Name from which to clone.")]
    public string ServiceBusFrom { get; set; }
    
    [Option("sbTo", Required = true, HelpText = "Service Bus Name for the temporary created one.")]
    public string ServiceBusTo { get; set; }

    [Option("region", Required = false, HelpText = "The azure region for the new namespace.", Default = "westeurope")]
    public string AzureRegion { get; set; }

    [Option("resourcegroup", Required = false, HelpText = "The resource group where temporary namespace is created.", Default = "servicebuscloner")]
    public string ResourceGroup { get; set; }

    [Option("sku", Required = false, HelpText = "The namespace sku.", Default = "Standard")]
    public string Sku { get; set; }

    [Option("create", Required = false, HelpText = "Create a service bus and clone from an existing one. The create and delete options are mutually exclusive.", Default = true)]
    public bool Create { get; set; }
    
    [Option("delete", Required = false, HelpText = "Delete a service bus. The create and delete options are mutually exclusive.")]
    public bool Delete { get; set; }
    
    // Add other properties as needed...
}