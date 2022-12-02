FROM mcr.microsoft.com/dotnet/aspnet:6.0

WORKDIR /app

COPY _build/out/api/ ./

ENTRYPOINT ["dotnet", "ChessWithActors.Api.dll"]