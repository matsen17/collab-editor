# CollabEditor

A real-time collaborative text editor built with .NET 9.0, implementing Domain-Driven Design with event-driven architecture.

## Features

- **Real-time Collaboration**: Multiple users can edit documents simultaneously
- **Operational Transformation**: Automatic conflict resolution for concurrent edits
- **WebSocket Communication**: Low-latency client updates
- **Event-Driven Architecture**: RabbitMQ-based message distribution
- **Domain-Driven Design**: Clean architecture with clear separation of concerns
- **PostgreSQL Persistence**: Reliable data storage with EF Core

## Quick Start

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker](https://www.docker.com/get-started) and Docker Compose

### Run with Docker Compose

```bash
# Start all services
docker-compose up -d postgres rabbitmq

# Apply database migrations
dotnet ef database update --project CollabEditor.Infrastructure --startup-project CollabEditor.API

# Start API
docker-compose up -d --build api

# View logs
docker-compose logs -f api
```

**Access Points:**
- API: http://localhost:8080
- Health Check: http://localhost:8080/health
- Swagger: http://localhost:8080/swagger
- RabbitMQ Management: http://localhost:15672 (guest/guest)

### Run Locally

```bash
# Start infrastructure only
docker-compose up -d postgres rabbitmq

# Apply migrations
dotnet ef database update --project CollabEditor.Infrastructure --startup-project CollabEditor.API

# Run API locally
dotnet run --project CollabEditor.API
```

API available at http://localhost:5000

## Architecture

### Layer Structure

```
CollabEditor.API           → HTTP/WebSocket entry point
CollabEditor.Application   → CQRS commands/queries (MediatR)
CollabEditor.Domain        → Business logic, aggregates, domain events
CollabEditor.Infrastructure → EF Core, WebSocket management, event dispatching
CollabEditor.Messaging     → RabbitMQ integration with channel pooling
CollabEditor.Utilities     → Shared utilities
```

### Key Design Patterns

- **DDD**: Aggregate roots (EditSession) enforce consistency boundaries
- **CQRS**: Commands modify state, queries read state via MediatR
- **Event Sourcing**: Domain events converted to integration messages
- **Operational Transformation**: Conflict resolution for concurrent edits
- **Channel Pooling**: High-throughput RabbitMQ publishing

### Data Flow

```
Client WebSocket → ApplyOperationCommand → EditSession.ApplyOperation()
  → SessionWriter (persist + dispatch events) → RabbitMQ
  → OperationFlowManager → Broadcast to all session clients
```

## API Endpoints

### Sessions

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/sessions` | Create new editing session |
| GET | `/api/sessions/{id}` | Get session details |
| POST | `/api/sessions/{id}/join` | Join session as participant |
| POST | `/api/sessions/{id}/leave` | Leave session |
| POST | `/api/sessions/{id}/operations` | Apply text operation |

### WebSocket

Connect to `/ws` with query parameter `?sessionId={guid}` for real-time updates.

**Message Types:**
- `operation.applied` - Text operation broadcast
- `participant.joined` - New participant notification
- `participant.left` - Participant disconnect notification

## Development

### Build & Test

```bash
# Build solution
dotnet build CollabEditor.sln

# Run tests
dotnet test

# Run with code coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Database Migrations

```bash
# Create migration
dotnet ef migrations add <MigrationName> --project CollabEditor.Infrastructure --startup-project CollabEditor.API

# Apply migrations
dotnet ef database update --project CollabEditor.Infrastructure --startup-project CollabEditor.API

# Remove last migration
dotnet ef migrations remove --project CollabEditor.Infrastructure --startup-project CollabEditor.API
```

### Configuration

Configuration uses environment-specific files:
- `appsettings.Development.json` - Local development (localhost)
- `appsettings.Production.json` - Container deployment

**Environment Variable Overrides:**
```bash
ConnectionStrings__DefaultConnection="Host=postgres;Port=5432;Database=collab_editor;Username=postgres;Password=postgres"
RabbitMQ__HostName="rabbitmq"
Cors__AllowedOrigins__0="https://yourdomain.com"
```

## Deployment

### Docker Image

```bash
# Build image
docker build -t collab-editor:latest .

# Run container
docker run -p 8080:8080 \
  -e ConnectionStrings__DefaultConnection="Host=host.docker.internal;Port=5432;Database=collab_editor;Username=postgres;Password=postgres" \
  -e RabbitMQ__HostName="host.docker.internal" \
  collab-editor:latest
```

**Image Details:**
- Multi-stage build (SDK → Runtime)
- Final size: ~375MB
- Runs as non-root user
- Exposes port 8080

### Production Checklist

- [ ] Configure production database connection string
- [ ] Set up RabbitMQ cluster or managed service
- [ ] Configure CORS allowed origins
- [ ] Set logging to Warning level
- [ ] Enable health checks monitoring
- [ ] Configure secrets management (Azure Key Vault, etc.)
- [ ] Set up reverse proxy (nginx, Traefik)
- [ ] Enable HTTPS/TLS

## CI/CD

**GitHub Actions:**
- `.github/workflows/ci.yml` - Build, test, coverage on every push/PR
- `.github/workflows/docker-build.yml` - Docker images on tagged releases

**Create Release:**
```bash
git tag v1.0.0
git push origin v1.0.0
```

Images published to GitHub Container Registry:
- `ghcr.io/[owner]/collabeditor:1.0.0`
- `ghcr.io/[owner]/collabeditor:latest`

## Contributing

1. Create feature branch from `master`
2. Make changes following DDD patterns
3. Add tests for new functionality
4. Ensure CI passes
5. Submit pull request

**Key Patterns to Follow:**
- Use `SessionWriter` for aggregate persistence (not repository directly)
- Keep value objects immutable
- Raise domain events inside aggregates
- Let flow managers handle WebSocket broadcasting

## Project Structure

```
src/
├── CollabEditor.API/                 # Controllers, middleware, startup
├── CollabEditor.Application/         # Commands, queries, handlers
│   ├── Commands/                     # State-changing operations
│   ├── Queries/                      # Read operations
│   └── Handlers/                     # MediatR handlers
├── CollabEditor.Domain/              # Core business logic
│   ├── Aggregates/                   # EditSession
│   ├── Entities/                     # Participant
│   ├── ValueObjects/                 # TextOperation, DocumentContent
│   ├── Events/                       # Domain events
│   └── Services/                     # IOperationalTransformer
├── CollabEditor.Infrastructure/      # External concerns
│   ├── Persistence/                  # EF Core, DbContext
│   ├── Services/                     # WebSocket, event dispatching
│   └── FlowManagers/                 # RabbitMQ message handlers
├── CollabEditor.Messaging/           # RabbitMQ infrastructure
└── CollabEditor.Utilities/           # Shared utilities

test/
└── Unit/
    └── CollabEditor.Domain.Test/     # Domain logic tests
```

## Technology Stack

- **Framework**: .NET 9.0
- **Database**: PostgreSQL with Entity Framework Core
- **Messaging**: RabbitMQ
- **Communication**: WebSockets, REST API
- **Patterns**: CQRS (MediatR), DDD, Event-Driven Architecture
- **Testing**: xUnit

## License

[Add your license here]

## Support

For issues and feature requests, please use the GitHub issue tracker.