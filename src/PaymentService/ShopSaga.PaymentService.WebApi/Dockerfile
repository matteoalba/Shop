FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia i file di progetto del PaymentService
COPY ["PaymentService/ShopSaga.PaymentService.WebApi/ShopSaga.PaymentService.WebApi.csproj", "PaymentService/ShopSaga.PaymentService.WebApi/"]
COPY ["PaymentService/ShopSaga.PaymentService.Business/ShopSaga.PaymentService.Business.csproj", "PaymentService/ShopSaga.PaymentService.Business/"]
COPY ["PaymentService/ShopSaga.PaymentService.ClientHttp/ShopSaga.PaymentService.ClientHttp.csproj", "PaymentService/ShopSaga.PaymentService.ClientHttp/"]
COPY ["PaymentService/ShopSaga.PaymentService.Repository/ShopSaga.PaymentService.Repository.csproj", "PaymentService/ShopSaga.PaymentService.Repository/"]
COPY ["PaymentService/ShopSaga.PaymentService.Shared/ShopSaga.PaymentService.Shared.csproj", "PaymentService/ShopSaga.PaymentService.Shared/"]

# Copia i file di progetto dell'OrderService (dependencies)
COPY ["OrderService/ShopSaga.OrderService.ClientHttp/ShopSaga.OrderService.ClientHttp.csproj", "OrderService/ShopSaga.OrderService.ClientHttp/"]
COPY ["OrderService/ShopSaga.OrderService.Shared/ShopSaga.OrderService.Shared.csproj", "OrderService/ShopSaga.OrderService.Shared/"]
COPY ["StockService/ShopSaga.StockService.ClientHttp/ShopSaga.StockService.ClientHttp.csproj", "StockService/ShopSaga.StockService.ClientHttp/"]
COPY ["StockService/ShopSaga.StockService.Shared/ShopSaga.StockService.Shared.csproj", "StockService/ShopSaga.StockService.Shared/"]

RUN dotnet restore "PaymentService/ShopSaga.PaymentService.WebApi/ShopSaga.PaymentService.WebApi.csproj"

COPY . .

# Build 
WORKDIR "/src/PaymentService/ShopSaga.PaymentService.WebApi"
RUN dotnet build "ShopSaga.PaymentService.WebApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ShopSaga.PaymentService.WebApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ShopSaga.PaymentService.WebApi.dll"]
