FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["ShopSaga.StockService.WebApi/ShopSaga.StockService.WebApi.csproj", "ShopSaga.StockService.WebApi/"]
COPY ["ShopSaga.StockService.Business/ShopSaga.StockService.Business.csproj", "ShopSaga.StockService.Business/"]
COPY ["ShopSaga.StockService.ClientHttp/ShopSaga.StockService.ClientHttp.csproj", "ShopSaga.StockService.ClientHttp/"]
COPY ["ShopSaga.StockService.Repository/ShopSaga.StockService.Repository.csproj", "ShopSaga.StockService.Repository/"]
COPY ["ShopSaga.StockService.Shared/ShopSaga.StockService.Shared.csproj", "ShopSaga.StockService.Shared/"]
RUN dotnet restore "ShopSaga.StockService.WebApi/ShopSaga.StockService.WebApi.csproj"
COPY . .
WORKDIR "/src/ShopSaga.StockService.WebApi"
RUN dotnet build "ShopSaga.StockService.WebApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ShopSaga.StockService.WebApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ShopSaga.StockService.WebApi.dll"]
