FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY PMIS.csproj ./
RUN dotnet restore PMIS.csproj

COPY . .
RUN dotnet publish PMIS.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

USER app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "PMIS.dll"]
