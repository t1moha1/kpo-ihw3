# Stage: build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Восстановление и сборка
COPY ["APIGateway.csproj", "./"]
RUN dotnet restore "APIGateway.csproj"

# Копируем весь код и публикуем
COPY . .
RUN dotnet publish "APIGateway.csproj" -c Release -o /app/publish

# Stage: runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./

EXPOSE 80
ENTRYPOINT ["dotnet", "APIGateway.dll"]