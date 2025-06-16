# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 1) Копируем файл проекта и восстанавливаем зависимости
COPY *.csproj ./
RUN dotnet restore

# 2) Копируем всё остальное и публикуем в Release-режиме
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Копируем артефакты из стадии сборки
COPY --from=build /app/publish .

# Открываем порт (по умолчанию ASP.NET Core слушает 80)
EXPOSE 80

# Точка входа
ENTRYPOINT ["dotnet", "APIGateway.dll"]