# syntax=docker/dockerfile:1.7

FROM node:20.16-alpine AS client-build
WORKDIR /src/src/StrideFlow.ClientApp

COPY src/StrideFlow.ClientApp/package.json src/StrideFlow.ClientApp/package-lock.json ./
RUN npm ci

COPY src/StrideFlow.ClientApp/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS server-build
WORKDIR /src

COPY StrideFlow.sln ./
COPY src/StrideFlow.Domain/StrideFlow.Domain.csproj src/StrideFlow.Domain/
COPY src/StrideFlow.Application/StrideFlow.Application.csproj src/StrideFlow.Application/
COPY src/StrideFlow.Infrastructure/StrideFlow.Infrastructure.csproj src/StrideFlow.Infrastructure/
COPY src/StrideFlow.Api/StrideFlow.Api.csproj src/StrideFlow.Api/

RUN dotnet restore src/StrideFlow.Api/StrideFlow.Api.csproj

COPY src/ ./src/
RUN dotnet publish src/StrideFlow.Api/StrideFlow.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

COPY --from=client-build /src/src/StrideFlow.ClientApp/dist /app/publish/wwwroot/spa

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=server-build /app/publish ./

EXPOSE 8080

ENTRYPOINT ["dotnet", "StrideFlow.Api.dll"]
