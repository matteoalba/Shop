FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Progetti dell'OrderService
COPY ["OrderService/ShopSaga.OrderService.WebApi/ShopSaga.OrderService.WebApi.csproj", "OrderService/ShopSaga.OrderService.WebApi/"]
COPY ["OrderService/ShopSaga.OrderService.Business/ShopSaga.OrderService.Business.csproj", "OrderService/ShopSaga.OrderService.Business/"]
COPY ["OrderService/ShopSaga.OrderService.ClientHttp/ShopSaga.OrderService.ClientHttp.csproj", "OrderService/ShopSaga.OrderService.ClientHttp/"]
COPY ["OrderService/ShopSaga.OrderService.Repository/ShopSaga.OrderService.Repository.csproj", "OrderService/ShopSaga.OrderService.Repository/"]
COPY ["OrderService/ShopSaga.OrderService.Shared/ShopSaga.OrderService.Shared.csproj", "OrderService/ShopSaga.OrderService.Shared/"]

# Dipendenze da altri servizi
COPY ["PaymentService/ShopSaga.PaymentService.Shared/ShopSaga.PaymentService.Shared.csproj", "PaymentService/ShopSaga.PaymentService.Shared/"]
COPY ["PaymentService/ShopSaga.PaymentService.ClientHttp/ShopSaga.PaymentService.ClientHttp.csproj", "PaymentService/ShopSaga.PaymentService.ClientHttp/"]
COPY ["StockService/ShopSaga.StockService.Shared/ShopSaga.StockService.Shared.csproj", "StockService/ShopSaga.StockService.Shared/"]
COPY ["StockService/ShopSaga.StockService.ClientHttp/ShopSaga.StockService.ClientHttp.csproj", "StockService/ShopSaga.StockService.ClientHttp/"]

# Restore
RUN dotnet restore "OrderService/ShopSaga.OrderService.WebApi/ShopSaga.OrderService.WebApi.csproj"

# Copia tutto il codice sorgente
COPY . .

WORKDIR "/src/OrderService/ShopSaga.OrderService.WebApi"
RUN dotnet build "ShopSaga.OrderService.WebApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ShopSaga.OrderService.WebApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ShopSaga.OrderService.WebApi.dll"]
