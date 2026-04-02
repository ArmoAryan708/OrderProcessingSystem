FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/OPS.Web/OPS.Web.csproj", "src/OPS.Web/"]
COPY ["src/OPS.Application/OPS.Application.csproj", "src/OPS.Application/"]
COPY ["src/OPS.Domain/OPS.Domain.csproj", "src/OPS.Domain/"]
COPY ["src/OPS.Database/OPS.Database.csproj", "src/OPS.Database/"]
RUN dotnet restore "src/OPS.Web/OPS.Web.csproj"
COPY . .
WORKDIR "/src/src/OPS.Web"
RUN dotnet build "OPS.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "OPS.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OPS.Web.dll"]
