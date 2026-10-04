FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8001
ENV ASPNETCORE_URLS=http://+:8001

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY CiCd.Gui/CiCd.Gui.csproj CiCd.Gui/
COPY CiCd.Facade/CiCd.Facade.csproj CiCd.Facade/
COPY CiCd.Domain/CiCd.Domain.csproj CiCd.Domain/
COPY CiCd.UseCase/CiCd.UseCase.csproj CiCd.UseCase/
COPY CiCd.Infrastructure/CiCd.Infrastructure.csproj CiCd.Infrastructure/

RUN dotnet restore "CiCd.Gui/CiCd.Gui.csproj"

COPY CiCd.Gui/ CiCd.Gui/
COPY CiCd.Facade/ CiCd.Facade/
COPY CiCd.Domain/ CiCd.Domain/
COPY CiCd.UseCase/ CiCd.UseCase/
COPY CiCd.Infrastructure/ CiCd.Infrastructure/

RUN dotnet build "CiCd.Gui/CiCd.Gui.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
RUN dotnet publish "CiCd.Gui/CiCd.Gui.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "CiCd.Gui.dll"]