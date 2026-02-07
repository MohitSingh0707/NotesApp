# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project file and restore
COPY ["AI.Summary.Service.csproj", "./"]
RUN dotnet restore "AI.Summary.Service.csproj"

# Copy the rest of the code
COPY . .

# Build and Publish
RUN dotnet publish "AI.Summary.Service.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "AI.Summary.Service.dll"]
