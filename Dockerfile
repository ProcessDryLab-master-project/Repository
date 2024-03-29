#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base

WORKDIR /app

EXPOSE 4000
EXPOSE 4001
ENV ASPNETCORE_URLS=https://*:4000
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build

#RUN dotnet dev-certs https

WORKDIR /src

COPY ["Repository.csproj", "."]
RUN dotnet restore "./Repository.csproj"
COPY . .

WORKDIR "/src/."

RUN dotnet build "Repository.csproj" -c Release -o /app/build
FROM build AS publish
RUN dotnet publish "Repository.csproj" -c Release -o /app/publish /p:UseAppHost=false
FROM base AS final

WORKDIR /app

#COPY --from=publish /root/.dotnet/corefx/cryptography/x509stores/my/* /root/.dotnet/corefx/cryptography/x509stores/my/
COPY --from=publish /app/publish .
COPY ["Resources", "./Resources"] 
ENTRYPOINT ["dotnet", "Repository.dll"]