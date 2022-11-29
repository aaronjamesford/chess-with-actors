using ChessWithActors.Backend;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddChessBackendProtoActor();

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();

public partial class Program { } // For testing