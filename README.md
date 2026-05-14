# Chess with Actors

A real-time multiplayer chess application built to answer a question that came up in a system design interview: *are actors actually a good fit for a game like this?*

The answer is yes — and this repo is the proof. It's deliberately production-like: distributed across services, deployed to Kubernetes via GitOps, and wired up with a full observability stack.

---

## Architecture

```
┌──────────────────────────────────────┐
│  Browser                             │
│  Angular + ngx-chess-board           │
└───────────────┬──────────────────────┘
                │ WebSocket (SignalR)
┌───────────────▼──────────────────────┐
│  API Service                         │
│  ChessHub (SignalR)                  │
│  HubUser Actor  ← one per connection │
└───────────────┬──────────────────────┘
                │ Proto.Actor Cluster (gRPC)
┌───────────────▼──────────────────────┐
│  Backend Service                     │
│  ChessGameActor  ← one per game      │
│  validates moves, owns game state    │
└───────────────┬──────────────────────┘
                │ Cluster Pub/Sub
┌───────────────▼──────────────────────┐
│  Redis                               │
│  Subscriber store                    │
│  Topics: chess-{gameId}, global      │
└──────────────────────────────────────┘
```

### How a move travels through the system

1. A player makes a move in the browser; the Angular client sends it over a SignalR WebSocket.
2. `ChessHub` forwards it to the connection's dedicated `HubUser` actor.
3. `HubUser` resolves the `ChessGameActor` for that game via the cluster and sends it a `MakeMove` message.
4. `ChessGameActor` validates the move (using the Gera.Chess engine), updates state, and publishes a `MoveMade` event to the `chess-{gameId}` pub/sub topic.
5. Redis distributes the event to every subscribed `HubUser` across **all** API nodes — not just the one that received the move.
6. Each `HubUser` pushes the event to its SignalR connection, updating both players' boards.

The key property: game state lives entirely inside one actor. No locks. No shared memory. Concurrent moves from both players are serialised naturally by the actor's mailbox.

---

## Why Proto.Actor?

Orleans has virtual actors and lower operational friction. Akka.NET has a larger ecosystem. I chose Proto.Actor because:

- It keeps the original actor feel — explicit activation, explicit messaging — rather than abstracting it away with virtual actors.
- It has significantly less overhead and operational complexity than Akka.
- The cluster pub/sub primitive maps directly onto the fan-out problem: one game event needs to reach all connected clients, potentially across multiple API nodes.

Actors aren't always the right answer. For a chess game specifically, the actor-per-game model gives you isolated mutable state for free — no ORM, no row-level locking, no cache invalidation. Whether that trade-off is worth it depends on your team and your scale. This project is partly a demonstration that it *can* work, and partly a reminder that the actor model is underused in the .NET world.

---

## Stack

| Layer | Technology |
|---|---|
| Frontend | Angular 15, ngx-chess-board, SignalR client |
| API | .NET 6, ASP.NET SignalR, Proto.Actor |
| Backend | .NET 6, Proto.Actor Cluster, Proto.Cluster.Kubernetes |
| Messaging | Protocol Buffers (protobuf3) |
| Pub/Sub store | Redis (StackExchange.Redis) |
| Chess engine | Gera.Chess |
| Logging | Serilog → Grafana Loki |
| Tracing | OpenTelemetry → Grafana Tempo |
| Deployment | Kubernetes, Flux (GitOps) |
| CI | Jenkins |

---

## Repository Layout

```
src/
  ChessWithActors.Api/          # SignalR gateway; HubUser actor per connection
  ChessWithActors.Backend/      # ChessGameActor; cluster pub/sub; Redis subscriber store
  ChessWithActors.Comms/        # Shared protobuf messages and cluster helpers
  ChessWithActors.Backend.Tests/ # Integration tests against a real actor system
  chess-ui/                     # Angular frontend

cd/
  app/                          # Kubernetes manifests for API, Backend, UI
  infrastructure/               # Ingress, Redis operator, RabbitMQ operator, monitoring
  flux-system/                  # Flux GitOps configuration and image automation
```

---

## Tests

The test suite spins up a real `ActorSystem` (using `WebApplicationFactory`) and tests actor behaviour end-to-end — no mocks of the actor layer.

```
dotnet test src/ChessWithActors.Backend.Tests
```

Covered scenarios include: game creation, player joining, turn enforcement, invalid move rejection, and a full Scholar's Mate to verify end-game detection.

---

## Running Locally

The application is designed to run on Kubernetes and the cluster management doesn't translate well to a local `dotnet run` setup. If you want to run it, you'll need:

- A Kubernetes cluster (local or remote)
- Redis available to the Backend service
- The manifests in `cd/app/` applied with Flux or `kubectl`

For a quick look at the code without running it, the core logic is in:
- [`ChessGameActor.cs`](src/ChessWithActors.Backend/Actors/ChessGameActor.cs) — game state and move validation
- [`HubUser.cs`](src/ChessWithActors.Api/Actors/HubUser.cs) — per-connection actor bridging SignalR to the cluster
- [`Messages.proto`](src/ChessWithActors.Comms/Messages.proto) — the full message contract

---

## Known Limitations

Player identity is unauthenticated — usernames are client-supplied strings. A production implementation would validate against a JWT before processing hub commands. This is intentionally out of scope for a demo that's focused on the distributed architecture rather than the auth layer.
