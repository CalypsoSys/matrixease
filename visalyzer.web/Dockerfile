FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /app 
#
# copy csproj and restore as distinct layers
COPY VisAlyzer.Web/*.csproj ./VisAlyzer.Web/
COPY Manga.IncTrak/*.csproj ./Manga.IncTrak/
#
RUN dotnet restore ./VisAlyzer.Web/VisAlyzer.Web.csproj
RUN dotnet restore ./Manga.IncTrak/Manga.IncTrak.csproj 
#
# copy everything else and build app
COPY VisAlyzer.Web/. ./VisAlyzer.Web/
COPY Manga.IncTrak/. ./Manga.IncTrak/
#
WORKDIR /app/VisAlyzer.Web
RUN dotnet publish -c Release -o out 
#
FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS runtime
WORKDIR /app 
#
COPY --from=build /app/VisAlyzer.Web/out ./
ENTRYPOINT ["dotnet", "VisAlyzer.Web.dll"]
