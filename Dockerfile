# Use the official image as a parent image
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 42069/udp

# Clone vcpkg and build dependencies (this sucks)
FROM debian:buster-slim AS vcpkg
RUN apt-get update && apt-get install -y curl zip unzip tar git build-essential pkg-config
WORKDIR /src
RUN git clone https://github.com/Microsoft/vcpkg.git
WORKDIR /src/vcpkg
RUN ./bootstrap-vcpkg.sh
WORKDIR /src
COPY vcpkg.json .
ENV VCPKG_INSTALLED_DIR=/src/vcpkg_installed
RUN ./vcpkg/vcpkg install

# Use the SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
RUN mkdir -p libs/GameNetworkingSockets
COPY --from=vcpkg /src/vcpkg_installed/x64-linux/lib/libGameNetworkingSockets.so /src/libs/GameNetworkingSockets
COPY SlopCrew.Server/SlopCrew.Server.csproj SlopCrew.Server/
COPY SlopCrew.Common/SlopCrew.Common.csproj SlopCrew.Common/
RUN dotnet restore "SlopCrew.Server/SlopCrew.Server.csproj"
COPY . .
WORKDIR "/src/SlopCrew.Server"
RUN dotnet build "SlopCrew.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SlopCrew.Server.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SlopCrew.Server.dll"]
