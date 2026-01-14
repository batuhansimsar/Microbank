# Microbank - Microservices Banking Application

A production-ready microservices-based banking application built with .NET 8, implementing the SAGA pattern for distributed transactions.

## Architecture

This project demonstrates a complete microservices architecture with:

- **4 Microservices**: Identity, Account, Transfer (SAGA Orchestrator), Notification
- **Event-Driven Communication**: MassTransit with RabbitMQ message broker
- **Database-Per-Service**: 3 PostgreSQL databases
- **SAGA Pattern**: Orchestration-based distributed transactions with compensation
- **Containerization**: Docker & Docker Compose

### Services

1. **Identity Service** (Port 5001)
   - User registration & authentication
   - JWT token generation
   - PostgreSQL database

2. **Account Service** (Port 5002)
   - Bank account management
   - Balance operations (debit/credit)
   - Event-driven balance updates
   - PostgreSQL database

3. **Transfer Service** (Port 5003)
   - SAGA orchestrator for money transfers
   - Distributed transaction coordination
   - Compensation logic for failures
   - PostgreSQL database

4. **Notification Service** (Port 5004)
   - Asynchronous event consumer
   - Transfer success/failure notifications
   - Stateless service

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [Git](https://git-scm.com/)

## Quick Start

### 1. Clone the Repository

```bash
git clone https://github.com/batuhansimsar/Microbank.git
cd Microbank
```

### 2. Start All Services with Docker Compose

```bash
docker-compose up -d
```

This will start:
- 3 PostgreSQL databases (ports 5532, 5533, 5534)
- RabbitMQ (port 5672, management UI on 15672)
- 4 microservices (ports 5001-5004)

### 3. Verify Services are Running

```bash
docker-compose ps
```

All containers should show "Up" status.

### 4. Test the Application

#### Register a User

```bash
curl -X POST http://localhost:5001/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "john@example.com",
    "password": "SecurePass123!",
    "fullName": "John Doe"
  }'
```

#### Login and Get Token

```bash
curl -X POST http://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "john@example.com",
    "password": "SecurePass123!"
  }'
```

Save the `token` from the response.

#### Create a Bank Account

```bash
TOKEN="your-jwt-token-here"

curl -X POST http://localhost:5002/api/accounts \
  -H "Authorization: Bearer $TOKEN"
```

Save the account `id` from the response.

#### Check Balance

```bash
ACCOUNT_ID="your-account-id-here"

curl http://localhost:5002/api/accounts/$ACCOUNT_ID/balance \
  -H "Authorization: Bearer $TOKEN"
```

#### Create Second User and Account

Repeat the registration, login, and account creation steps for a second user.

#### Initiate a Transfer (SAGA Pattern)

```bash
FROM_ACCOUNT="first-account-id"
TO_ACCOUNT="second-account-id"

curl -X POST http://localhost:5003/api/transfers \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"fromAccountId\": \"$FROM_ACCOUNT\",
    \"toAccountId\": \"$TO_ACCOUNT\",
    \"amount\": 100,
    \"currency\": \"TRY\"
  }"
```

#### Check Transfer Status

```bash
TRANSFER_ID="transfer-id-from-response"

curl http://localhost:5003/api/transfers/$TRANSFER_ID \
  -H "Authorization: Bearer $TOKEN"
```

## Development Setup

### Run Services Locally (Without Docker)

1. **Start Infrastructure Only**

```bash
docker-compose up -d identity-db account-db transfer-db rabbitmq
```

2. **Run Each Service**

```bash
# Terminal 1 - Identity Service
cd src/Services/Identity/Identity.API
dotnet run --urls="http://localhost:5001"

# Terminal 2 - Account Service
cd src/Services/Account/Account.API
dotnet run --urls="http://localhost:5002"

# Terminal 3 - Transfer Service
cd src/Services/Transfer/Transfer.API
dotnet run --urls="http://localhost:5003"

# Terminal 4 - Notification Service
cd src/Services/Notification/Notification.API
dotnet run --urls="http://localhost:5004"
```

## Project Structure

```
Microbank/
├── src/
│   ├── BuildingBlocks/
│   │   ├── Common/              # Shared utilities
│   │   ├── Common.Logging/      # Serilog configuration
│   │   └── EventBus.RabbitMQ/   # Event bus implementation
│   │
│   └── Services/
│       ├── Identity/
│       │   ├── Identity.API/
│       │   └── Dockerfile
│       ├── Account/
│       │   ├── Account.Domain/
│       │   ├── Account.API/
│       │   └── Dockerfile
│       ├── Transfer/
│       │   ├── Transfer.Domain/
│       │   ├── Transfer.API/
│       │   └── Dockerfile
│       └── Notification/
│           ├── Notification.API/
│           └── Dockerfile
│
├── docker-compose.yml
├── Microbank.sln
└── README.md
```

## SAGA Pattern Implementation

The Transfer Service implements a **centralized orchestration-based Saga pattern** using MassTransit State Machine to manage distributed transactions across Account service boundaries.

### Architecture: Orchestration vs Choreography

**Orchestration-based (Current):**
- ✅ Centralized state machine in Transfer service
- ✅ All orchestration logic in `TransferStateMachine`
- ✅ Saga state persisted to database (`TransferSagaStates` table)
- ✅ Easy to monitor, debug, and extend
- ✅ Automatic compensation on failures

**Previous Choreography approach:**
- Services communicated directly via events
- No central state tracking
- Logic scattered across event handlers

### State Machine Flow

```
[Transfer Initiated] 
    ↓
[Debiting] → Publish IDebitAccountRequested
    ↓
    ├─→ [Success: AccountDebited] 
    │       ↓
    │   [Crediting] → Publish ICreditAccountRequested
    │       ↓
    │       ├─→ [Success: AccountCredited] → ✅ COMPLETED
    │       │
    │       └─→ [Failure: Credit Failed] 
    │               ↓
    │           [Compensating] → Refund sender
    │               ↓
    │           ❌ FAILED (with compensation)
    │
    └─→ [Failure: Debit Failed] → ❌ FAILED (no compensation needed)
```

### Saga States

| State | Description |
|-------|-------------|
| `Initiated` | Transfer created, ready to debit sender account |
| `Debiting` | Waiting for debit confirmation from Account service |
| `Debited` | Sender debited successfully, ready to credit receiver |
| `Crediting` | Waiting for credit confirmation from Account service |
| `Completed` | Transfer successful - both debit and credit completed ✅ |
| `Compensating` | Rolling back - refunding sender after credit failure |
| `Failed` | Transfer failed (with or without compensation) ❌ |

### Happy Path Flow

1. **User initiates transfer** → Saga state: `Initiated`
2. **Saga requests debit** → Publishes `IDebitAccountRequested` → State: `Debiting`
3. **Account service debits sender** → Publishes `IAccountDebited` → State: `Debited`
4. **Saga requests credit** → Publishes `ICreditAccountRequested` → State: `Crediting`
5. **Account service credits receiver** → Publishes `IAccountCredited` → State: `Completed` ✅
6. **Notification sent** → Transfer complete

### Compensation Path (Failure Scenario)

1. **User initiates transfer** → Saga state: `Initiated`
2. **Saga requests debit** → State: `Debiting`
3. **Sender account debited** → State: `Debited`
4. **Saga requests credit** → State: `Crediting`
5. **Credit fails** (e.g., receiver account closed) → Publishes `IAccountOperationFailed`
6. **Saga triggers compensation** → Publishes `ICompensateDebit` → State: `Compensating`
7. **Account service refunds sender** → Money returned to original account
8. **Transfer marked as failed** → State: `Failed` ❌
9. **Failure notification sent**

### Database Persistence

The saga state is persisted in the `TransferSagaStates` table:

```sql
-- View all saga states
SELECT 
    "CorrelationId",
    "CurrentState",
    "TransferId",
    "Amount",
    "InitiatedAt",
    "CompletedAt",
    "FailureReason"
FROM "TransferSagaStates"
ORDER BY "InitiatedAt" DESC;

-- Find stuck/pending transfers
SELECT * FROM "TransferSagaStates" 
WHERE "CurrentState" NOT IN ('Completed', 'Failed')
AND "InitiatedAt" < NOW() - INTERVAL '5 minutes';
```

### Key Features

- **Automatic Compensation**: If credit fails after successful debit, saga automatically refunds sender
- **State Persistence**: Survives service restarts - saga continues from last known state
- **Idempotency**: Inbox/Outbox pattern ensures exactly-once message processing
- **Observability**: Query database to see exact state of any transfer
- **Retry Policies**: Configured with 3 retries at 5-second intervals
- **Concurrency Control**: Pessimistic locking prevents race conditions

## API Documentation

### Identity Service (5001)

- `POST /api/auth/register` - Register new user
- `POST /api/auth/login` - Login and get JWT token
- `GET /api/auth/me` - Get current user info

### Account Service (5002)

- `POST /api/accounts` - Create bank account
- `GET /api/accounts/{id}` - Get account details
- `GET /api/accounts/{id}/balance` - Get balance
- `GET /api/accounts/{id}/transactions` - Get transaction history

### Transfer Service (5003)

- `POST /api/transfers` - Initiate transfer
- `GET /api/transfers/{id}` - Get transfer status
- `GET /api/transfers/user/{userId}` - Get user's transfers

### Notification Service (5004)

- No HTTP endpoints (event-driven only)

## Configuration

### Database Connections

Each service connects to its own PostgreSQL database:
- Identity: `localhost:5532`
- Account: `localhost:5533`
- Transfer: `localhost:5534`

### RabbitMQ

- AMQP: `localhost:5672`
- Management UI: `http://localhost:15672` (guest/guest)

### JWT Settings

Configured in each service's `appsettings.json`:
- SecretKey: Change in production!
- Issuer: `MicrobankIdentity`
- Audience: `MicrobankServices`

## Stopping Services

```bash
# Stop all containers
docker-compose down

# Stop and remove volumes (WARNING: deletes all data)
docker-compose down -v
```

## Technology Stack

- **Backend**: .NET 8, ASP.NET Core
- **Database**: PostgreSQL 16
- **Message Broker**: RabbitMQ 3
- **Service Bus**: MassTransit 8
- **ORM**: Entity Framework Core 8
- **Logging**: Serilog
- **Authentication**: JWT Bearer
- **Containerization**: Docker

## License

MIT License

## Author

[Batuhan Simsar](https://github.com/batuhansimsar)
