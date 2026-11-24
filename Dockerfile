# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["AutoTechHub/AutoTechHub.csproj", "AutoTechHub/"]
RUN dotnet restore "AutoTechHub/AutoTechHub.csproj"
COPY . .
WORKDIR "/src/AutoTechHub"
RUN dotnet publish "AutoTechHub.csproj" -c Release -o /app/publish

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV DOTNET_RUNNING_IN_CONTAINER=true
EXPOSE 80
ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://0.0.0.0:${PORT:-80} exec dotnet AutoTechHub.dll"]
