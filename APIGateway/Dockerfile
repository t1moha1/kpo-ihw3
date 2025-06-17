FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["APIGateway.csproj", "./"]
RUN dotnet restore "APIGateway.csproj"

COPY . .

RUN dotnet publish "APIGateway.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN apt-get update \
 && apt-get install -y netcat-openbsd \
 && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish ./

EXPOSE 80

ENTRYPOINT ["sh","-c","\
  echo 'Waiting for orders-service on port 80…'; \
  until nc -z orders-service 80; do sleep 1; done; \
  echo 'orders-service is up!'; \
  echo 'Waiting for payments-service on port 80…'; \
  until nc -z payments-service 80; do sleep 1; done; \
  echo 'payments-service is up!'; \
  exec dotnet APIGateway.dll \
"]