FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/API/ProductsApi.Api.csproj", "src/API/"]
COPY ["src/Application/ProductsApi.Application.csproj", "src/Application/"]
COPY ["src/Infrastructure/ProductsApi.Infrastructure.csproj", "src/Infrastructure/"]
COPY ["src/Domain/ProductsApi.Domain.csproj", "src/Domain/"]
RUN dotnet restore "src/API/ProductsApi.Api.csproj"

COPY . .
WORKDIR /src/src/API
RUN dotnet publish "ProductsApi.Api.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "ProductsApi.Api.dll"]
