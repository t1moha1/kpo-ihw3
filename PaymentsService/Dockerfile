
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src


COPY ["PaymentsService.csproj", "./"]
RUN dotnet restore "PaymentsService.csproj"


COPY . .
RUN dotnet publish "PaymentsService.csproj" -c Release -o /app/publish


FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish ./

EXPOSE 80

ENTRYPOINT ["dotnet", "PaymentsService.dll"]