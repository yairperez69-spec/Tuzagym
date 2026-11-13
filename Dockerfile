
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY Tuzagym.API/ ./Tuzagym.API/

RUN dotnet restore "Tuzagym.API/Tuzagym.API.csproj"

RUN dotnet publish "Tuzagym.API/Tuzagym.API.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:10000

EXPOSE 10000

ENTRYPOINT ["dotnet", "Tuzagym.API.dll"]
