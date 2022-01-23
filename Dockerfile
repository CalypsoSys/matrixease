FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /app 
#
# copy csproj and restore as distinct layers
COPY visalyzer.web/*.csproj ./visalyzer.web/
COPY Manga.IncTrak/*.csproj ./Manga.IncTrak/
#
RUN dotnet restore ./visalyzer.web/visalyzer.web.csproj
RUN dotnet restore ./Manga.IncTrak/Manga.IncTrak.csproj 
#
# copy everything else and build app
COPY visalyzer.web/. ./visalyzer.web/
COPY Manga.IncTrak/. ./Manga.IncTrak/
#
WORKDIR /app/visalyzer.web
RUN dotnet publish -c Release -o out 
#
FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS runtime
WORKDIR /app 
#
COPY --from=build /app/visalyzer.web/out ./
ENTRYPOINT ["dotnet", "visalyzer.web.dll"]
