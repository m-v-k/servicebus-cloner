# Description

Clones a Azure Service Bus by creating a new one and cloning all queues, topics and subscriptions into it, when ready outputs the connectionstring.
Because Azure doesn't have a emulator, use this for automation such as integration tests.
When ready, run again with `--delete` to cleanup the temporary Azure resource(s).

[Docker Hub](https://hub.docker.com/r/mvk0/servicebus-cloner)

PR's and issues are welcome.

## Requirements

- Docker.
- Azure service principal with _Azure Service Bus Data Owner_ and _Contributor_ on subscription level or replace _Contributor_ with a custom one with resource group write/read/list (Microsoft.Resources/resourceGroups/write).

## Example

1. Create a clone.

    ```bash
    output_string=$(docker run --rm servicebus-cloner:latest \
        --tenant=a2c511af-be9b-4d4f-a265-d2a3fcf3dc98 \
        --subscription=89d5e09c-47ac-4008-8b09-3f6e67c682eb \
        --clientid=5bc23718-68c1-472c-9a82-6c73f48d049d \
        --clientsecret=MySuperSecret \
        --sbFrom=MyRealServiceBusName \
        --sbTo=MyTemporaryServiceBusForTesting)
    ```

2. Run your tests

    ```bash
    dotnet test -e "ServiceBusConnectionString=$output_string"
    ```

3. Clean up resources

    ```bash
    docker run --rm servicebus-cloner:latest \
        --tenant=a2c511af-be9b-4d4f-a265-d2a3fcf3dc98 \
        --subscription=89d5e09c-47ac-4008-8b09-3f6e67c682eb \
        --clientid=5bc23718-68c1-472c-9a82-6c73f48d049d \
        --clientsecret=MySuperSecret \
        --sbTo=MyTemporaryServiceBusNameForTesting \
        --delete
    ```
