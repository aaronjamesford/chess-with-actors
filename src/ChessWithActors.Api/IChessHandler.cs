namespace ChessWithActors.Api;

public interface IChessHandler
{
    Task GameStarted(GameStarted started);
    Task PlayerJoined(PlayerJoined joined);
    Task InvalidMove(InvalidMove details);
    Task MoveMade(MoveMade move);
    Task GameEnded(GameEnded ended);
}