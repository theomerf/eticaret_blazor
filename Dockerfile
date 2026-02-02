# 1. Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . .

WORKDIR /src/ETicaret
RUN dotnet restore ../ETicaret.sln
RUN dotnet publish ETicaret.csproj -c Release -o /app/out

# 2. Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out ./
ENTRYPOINT ["dotnet", "ETicaret.dll"]
