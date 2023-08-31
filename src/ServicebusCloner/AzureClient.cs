
using Azure;
using Azure.Identity;
using Azure.Messaging.ServiceBus.Administration;
using Azure.ResourceManager;
using Azure.ResourceManager.ServiceBus;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.ServiceBus.Models;
using Microsoft.IdentityModel.Abstractions;
using Microsoft.Extensions.Logging;

public class AzureClient : HttpClient
{
    private readonly Options options;
    private readonly ILogger<AzureClient> logger;
    
    public AzureClient(Options options, ILoggerFactory loggerFactory)
    {
        this.options = options;
        this.logger = loggerFactory.CreateLogger<AzureClient>();
    }
    
    public async Task CloneNamespaceAsync()
    {

        var credential = new ClientSecretCredential(options.TenantId, options.ClientId, options.ClientSecret);
        var adminClientOriginal = new ServiceBusAdministrationClient($"{options.ServiceBusFrom}.servicebus.windows.net",  credential);
        var adminClientNew = new ServiceBusAdministrationClient($"{options.ServiceBusTo}.servicebus.windows.net", credential);

        // Clone Queues
        await foreach (var queue in adminClientOriginal.GetQueuesAsync())
        {
            var createQueueOptions = new CreateQueueOptions(queue.Name)
            {
                DefaultMessageTimeToLive = queue.DefaultMessageTimeToLive,
                // TODO: ... set other properties as needed
            };
            await adminClientNew.CreateQueueAsync(createQueueOptions);
        }

        // Clone Topics and Subscriptions
        await foreach (var topic in adminClientOriginal.GetTopicsAsync())
        {

            var createTopicOptions = new CreateTopicOptions(topic.Name)
            {
                DefaultMessageTimeToLive = topic.DefaultMessageTimeToLive,
                AutoDeleteOnIdle = topic.AutoDeleteOnIdle,
                DuplicateDetectionHistoryTimeWindow = topic.DuplicateDetectionHistoryTimeWindow,
                EnableBatchedOperations = topic.EnableBatchedOperations,
                EnablePartitioning = topic.EnablePartitioning,
                MaxSizeInMegabytes = topic.MaxSizeInMegabytes,
                RequiresDuplicateDetection = topic.RequiresDuplicateDetection,
                SupportOrdering = topic.SupportOrdering,
            };
            if (options.Sku == ServiceBusSkuName.Premium.ToString())
            {
                createTopicOptions.MaxMessageSizeInKilobytes = topic.MaxMessageSizeInKilobytes;
            }
            await adminClientNew.CreateTopicAsync(createTopicOptions);

            await foreach (var subscription in adminClientOriginal.GetSubscriptionsAsync(topic.Name))
            {
                var createSubscriptionOptions = new CreateSubscriptionOptions(topic.Name, subscription.SubscriptionName)
                {
                    DefaultMessageTimeToLive = subscription.DefaultMessageTimeToLive,
                    AutoDeleteOnIdle = subscription.AutoDeleteOnIdle,
                    DeadLetteringOnMessageExpiration = subscription.DeadLetteringOnMessageExpiration,
                    EnableBatchedOperations = subscription.EnableBatchedOperations,
                    EnableDeadLetteringOnFilterEvaluationExceptions = subscription.EnableDeadLetteringOnFilterEvaluationExceptions,
                    LockDuration = subscription.LockDuration,
                    MaxDeliveryCount = subscription.MaxDeliveryCount,
                    RequiresSession = subscription.RequiresSession,
                    // TODO: ensure that forward entities already exists
                    ForwardDeadLetteredMessagesTo = subscription.ForwardDeadLetteredMessagesTo,
                    ForwardTo = subscription.ForwardTo,
                };
                var newSubscription = (await adminClientNew.CreateSubscriptionAsync(createSubscriptionOptions)).Value;

                // Clone rules
                List<RuleProperties> rulesList = new List<RuleProperties>();
                await foreach (var rule in adminClientOriginal.GetRulesAsync(topic.Name, subscription.SubscriptionName))
                {
                    rulesList.Add(rule);
                }
                if (rulesList.Count > 0)
                {
                    await adminClientNew.DeleteRuleAsync(topic.Name, subscription.SubscriptionName, RuleProperties.DefaultRuleName);
                }
                foreach (var rule in rulesList)
                {
                    var ruleProperties = new CreateRuleOptions
                    {
                        Name = rule.Name,
                        Filter = rule.Filter,
                        Action = rule.Action
                    };
                    await adminClientNew.CreateRuleAsync(topic.Name, subscription.SubscriptionName, ruleProperties);
                }
            }
        }
    }

    public async Task<string> CreateNamespaceAsync()
    {
        var credential = new ClientSecretCredential(options.TenantId, options.ClientId, options.ClientSecret);
        
        var clientOptions = new ArmClientOptions(){
            // 5 minutes
            Retry = {
                MaxRetries = 30,
                Delay = TimeSpan.FromSeconds(10),
                Mode = Azure.Core.RetryMode.Exponential,
                NetworkTimeout = TimeSpan.FromSeconds(10),
            }
        };
        // client
        ArmClient armClient = new ArmClient(credential, options.SubscriptionId, clientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var location = new Azure.Core.AzureLocation(options.AzureRegion);

        // resource group
        // TODO: don't update on existing group
        ArmOperation<ResourceGroupResource> operation = await subscription.GetResourceGroups().CreateOrUpdateAsync(WaitUntil.Completed, options.ResourceGroup, new ResourceGroupData(location));
        ResourceGroupResource resourceGroup = operation.Value;
        
        // namespace
        // TODO: throw on existing ns
        ServiceBusNamespaceCollection namespaceCollection = resourceGroup.GetServiceBusNamespaces();
        var namespaceData = new ServiceBusNamespaceData(location);
        ServiceBusSkuName skuName = (ServiceBusSkuName) Enum.Parse(typeof(ServiceBusSkuName), options.Sku);
        namespaceData.Sku = new ServiceBusSku(skuName);
        ServiceBusNamespaceResource serviceBusNamespace = (await namespaceCollection.CreateOrUpdateAsync(WaitUntil.Completed, options.ServiceBusTo, namespaceData)).Value;

        // connectionstring
        var authRule = serviceBusNamespace.GetServiceBusNamespaceAuthorizationRule("RootManageSharedAccessKey");
        var keys = await authRule.Value.GetKeysAsync();

        return keys.Value.PrimaryConnectionString;
    }

    public async Task<string> DeleteNamespaceAsync()
    {
        var credential = new ClientSecretCredential(options.TenantId, options.ClientId, options.ClientSecret);
        
        // client
        ArmClient armClient = new ArmClient(credential, options.SubscriptionId);
        var subscription = await armClient.GetDefaultSubscriptionAsync();

        // resource group
        ResourceGroupResource resourceGroup = await subscription.GetResourceGroups().GetAsync(options.ResourceGroup);

        // namespace
        ServiceBusNamespaceResource serviceBusNamespace = await resourceGroup.GetServiceBusNamespaces().GetAsync(options.ServiceBusTo);
        if (serviceBusNamespace.Data.Status == "Activating")
        {
            return "Error: Namespace is still activating. Please try again later";
        }

        await serviceBusNamespace.DeleteAsync(WaitUntil.Started);
        return "deleted";
    }
}
