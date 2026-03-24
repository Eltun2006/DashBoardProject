# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["DashBoardProject.sln", "./"]
COPY ["DashBoardProject/DashBoardProject.csproj", "DashBoardProject/"]

# Restore dependencies
RUN dotnet restore

# Copy all source code and build
COPY . .
WORKDIR "/src/DashBoardProject"
RUN dotnet build "DashBoardProject.csproj" -c Release -o /app/build

# Publish Stage
FROM build AS publish
RUN dotnet publish "DashBoardProject.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final Runtime Image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Environment variables for Render
# Render typically listens on port 10000, but we can set it explicitly
ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "DashBoardProject.dll"]
