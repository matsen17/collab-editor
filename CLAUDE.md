# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

CollabEditor is a real-time collaborative text editor built with .NET 9.0, implementing Domain-Driven Design (DDD) with event-driven architecture using RabbitMQ for message distribution and WebSockets for client communication.

## Commands

### Development Setup
```bash
# Start infrastructure (PostgreSQL + RabbitMQ)
docker-compose up -d

# Apply database migrations
dotnet ef database update --project CollabEditor.Infrastructure --startup-project CollabEditor.API

# Run the API
dotnet run --project CollabEditor.API
```

### Building and Testing
```bash
# Build entire solution
dotnet build CollabEditor.sln

# Run all tests
dotnet test

# Run specific test project
dotnet test CollabEditor.Domain.Test/CollabEditor.Domain.Test.csproj

# Build in Release mode
dotnet build CollabEditor.sln --configuration Release
```

### Database Migrations
```bash
# Create new migration
dotnet ef migrations add <MigrationName> --project CollabEditor.Infrastructure --startup-project CollabEditor.API

# Remove last migration
dotnet ef migrations remove --project CollabEditor.Infrastructure --startup-project CollabEditor.API

# Update database to specific migration
dotnet ef database update <MigrationName> --project CollabEditor.Infrastructure --startup-project CollabEditor.API
```

### Infrastructure Access
- API: http://localhost:5000 (or configured port)
- Swagger UI: http://localhost:5000/swagger (Development only)
- RabbitMQ Management UI: http://localhost:15672 (guest/guest)
- PostgreSQL: localhost:5432 (postgres/postgres, database: collab_editor)

## Architecture

### Layer Responsibilities

**CollabEditor.Domain** - Core business logic with no external dependencies
- Contains aggregates (EditSession), entities (Participant), value objects (TextOperation, DocumentContent)
- Domain events raised by aggregates: OperationAppliedEvent, ParticipantJoinedEvent, ParticipantLeftEvent
- Domain service: IOperationalTransformer (implements Operational Transformation algorithm for collaborative editing)

**CollabEditor.Application** - Use cases via CQRS pattern
- Commands: CreateSessionCommand, JoinSessionCommand, LeaveSessionCommand, ApplyOperationCommand
- Queries: GetSessionQuery
- Uses MediatR for command/query handling, FluentResults for error handling
- Defines interfaces: IEditSessionRepository, IMessageBus, ISessionWriter

**CollabEditor.Infrastructure** - External integrations
- EF Core persistence with PostgreSQL (CollabEditorDbContext)
- SessionWriter pattern: coordinates persistence with event dispatching atomically
- DomainEventDispatcher: converts domain events to integration messages for RabbitMQ
- WebSocketConnectionManager: tracks active WebSocket connections per session
- Flow Managers: OperationFlowManager and SessionFlowManager handle RabbitMQ messages and broadcast via WebSocket

**CollabEditor.Messaging** - RabbitMQ infrastructure
- Channel pooling (10 channels) for high-throughput publishing
- Separate consumer channels for each handler
- Topic-based routing (e.g., session.operation.applied)

**CollabEditor.API** - HTTP/WebSocket entry point
- REST controllers (SessionsController)
- WebSocketMiddleware: handles WebSocket connections with participant tracking
- CORS configured for http://localhost:3000 (React frontend)

### Data Flow for Text Edits

```
Client WebSocket → WebSocketMessageHandler → ApplyOperationCommand
  → EditSession.ApplyOperation() [raises OperationAppliedEvent]
  → SessionWriter.SaveAsync() [persists + dispatches events]
  → DomainEventDispatcher → RabbitMQ
  → OperationFlowManager → WebSocketConnectionManager
  → Broadcast to all clients in session
```

### Key Design Patterns

**Domain-Driven Design**
- EditSession is the aggregate root enforcing consistency boundaries
- All domain logic lives in the aggregate, not in services or handlers
- Value objects are immutable (DocumentContent, TextOperation)
- Domain events raised for significant state changes

**CQRS with MediatR**
- Commands modify state, queries read state
- Handlers live in Application layer, delegate to domain for business logic

**Event Sourcing (Partial)**
- Domain events raised in aggregates
- SessionWriter ensures atomicity: save → dispatch → clear events
- DomainEventDispatcher converts domain events to integration messages

**Operational Transformation**
- IOperationalTransformer service transforms concurrent operations for conflict resolution
- Critical for maintaining consistency across distributed collaborative clients

**Channel Pooling in RabbitMQ**
- Addresses threading issues (see commit history)
- Pool of 10 channels for publishing operations
- Separate dedicated channels for long-lived message consumers

## Configuration

**Connection Strings**: CollabEditor.API/appsettings.Development.json
- PostgreSQL: `Host=localhost;Port=5432;Database=collab_editor;Username=postgres;Password=postgres`

**RabbitMQ Settings**: CollabEditor.API/appsettings.Development.json
- Exchange: `collab-editor-exchange` (type: topic)
- Routing keys: `session.operation.applied`, `session.participant.joined`, `session.participant.left`

## Solution Structure

```
CollabEditor.sln
├── src/
│   ├── CollabEditor.API          # Entry point (controllers, middleware)
│   ├── CollabEditor.Application  # Use cases (commands, queries, handlers)
│   ├── CollabEditor.Domain       # Business logic (aggregates, events, services)
│   ├── CollabEditor.Infrastructure # Persistence, WebSockets, event dispatching
│   ├── CollabEditor.Messaging    # RabbitMQ implementation
│   └── CollabEditor.Utilities    # Shared utilities (AsyncLock, etc.)
└── test/
    └── Unit/
        └── CollabEditor.Domain.Test
```

## Adding New Features

1. **Define domain concept**: Add entities/value objects to Domain layer
2. **Add domain logic**: Implement in aggregate (EditSession), raise domain events
3. **Create command/query**: In Application layer with validation
4. **Implement handler**: Delegate to aggregate, use SessionWriter to persist
5. **Add flow manager** (if needed): Subscribe to new domain events via RabbitMQ
6. **Update API**: Add controller endpoint if external access needed

## Important Patterns to Follow

**SessionWriter Pattern**: Always use SessionWriter (not repository directly) to save aggregates. It ensures domain events are dispatched after persistence.

**Immutability**: Value objects must be immutable. Operations return new instances, never mutate in place.

**Domain Events**: Raised inside aggregates when state changes. Never dispatch them directly; SessionWriter handles this.

**WebSocket Broadcasting**: Flow managers automatically broadcast changes. Don't manually send WebSocket messages from handlers.

**RabbitMQ Message Types**: Use integration messages (OperationAppliedMessage, etc.) not domain events for inter-service communication.