FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Build.props", "Directory.Build.targets", "Directory.Packages.props", "NuGet.config", "./"]
COPY ["SimplyBudget.Api/SimplyBudget.Api.csproj", "SimplyBudget.Api/"]
COPY ["SimplyBudget.Data/SimplyBudget.Data.csproj", "SimplyBudget.Data/"]
COPY ["SimplyBudget.ServiceDefaults/SimplyBudget.ServiceDefaults.csproj", "SimplyBudget.ServiceDefaults/"]
COPY ["SimplyBudgetShared/SimplyBudgetShared.csproj", "SimplyBudgetShared/"]
RUN dotnet restore "SimplyBudget.Api/SimplyBudget.Api.csproj"
COPY . .
WORKDIR "/src/SimplyBudget.Api"
RUN dotnet build "SimplyBudget.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "SimplyBudget.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SimplyBudget.Api.dll"]
