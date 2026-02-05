# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files for restore optimization
COPY . .

# Restore dependencies
RUN dotnet restore "NotesApp.sln"

# Build the main API project
RUN dotnet build "NotesApp.API/NotesApp.API.csproj" -c Release -o /app/build

# Publish Stage
FROM build AS publish
RUN dotnet publish "NotesApp.API/NotesApp.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Environment Configuration
EXPOSE 5278
ENV ASPNETCORE_URLS=http://+:5278
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "NotesApp.API.dll"]
