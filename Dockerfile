ARG GITHUB_TOKEN
ARG GITHUB_USER
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5003

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Loans.API/Loans.API.csproj", "Loans.API/"]
RUN dotnet nuget update source github --username ${GITHUB_USER} --password ${GITHUB_TOKEN}
RUN dotnet restore "Loans.API/Loans.API.csproj"
COPY src/Loans.API/. Loans.API/
WORKDIR "/src/Loans.API"
RUN dotnet build "Loans.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Loans.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:5003
ENTRYPOINT ["dotnet", "Loans.API.dll"]
