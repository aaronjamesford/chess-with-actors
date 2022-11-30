using ChessWithActors.Comms;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Proto;
using Proto.Cluster;
using Proto.Cluster.PubSub;
using Proto.TestKit;

namespace ChessWithActors.Backend.Tests.Actors;

public class ChessActorTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncDisposable
{
    private readonly ActorSystem _actorSystem;

    private readonly List<(PID pid, string topic)> _subscribers = new();

    public ChessActorTests(WebApplicationFactory<Program> factory)
    {
        _actorSystem = factory.Services.GetRequiredService<ActorSystem>();
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var (pid, topic) in _subscribers)
        {
            await _actorSystem.Cluster().Unsubscribe(topic, pid);
        }
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

    [Fact]
    public async Task GivenWhitePlayerMovesFirstThenAnEventIsPublished()
    {
        var game = Guid.NewGuid().ToString();
        var gamePid = (await _actorSystem.Cluster().GetChessGame(game))!;

        var probe = new TestProbe();
        var probePid = _actorSystem.Root.Spawn(Props.FromProducer(_ => probe));

        await _actorSystem.Cluster().Subscribe(ChessGame.Topic(game), probePid);
        _subscribers.Add((probePid, ChessGame.Topic(game)));
        
        _actorSystem.Root.Send(gamePid, new CreateGame { GameId = game });
        _actorSystem.Root.Send(gamePid, new JoinGame { Username = "a" });
        _actorSystem.Root.Send(gamePid, new JoinGame { Username = "b" });
        
        probe.Request(gamePid, new MakeMove { Username = "a", From = "a2", To = "a4" });

        var evt = probe.FishForMessage<MoveMade>();
        Assert.Equal(game, evt.GameId);
        Assert.Equal("a", evt.Username);
        Assert.Equal("a2", evt.From);
        Assert.Equal("a4", evt.To);
    }

    [Fact]
    public async Task GivenBlackPlayerMovesFirstThenAnErrorIsReturned()
    {
        var game = Guid.NewGuid().ToString();
        var gamePid = (await _actorSystem.Cluster().GetChessGame(game))!;
        
        var probe = new TestProbe();
        var probePid = _actorSystem.Root.Spawn(Props.FromProducer(_ => probe));

        await _actorSystem.Cluster().Subscribe(ChessGame.Topic(game), probePid);
        _subscribers.Add((probePid, ChessGame.Topic(game)));
        
        _actorSystem.Root.Send(gamePid, new CreateGame { GameId = game });
        _actorSystem.Root.Send(gamePid, new JoinGame { Username = "a" });
        _actorSystem.Root.Send(gamePid, new JoinGame { Username = "b" });
        
        probe.Request(gamePid, new MakeMove { Username = "b", From = "a7", To = "a5" });

        var error = probe.FishForMessage<InvalidMove>();
        Assert.Equal(game, error.GameId);
        Assert.Equal("b", error.Username);
        Assert.Equal("a7", error.From);
        Assert.Equal("a5", error.To);
        Assert.Equal("WaitingOpponentMove", error.Reason);
    }

    [Fact]
    public async Task GivenBlackPlayerMovesFirstThenNoEventIsEmitted()
    {
        var game = Guid.NewGuid().ToString();
        var gamePid = (await _actorSystem.Cluster().GetChessGame(game))!;
        
        var probe = new TestProbe();
        var probePid = _actorSystem.Root.Spawn(Props.FromProducer(_ => probe));

        await _actorSystem.Cluster().Subscribe(ChessGame.Topic(game), probePid);
        _subscribers.Add((probePid, ChessGame.Topic(game)));
        
        _actorSystem.Root.Send(gamePid, new CreateGame { GameId = game });
        _actorSystem.Root.Send(gamePid, new JoinGame { Username = "a" });
        _actorSystem.Root.Send(gamePid, new JoinGame { Username = "b" });
        
        probe.Request(gamePid, new MakeMove { Username = "b", From = "a7", To = "a5" });

        Assert.Empty(probe.ProcessMessages<MoveMade>());
    }

    [Fact]
    public async Task GivenInvalidMoveThenAnErrorIsReturned()
    {
        var game = Guid.NewGuid().ToString();
        var gamePid = (await _actorSystem.Cluster().GetChessGame(game))!;
        
        var probe = new TestProbe();
        var probePid = _actorSystem.Root.Spawn(Props.FromProducer(_ => probe));

        await _actorSystem.Cluster().Subscribe(ChessGame.Topic(game), probePid);
        _subscribers.Add((probePid, ChessGame.Topic(game)));
        
        _actorSystem.Root.Send(gamePid, new CreateGame { GameId = game });
        _actorSystem.Root.Send(gamePid, new JoinGame { Username = "a" });
        _actorSystem.Root.Send(gamePid, new JoinGame { Username = "b" });
        
        probe.Request(gamePid, new MakeMove { Username = "a", From = "a7", To = "a5" });

        var error = probe.FishForMessage<InvalidMove>();
        Assert.Equal(game, error.GameId);
        Assert.Equal("a", error.Username);
        Assert.Equal("a7", error.From);
        Assert.Equal("a5", error.To);
        Assert.Equal("InvalidMove", error.Reason);
    }

    [Fact]
    public async Task GivenInvalidMoveThenNoEventIsEmitted()
    {
        var game = Guid.NewGuid().ToString();
        var gamePid = (await _actorSystem.Cluster().GetChessGame(game))!;
        
        var probe = new TestProbe();
        var probePid = _actorSystem.Root.Spawn(Props.FromProducer(_ => probe));

        await _actorSystem.Cluster().Subscribe(ChessGame.Topic(game), probePid);
        _subscribers.Add((probePid, ChessGame.Topic(game)));
        
        _actorSystem.Root.Send(gamePid, new CreateGame { GameId = game });
        _actorSystem.Root.Send(gamePid, new JoinGame { Username = "a" });
        _actorSystem.Root.Send(gamePid, new JoinGame { Username = "b" });
        
        probe.Request(gamePid, new MakeMove { Username = "a", From = "a7", To = "a5" });

        Assert.Empty(probe.ProcessMessages<MoveMade>());
    }

    [Fact]
    public async Task GivenBlackPlayerMovesAfterWhiteThenAnEventIsPublished()
    {
        var game = Guid.NewGuid().ToString();
        var gamePid = (await _actorSystem.Cluster().GetChessGame(game))!;

        var probe = new TestProbe();
        var probePid = _actorSystem.Root.Spawn(Props.FromProducer(_ => probe));

        await _actorSystem.Cluster().Subscribe(ChessGame.Topic(game), probePid);
        _subscribers.Add((probePid, ChessGame.Topic(game)));
        
        _actorSystem.Root.Send(gamePid, new CreateGame { GameId = game });
        _actorSystem.Root.Send(gamePid, new JoinGame { Username = "a" });
        _actorSystem.Root.Send(gamePid, new JoinGame { Username = "b" });
        
        probe.Request(gamePid, new MakeMove { Username = "a", From = "a2", To = "a4" });
        probe.Request(gamePid, new MakeMove { Username = "b", From = "a7", To = "a5" });

        var evts = probe.ProcessMessages<MoveMade>();
        Assert.Contains(evts, move => move is { Username: "b", From: "a7", To: "a5" });
    }

    [Fact]
    public async Task GivenWhiteMovesBeforeBlackThenAnErrorIsReturned()
    {
        var game = Guid.NewGuid().ToString();
        var gamePid = (await _actorSystem.Cluster().GetChessGame(game))!;
        
        var probe = new TestProbe();
        var probePid = _actorSystem.Root.Spawn(Props.FromProducer(_ => probe));

        await _actorSystem.Cluster().Subscribe(ChessGame.Topic(game), probePid);
        _subscribers.Add((probePid, ChessGame.Topic(game)));
        
        _actorSystem.Root.Send(gamePid, new CreateGame { GameId = game });
        _actorSystem.Root.Send(gamePid, new JoinGame { Username = "a" });
        _actorSystem.Root.Send(gamePid, new JoinGame { Username = "b" });
        
        probe.Request(gamePid, new MakeMove { Username = "a", From = "a2", To = "a4" });
        probe.Request(gamePid, new MakeMove { Username = "a", From = "b2", To = "b4" });

        var error = probe.FishForMessage<InvalidMove>();
        Assert.Equal(game, error.GameId);
        Assert.Equal("a", error.Username);
        Assert.Equal("b2", error.From);
        Assert.Equal("b4", error.To);
        Assert.Equal("WaitingOpponentMove", error.Reason);
    }
}