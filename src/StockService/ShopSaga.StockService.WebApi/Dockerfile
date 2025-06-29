FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Progetti di StockService
COPY ["StockService/ShopSaga.StockService.WebApi/ShopSaga.StockService.WebApi.csproj", "StockService/ShopSaga.StockService.WebApi/"]
COPY ["StockService/ShopSaga.StockService.Business/ShopSaga.StockService.Business.csproj", "StockService/ShopSaga.StockService.Business/"]
COPY ["StockService/ShopSaga.StockService.ClientHttp/ShopSaga.StockService.ClientHttp.csproj", "StockService/ShopSaga.StockService.ClientHttp/"]
COPY ["StockService/ShopSaga.StockService.Repository/ShopSaga.StockService.Repository.csproj", "StockService/ShopSaga.StockService.Repository/"]
COPY ["StockService/ShopSaga.StockService.Shared/ShopSaga.StockService.Shared.csproj", "StockService/ShopSaga.StockService.Shared/"]

# Dipendenze da altri servizi (Order e Payment)
COPY ["OrderService/ShopSaga.OrderService.Shared/ShopSaga.OrderService.Shared.csproj", "OrderService/ShopSaga.OrderService.Shared/"]
COPY ["OrderService/ShopSaga.OrderService.ClientHttp/ShopSaga.OrderService.ClientHttp.csproj", "OrderService/ShopSaga.OrderService.ClientHttp/"]
COPY ["PaymentService/ShopSaga.PaymentService.Shared/ShopSaga.PaymentService.Shared.csproj", "PaymentService/ShopSaga.PaymentService.Shared/"]
COPY ["PaymentService/ShopSaga.PaymentService.ClientHttp/ShopSaga.PaymentService.ClientHttp.csproj", "PaymentService/ShopSaga.PaymentService.ClientHttp/"]

# Restore
RUN dotnet restore "StockService/ShopSaga.StockService.WebApi/ShopSaga.StockService.WebApi.csproj"

# Copia sorgente
COPY . .

# Build
WORKDIR "/src/StockService/ShopSaga.StockService.WebApi"
RUN dotnet build "ShopSaga.StockService.WebApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ShopSaga.StockService.WebApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ShopSaga.StockService.WebApi.dll"]
