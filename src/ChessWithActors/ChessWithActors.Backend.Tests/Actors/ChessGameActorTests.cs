using ChessWithActors.Comms;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Proto;
using Proto.Cluster;

namespace ChessWithActors.Backend.Tests.Actors;

public class ChessActorTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly ActorSystem _actorSystem;

    public ChessActorTests(WebApplicationFactory<Program> factory)
    {
        _actorSystem = factory.Services.GetRequiredService<ActorSystem>();
    }

    [Fact]
    public async Task GivenTheGameIsNotCreatedThenTheGameStateIsCorrect()
    {
        var state = await _actorSystem.Cluster().GetChessGameState(Guid.NewGuid().ToString());
        
        Assert.Equal(GameState.DoesNotExist, state);
    }

    [Fact]
    public async Task GivenTheGameIsCreatedThenTheGameStateIsCorrect()
    {
        var game = Guid.NewGuid().ToString();
        var gamePid = (await _actorSystem.Cluster().GetChessGame(game))!;
        
        _actorSystem.Root.Send(gamePid, new CreateGame { GameId = game });

        var state = await _actorSystem.Cluster().GetChessGameState(game);
        
        Assert.Equal(GameState.PendingPlayerJoin, state);
    }

    [Fact]
    public async Task GivenBothPlayersJoinThenTheGameStateIsCorrect()
    {
        var game = Guid.NewGuid().ToString();
        var gamePid = (await _actorSystem.Cluster().GetChessGame(game))!;
        
        _actorSystem.Root.Send(gamePid, new CreateGame { GameId = game });
        _actorSystem.Root.Send(gamePid, new JoinGame { Username = "a" });
        _actorSystem.Root.Send(gamePid, new JoinGame { Username = "b" });

        var state = await _actorSystem.Cluster().GetChessGameState(game);
        
        Assert.Equal(GameState.InProgress, state);
    }
}