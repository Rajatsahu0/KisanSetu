FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/KisanMitraAI.API/KisanMitraAI.API.csproj", "src/KisanMitraAI.API/"]
COPY ["src/KisanMitraAI.Core/KisanMitraAI.Core.csproj", "src/KisanMitraAI.Core/"]
COPY ["src/KisanMitraAI.Infrastructure/KisanMitraAI.Infrastructure.csproj", "src/KisanMitraAI.Infrastructure/"]
RUN dotnet restore "src/KisanMitraAI.API/KisanMitraAI.API.csproj"
COPY . .
WORKDIR "/src/src/KisanMitraAI.API"
RUN dotnet build -c Release -o /app/build
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "KisanMitraAI.API.dll"]
