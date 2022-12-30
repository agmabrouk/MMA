#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["mma.server/mma.server.csproj", "mma.server/"]
COPY ["mma.common/mma.common.csproj", "mma.common/"]
COPY ["mma.model/mma.model.csproj", "mma.model/"]
RUN dotnet restore "mma.server/mma.server.csproj"
COPY . .
WORKDIR "/src/mma.server"
RUN dotnet build "mma.server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "mma.server.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "mma.server.dll"]
