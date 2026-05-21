FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
WORKDIR /src

COPY MatrixEase.Web/MatrixEase.Web.csproj MatrixEase.Web/
COPY MatrixEase.Manga/MatrixEase.Manga.csproj MatrixEase.Manga/
RUN dotnet restore MatrixEase.Web/MatrixEase.Web.csproj -a ${TARGETARCH:-amd64}

COPY MatrixEase.Web/ MatrixEase.Web/
COPY MatrixEase.Manga/ MatrixEase.Manga/
RUN dotnet publish MatrixEase.Web/MatrixEase.Web.csproj \
    -c Release \
    -a ${TARGETARCH:-amd64} \
    --no-restore \
    --self-contained false \
    /p:SkipWebBlaster=true \
    -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish/ ./

ENTRYPOINT ["dotnet", "MatrixEase.Web.dll"]
