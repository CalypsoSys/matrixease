FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /app 
#
# copy csproj and restore as distinct layers
COPY MatrixEase.Web/*.csproj ./MatrixEase.Web/
COPY MatrixEase.Manga/*.csproj ./MatrixEase.Manga/
COPY ExcelReader/*.csproj ./ExcelReader/
#
RUN dotnet restore ./MatrixEase.Web/MatrixEase.Web.csproj
RUN dotnet restore ./MatrixEase.Manga/MatrixEase.Manga.csproj 
RUN dotnet restore ./ExcelReader/ExcelReader.csproj 
#
# copy everything else and build app
COPY MatrixEase.Web/. ./MatrixEase.Web/
COPY MatrixEase.Manga/. ./MatrixEase.Manga/
COPY ExcelReader/. ./ExcelReader/
#
WORKDIR /app/matrixease_web
RUN dotnet publish -c Release -o out 
#
FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS runtime
WORKDIR /app 
#
COPY --from=build /app/matrixease_web/out ./
ENTRYPOINT ["dotnet", "MatrixEase.Web.dll"]
