FROM mcr.microsoft.com/dotnet/sdk:7.0-alpine AS build
WORKDIR /src
COPY . ./
WORKDIR Groceries
RUN dotnet restore
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
