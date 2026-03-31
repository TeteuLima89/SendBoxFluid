FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY SendBoxFluid/SendBoxFluid.csproj SendBoxFluid/
RUN dotnet restore SendBoxFluid/SendBoxFluid.csproj
COPY SendBoxFluid/ SendBoxFluid/
RUN dotnet publish SendBoxFluid/SendBoxFluid.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "SendBoxFluid.dll"]
