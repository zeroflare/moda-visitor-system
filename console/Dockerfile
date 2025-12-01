# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["console.csproj", "./"]
RUN dotnet restore "console.csproj"

# Copy everything else and build
COPY . .
RUN dotnet build "console.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "console.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Copy published app
COPY --from=publish /app/publish .

# Set entry point (使用 CMD 以便可以覆蓋參數)
CMD ["dotnet", "console.dll"]

