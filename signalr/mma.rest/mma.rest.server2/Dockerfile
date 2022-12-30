#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

#FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["mma.rest.server2/mma.rest.server.csproj", "mma.rest.server2/"]
COPY ["mma.types/mma.types.csproj", "mma.types/"]
RUN dotnet restore "mma.rest.server2/mma.rest.server.csproj"
COPY . .
WORKDIR "/src/mma.rest.server2"
RUN dotnet build "mma.rest.server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "mma.rest.server.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "mma.rest.server.dll"]