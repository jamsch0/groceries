# syntax=docker/dockerfile:1.7-labs

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build1

WORKDIR /src
COPY ./.config ./
RUN dotnet tool restore

WORKDIR Groceries
COPY ./Groceries/libman.json ./
RUN dotnet libman restore

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build2

WORKDIR /src
COPY ./Groceries.slnx ./
COPY ./Directory.Build.props ./

COPY --parents */*.csproj .
RUN dotnet restore

COPY . ./
COPY --from=build1 /src ./
RUN dotnet publish --no-restore --output /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine-composite AS base
WORKDIR /groceries

COPY --from=build2 /out .
COPY --from=build2 /src/Groceries/config.ini /config/

RUN apk add --no-cache icu-libs tzdata

ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
ENV ASPNETCORE_HTTP_PORTS=80
ENV DOTNET_ENABLEDIAGNOSTICS=0
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

EXPOSE 80
VOLUME /config
ENTRYPOINT ["./Groceries", "--data", "/config"]
