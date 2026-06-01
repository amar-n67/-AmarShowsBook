# # STEP 1 - build stage
# FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
# WORKDIR /app

# COPY . ./
# RUN dotnet restore
# RUN dotnet publish -c Release -o out

# # STEP 2 - runtime stage
# FROM mcr.microsoft.com/dotnet/aspnet:8.0
# WORKDIR /app

# COPY --from=build /app/out ./

# ENV ASPNETCORE_URLS=http://+:8080
# EXPOSE 8080

# ENTRYPOINT ["dotnet", "AmarShowsBook.dll"]

# FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build

WORKDIR /src

COPY . .

RUN dotnet restore

RUN dotnet publish -c Release -o /app/publish

# FROM mcr.microsoft.com/dotnet/aspnet:9.0
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:10000

EXPOSE 10000

ENTRYPOINT ["dotnet","AmarShowsBook.dll"]