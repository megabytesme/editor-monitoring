FROM node:18 AS build-react
WORKDIR /app

COPY src/monitor-frontend/package.json src/monitor-frontend/package-lock.json ./
RUN npm install

COPY src/monitor-frontend/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build-csharp
WORKDIR /src

COPY src/monitor-service/*.csproj ./
RUN dotnet restore

COPY src/monitor-service/ ./
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS final
WORKDIR /app

COPY --from=build-react /app/build /app/monitor-frontend
COPY --from=build-csharp /app /app/monitor-service

RUN apt-get update && apt-get install -y sqlite3 npm \
    && npm install -g http-server

VOLUME ["/app/data"]

EXPOSE 3000 5000

CMD ["sh", "-c", "dotnet /app/monitor-service/monitor-service.dll & http-server /app/monitor-frontend -p 3000"]
