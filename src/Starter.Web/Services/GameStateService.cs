using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace Starter.Web.Services;

public class GameStateService
{
    private readonly ConcurrentDictionary<string, GameState> _gameStates = new();
    private readonly ConcurrentDictionary<string, List<ChannelWriter<GameEvent>>> _subscribers = new();

    public record GameState(
        string Background,
        List<Token> Tokens
    );

    public record Token(
        string Id,
        string UserId,
        string Username,
        string Color,
        int X,
        int Y
    );

    public record GameEvent(
        string Type,
        object Payload
    );

    public GameState GetGameState(string gameId = "default")
    {
        return _gameStates.GetOrAdd(gameId, _ => new GameState("", new List<Token>()));
    }

    public void SetBackground(string gameId, string url)
    {
        var state = _gameStates.GetOrAdd(gameId, _ => new GameState("", new List<Token>()));
        var newState = state with { Background = url };
        _gameStates[gameId] = newState;
        BroadcastEvent(gameId, new GameEvent("background_changed", new { url }));
    }

    public void AddToken(string gameId, Token token)
    {
        var state = _gameStates.GetOrAdd(gameId, _ => new GameState("", new List<Token>()));
        var tokens = new List<Token>(state.Tokens) { token };
        var newState = state with { Tokens = tokens };
        _gameStates[gameId] = newState;
        BroadcastEvent(gameId, new GameEvent("token_added", token));
    }

    public void MoveToken(string gameId, string tokenId, int x, int y)
    {
        var state = _gameStates.GetOrAdd(gameId, _ => new GameState("", new List<Token>()));
        var tokens = state.Tokens.Select(t =>
            t.Id == tokenId ? t with { X = x, Y = y } : t
        ).ToList();
        var newState = state with { Tokens = tokens };
        _gameStates[gameId] = newState;
        BroadcastEvent(gameId, new GameEvent("token_moved", new { tokenId, x, y }));
    }

    public async IAsyncEnumerable<GameEvent> Subscribe(string gameId, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var channel = Channel.CreateUnbounded<GameEvent>();
        var subscribers = _subscribers.GetOrAdd(gameId, _ => new List<ChannelWriter<GameEvent>>());
        
        lock (subscribers)
        {
            subscribers.Add(channel.Writer);
        }
        
        try
        {
            // Send initial state
            var initialState = GetGameState(gameId);
            yield return new GameEvent("game_state", initialState);

            await foreach (var evt in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return evt;
            }
        }
        finally
        {
            lock (subscribers)
            {
                subscribers.Remove(channel.Writer);
            }
            channel.Writer.Complete();
        }
    }

    private void BroadcastEvent(string gameId, GameEvent evt)
    {
        if (_subscribers.TryGetValue(gameId, out var subscribers))
        {
            List<ChannelWriter<GameEvent>>? toRemove = null;
            lock (subscribers)
            {
                foreach (var writer in subscribers)
                {
                    if (!writer.TryWrite(evt))
                    {
                        toRemove ??= new List<ChannelWriter<GameEvent>>();
                        toRemove.Add(writer);
                    }
                }
                
                if (toRemove != null)
                {
                    foreach (var writer in toRemove)
                    {
                        subscribers.Remove(writer);
                    }
                }
            }
        }
    }
}

