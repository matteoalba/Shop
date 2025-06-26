FROM mcr.microsoft.com/dotnet/aspnet:9.0-preview AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0-preview AS build
WORKDIR /src
COPY ["ShopSaga.PaymentService.WebApi/ShopSaga.PaymentService.WebApi.csproj", "ShopSaga.PaymentService.WebApi/"]
COPY ["ShopSaga.PaymentService.Business/ShopSaga.PaymentService.Business.csproj", "ShopSaga.PaymentService.Business/"]
COPY ["ShopSaga.PaymentService.ClientHttp/ShopSaga.PaymentService.ClientHttp.csproj", "ShopSaga.PaymentService.ClientHttp/"]
COPY ["ShopSaga.PaymentService.Repository/ShopSaga.PaymentService.Repository.csproj", "ShopSaga.PaymentService.Repository/"]
COPY ["ShopSaga.PaymentService.Shared/ShopSaga.PaymentService.Shared.csproj", "ShopSaga.PaymentService.Shared/"]
RUN dotnet restore "ShopSaga.PaymentService.WebApi/ShopSaga.PaymentService.WebApi.csproj"
COPY . .
WORKDIR "/src/ShopSaga.PaymentService.WebApi"
RUN dotnet build "ShopSaga.PaymentService.WebApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ShopSaga.PaymentService.WebApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ShopSaga.PaymentService.WebApi.dll"]
