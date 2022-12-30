#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

ARG serverip
ARG serverport

ENV SERVERIP=$serverip
ENV SERVERPORT=$serverport

RUN echo "SERVERIP: $SERVERIP"
RUN echo "SERVERPORT: $SERVERPORT"

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["mma.client/mma.client.csproj", "mma.client/"]
COPY ["mma.model/mma.model.csproj", "mma.model/"]
COPY ["mma.common/mma.common.csproj", "mma.common/"]
RUN dotnet restore "mma.client/mma.client.csproj"
COPY . .
WORKDIR "/src/mma.client"
RUN dotnet build "mma.client.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "mma.client.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "mma.client.dll"]
