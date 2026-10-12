# Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY DKP.slnx Directory.Packages.props ./
COPY src ./src
RUN dotnet restore src/DKP.Blazor/DKP.Blazor.csproj
RUN dotnet publish src/DKP.Blazor/DKP.Blazor.csproj --configuration Release --no-restore --output /app/publish /p:UseAppHost=false

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
EXPOSE 8080
COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "DKP.Blazor.dll"]
