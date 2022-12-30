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
COPY ["mma.rest.client/mma.rest.client.csproj", "mma.rest.client/"]
COPY ["mma.types/mma.types.csproj", "mma.types/"]
RUN dotnet restore "mma.rest.client/mma.rest.client.csproj"
COPY . .
WORKDIR "/src/mma.rest.client"
RUN dotnet build "mma.rest.client.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "mma.rest.client.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "mma.rest.client.dll"]