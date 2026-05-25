# ByteClick/Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5001
EXPOSE 7021

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["ByteClick/ByteClick.csproj", "ByteClick/"]
COPY ["ByteClick.Data/ByteClick.Data.csproj", "ByteClick.Data/"]
RUN dotnet restore "ByteClick/ByteClick.csproj"
COPY . .
WORKDIR "/src/ByteClick"
RUN dotnet build "ByteClick.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ByteClick.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ByteClick.dll"]