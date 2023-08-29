# Use the official .NET 6 SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /app

# Copy the .csproj and restore any dependencies (via dotnet restore)
COPY *.csproj ./
RUN dotnet restore

# Copy the main app files and build the app
COPY . ./
RUN dotnet publish -c Release -o out

# Use the .NET 6 runtime image to run the app
FROM mcr.microsoft.com/dotnet/runtime:6.0
WORKDIR /app
COPY --from=build-env /app/out .

# Define a default command with arguments
ENTRYPOINT ["dotnet", "ServicebusCloner.dll"]
