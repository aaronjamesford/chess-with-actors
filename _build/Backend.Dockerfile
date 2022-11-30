FROM mcr.microsoft.com/dotnet/aspnet:6.0

WORKDIR /app

COPY _build/out/backend/ ./

ENTRYPOINT ["dotnet", "ChessWithActors.Backend.dll"]