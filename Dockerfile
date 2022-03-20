FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /app 
#
# copy csproj and restore as distinct layers
COPY matrixease.web/*.csproj ./matrixease.web/
COPY Manga.IncTrak/*.csproj ./Manga.IncTrak/
COPY ExcelReader/*.csproj ./ExcelReader/
#
RUN dotnet restore ./matrixease.web/matrixease.web.csproj
RUN dotnet restore ./Manga.IncTrak/Manga.IncTrak.csproj 
RUN dotnet restore ./ExcelReader/ExcelReader.csproj 
#
# copy everything else and build app
COPY matrixease.web/. ./matrixease.web/
COPY Manga.IncTrak/. ./Manga.IncTrak/
COPY ExcelReader/. ./ExcelReader/
#
WORKDIR /app/matrixease.web
RUN dotnet publish -c Release -o out 
#
FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS runtime
WORKDIR /app 
#
COPY --from=build /app/matrixease.web/out ./
ENTRYPOINT ["dotnet", "matrixease.web.dll"]
