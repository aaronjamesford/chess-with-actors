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
    public async Task GivenTheGameIsCreatedThenAnEventIsPublished()
    {
        var game = Guid.NewGuid().ToString();
        var gamePid = (await _actorSystem.Cluster().GetChessGame(game))!;

        var probe = new TestProbe();
        var probePid = _actorSystem.Root.Spawn(Props.FromProducer(_ => probe));

        await _actorSystem.Cluster().Subscribe(ChessGame.Topic(game), probePid);
        await _actorSystem.Cluster().Subscribe(Topics.Notifications, probePid);
        _subscribers.Add((probePid, ChessGame.Topic(game)));
        _subscribers.Add((probePid, Topics.Notifications));
        
        _actorSystem.Root.Send(gamePid, new CreateGame { GameId = game });

        var evt = probe.FishForMessage<GameCreated>();
        Assert.Equal(game, evt.GameId);
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
    public async Task GivenBothPlayersJoinThenGameStartedEventIsPublished()
    {
        var game = Guid.NewGuid().ToString();
        var gamePid = (await _actorSystem.Cluster().GetChessGame(game))!;

        var probe = new TestProbe();
        var probePid = _actorSystem.Root.Spawn(Props.FromProducer(_ => probe));

        await _actorSystem.Cluster().Subscribe(ChessGame.Topic(game), probePid);
        await _actorSystem.Cluster().Subscribe(Topics.Notifications, probePid);
        _subscribers.Add((probePid, ChessGame.Topic(game)));
        _subscribers.Add((probePid, Topics.Notifications));
        
        _actorSystem.Root.Send(gamePid, new CreateGame { GameId = game });
        _actorSystem.Root.Send(gamePid, new JoinGame { Username = "a" });
        _actorSystem.Root.Send(gamePid, new JoinGame { Username = "b" });

        var evt = probe.FishForMessage<GameStarted>();
        Assert.Equal(game, evt.GameId);
        Assert.Equal("a", evt.WhitePlayer);
        Assert.Equal("b", evt.BlackPlayer);
    }

    [Fact]
    public async Task GivenBothPlayersJoinThenPlayerJoinedEventsArePublished()
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

        var evt = probe.ProcessMessages<PlayerJoined>().ToArray();
        Assert.Contains(evt, pj => pj.GameId == game && pj is { Username: "a", Player: ChessPlayerType.White });
        Assert.Contains(evt, pj => pj.GameId == game && pj is { Username: "b", Player: ChessPlayerType.Black });
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

    [Fact]
    public async Task GivenFullGameThenEndGameEventIsPublished()
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
        
        probe.Request(gamePid, new MakeMove { Username = "a", From = "e2", To = "e4" });
        probe.Request(gamePid, new MakeMove { Username = "b", From = "e7", To = "e5" });
        
        probe.Request(gamePid, new MakeMove { Username = "a", From = "d1", To = "h5" });
        probe.Request(gamePid, new MakeMove { Username = "b", From = "b8", To = "c6" });
        
        probe.Request(gamePid, new MakeMove { Username = "a", From = "f1", To = "c4" });
        probe.Request(gamePid, new MakeMove { Username = "b", From = "d7", To = "d6" });
        
        probe.Request(gamePid, new MakeMove { Username = "a", From = "h5", To = "f7" });

        var evt = probe.FishForMessage<GameEnded>();
        Assert.Equal(game, evt.GameId);
        Assert.Equal(ChessWinner.WhiteWin, evt.Winner);
        Assert.Equal(ChessEndReason.Checkmate, evt.EndReason);
    }
}