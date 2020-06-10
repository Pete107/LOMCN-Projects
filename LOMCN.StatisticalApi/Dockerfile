#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["LOMCN.StatisticalApi/LOMCN.StatisticalApi.csproj", "LOMCN.StatisticalApi/"]
COPY ["LOMCN.Common/LOMCN.Common.csproj", "LOMCN.Common/"]
RUN dotnet restore "LOMCN.StatisticalApi/LOMCN.StatisticalApi.csproj"
COPY . .
WORKDIR "/src/LOMCN.StatisticalApi"
RUN dotnet build "LOMCN.StatisticalApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LOMCN.StatisticalApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LOMCN.StatisticalApi.dll"]