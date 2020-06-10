#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/runtime:3.1-buster-slim AS base
ENV DOTNET_USE_POLLING_FILE_WATCHER 1
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["LOMCN.DiscordBot/LOMCN.DiscordBot.csproj", "LOMCN.DiscordBot/"]
RUN dotnet restore "LOMCN.DiscordBot/LOMCN.DiscordBot.csproj"
COPY . .
WORKDIR "/src/LOMCN.DiscordBot"
RUN dotnet build "LOMCN.DiscordBot.csproj" --build-arg "my_arg=NTgwNzE0NzMwNjIxMTA4MjQ0.XmqTug.7WhifKe9-Q6aV0TH58eyvUDPAy4" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LOMCN.DiscordBot.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT dotnet watch run --no-restore