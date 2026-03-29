FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /app 
#
# copy csproj and restore as distinct layers
COPY MatrixEase.Web/*.csproj ./MatrixEase.Web/
COPY MatrixEase.Manga/*.csproj ./MatrixEase.Manga/
#
RUN dotnet restore ./MatrixEase.Web/MatrixEase.Web.csproj
RUN dotnet restore ./MatrixEase.Manga/MatrixEase.Manga.csproj 
#
# copy everything else and build app
COPY MatrixEase.Web/. ./MatrixEase.Web/
COPY MatrixEase.Manga/. ./MatrixEase.Manga/
#
WORKDIR /app/MatrixEase.Web
RUN dotnet publish -c Release -o out 
#
FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS runtime
WORKDIR /app 
#
COPY --from=build /app/MatrixEase.Web/out ./
ENTRYPOINT ["dotnet", "MatrixEase.Web.dll"]
