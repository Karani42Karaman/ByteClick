FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["ByteClick.Web/ByteClick.Web.csproj", "ByteClick.Web/"]
COPY ["ByteClick.Data/ByteClick.Data.csproj", "ByteClick.Data/"]
RUN dotnet restore "ByteClick.Web/ByteClick.Web.csproj"
COPY . .
WORKDIR "/src/ByteClick.Web"
RUN dotnet build "ByteClick.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ByteClick.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ByteClick.Web.dll"]