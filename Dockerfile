FROM mcr.microsoft.com/dotnet/sdk:7.0-alpine AS build
WORKDIR /src

COPY ./.config ./
RUN dotnet tool restore

COPY ./Groceries.sln ./
COPY */*.csproj ./
RUN for file in $(ls *.csproj); do mkdir -p ${file%.*} && mv $file ${file%.*}; done
RUN dotnet restore

WORKDIR Groceries
COPY ./Groceries/libman.json ./
RUN dotnet libman restore

COPY . ../
RUN dotnet publish --no-restore --output /out

FROM mcr.microsoft.com/dotnet/aspnet:7.0-alpine AS base
WORKDIR /groceries

COPY --from=build /out .
COPY --from=build /src/Groceries/config.ini /config/

RUN apk add --no-cache icu-libs tzdata

ENV DOTNET_ENABLEDIAGNOSTICS=0
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

EXPOSE 80
VOLUME /config
ENTRYPOINT ["./Groceries", "--data", "/config"]
