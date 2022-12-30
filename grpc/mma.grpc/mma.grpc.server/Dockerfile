#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 6571
EXPOSE 6570

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["mma.grpc.server/mma.grpc.server.csproj", "mma.grpc.server/"]
RUN dotnet restore "mma.grpc.server/mma.grpc.server.csproj"
COPY . .
WORKDIR "/src/mma.grpc.server"
RUN dotnet build "mma.grpc.server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "mma.grpc.server.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "mma.grpc.server.dll"]