# Use the official image as a parent image
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80

# Use the SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
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
