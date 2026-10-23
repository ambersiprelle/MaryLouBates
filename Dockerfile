FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
COPY src/ProductSite/*.fsproj ./src/ProductSite/
RUN dotnet restore src/ProductSite/ProductSite.fsproj
COPY src/ProductSite/ ./src/ProductSite/
RUN dotnet publish src/ProductSite/ProductSite.fsproj -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /out .

# Static files
COPY wwwroot/ ./wwwroot/

# SQLite data dir (Fly volume mounts here)
RUN mkdir -p /data

ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "ProductSite.dll"]
