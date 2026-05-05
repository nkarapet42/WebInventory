FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["WebInventory.sln", "./"]
COPY ["src/WebInventory.Web/WebInventory.Web.csproj", "src/WebInventory.Web/"]
COPY ["src/WebInventory.Application/WebInventory.Application.csproj", "src/WebInventory.Application/"]
COPY ["src/WebInventory.Domain/WebInventory.Domain.csproj", "src/WebInventory.Domain/"]
COPY ["src/WebInventory.Infrastructure/WebInventory.Infrastructure.csproj", "src/WebInventory.Infrastructure/"]
RUN dotnet restore "src/WebInventory.Web/WebInventory.Web.csproj"
COPY . .
WORKDIR "/src/src/WebInventory.Web"
RUN dotnet publish "WebInventory.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "WebInventory.Web.dll"]
