FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/OPS.BackgroundServices/OPS.BackgroundServices.csproj", "src/OPS.BackgroundServices/"]
COPY ["src/OPS.Application/OPS.Application.csproj", "src/OPS.Application/"]
COPY ["src/OPS.Domain/OPS.Domain.csproj", "src/OPS.Domain/"]
COPY ["src/OPS.Database/OPS.Database.csproj", "src/OPS.Database/"]
RUN dotnet restore "src/OPS.BackgroundServices/OPS.BackgroundServices.csproj"
COPY . .
WORKDIR "/src/src/OPS.BackgroundServices"
RUN dotnet build "OPS.BackgroundServices.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "OPS.BackgroundServices.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OPS.BackgroundServices.dll"]
