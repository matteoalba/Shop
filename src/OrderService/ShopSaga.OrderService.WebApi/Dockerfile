FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["ShopSaga.OrderService.WebApi/ShopSaga.OrderService.WebApi.csproj", "ShopSaga.OrderService.WebApi/"]
COPY ["ShopSaga.OrderService.Business/ShopSaga.OrderService.Business.csproj", "ShopSaga.OrderService.Business/"]
COPY ["ShopSaga.OrderService.ClientHttp/ShopSaga.OrderService.ClientHttp.csproj", "ShopSaga.OrderService.ClientHttp/"]
COPY ["ShopSaga.OrderService.Repository/ShopSaga.OrderService.Repository.csproj", "ShopSaga.OrderService.Repository/"]
COPY ["ShopSaga.OrderService.Shared/ShopSaga.OrderService.Shared.csproj", "ShopSaga.OrderService.Shared/"]
RUN dotnet restore "ShopSaga.OrderService.WebApi/ShopSaga.OrderService.WebApi.csproj"
COPY . .
WORKDIR "/src/ShopSaga.OrderService.WebApi"
RUN dotnet build "ShopSaga.OrderService.WebApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ShopSaga.OrderService.WebApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ShopSaga.OrderService.WebApi.dll"]
