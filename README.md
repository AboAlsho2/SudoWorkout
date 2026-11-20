# SudoWorkout 💪
### Microservices-Based Workout & Training Management System

SudoWorkout is a backend portfolio project built with **.NET 8**, **ASP.NET Core Minimal APIs**, and **Microservices Architecture**.

The system separates workout planning and training session management into independent services, each owning its own database.

It demonstrates practical backend engineering concepts, including service-to-service communication, database isolation, asynchronous messaging, the Outbox Pattern, Docker containerization, and automated testing.

---

## ✨ Key Features

- **Exercise Management:** Create, retrieve, update, and delete exercises.
- **Workout Plan Management:** Build workout plans and organize exercises with specific sets and reps.
- **Member Plan Assignment:** Assign workout plans to individual members.
- **Training Session Management:** Start training sessions based on assigned workout plans.
- **Session Tracking:** Track session status (`InProgress` / `Completed`).
- **Training History:** Retrieve previous training sessions for each member.
- **Microservices Communication:** Integrate independent services through HTTP.
- **Asynchronous Events:** Publish training session completion events using RabbitMQ.
- **Reliable Event Publishing:** Implement the Outbox Pattern to improve reliability between database transactions and message publishing.
- **Health Monitoring:** Expose health check endpoints.

---

## 🏗️ System Architecture

The application follows a Microservices Architecture, separating responsibilities into two independent services.

```text
                   Client
                     |
          +----------+----------+
          |                     |
          v                     v
   WorkoutService        TrainingService
          |                     |
          v                     |
    Workout DB                  |
                                |
                         HTTP Client
                                |
                                v
                         WorkoutService
                                |
                                v
                          Training DB
                                |
                                v
                           Outbox
                                |
                                v
                            RabbitMQ
                                |
                                v
                     Event Consumers
                     (Future Extension)
```

### Architecture Principles

- **Service Separation:** Each microservice owns a specific business responsibility.
- **Database per Service:** Each service maintains its own database and data ownership.
- **Independent Deployment:** Services can be built and deployed separately.
- **Service-to-Service Communication:** HTTP is used when TrainingService needs workout plan information.
- **Event-Driven Communication:** RabbitMQ is used to publish asynchronous domain events.
- **Fault Isolation:** Services are independently hosted, with explicit handling of communication failures.

---

## 🧩 Microservices

### 1. WorkoutService

<badge color="success">Implemented</badge>

**Responsibility:** Manages the workout catalog, workout plans, and member plan assignments.

#### Domain Entities

| Entity | Description |
|---|---|
| `Exercise` | Exercise information, including name and description. |
| `WorkoutPlan` | A collection of exercises forming a workout plan. |
| `WorkoutPlanExercise` | Associates exercises with plans and defines sets and reps. |
| `PlanAssignment` | Assigns a workout plan to a member using `MemberId`. |

#### API Endpoints

**Exercises**

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/exercises` | Retrieve all exercises |
| GET | `/api/exercises/{id}` | Retrieve an exercise |
| POST | `/api/exercises` | Create an exercise |
| PUT | `/api/exercises/{id}` | Update an exercise |
| DELETE | `/api/exercises/{id}` | Delete an exercise |

**Workout Plans**

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/workout-plans` | Retrieve all workout plans |
| GET | `/api/workout-plans/{id}` | Retrieve a workout plan |
| POST | `/api/workout-plans` | Create a workout plan |
| PUT | `/api/workout-plans/{id}` | Update a workout plan |
| DELETE | `/api/workout-plans/{id}` | Delete a workout plan |
| POST | `/api/workout-plans/{planId}/exercises` | Add an exercise to a plan |
| GET | `/api/workout-plans/{planId}/exercises` | Retrieve plan exercises |
| PUT | `/api/workout-plans/{planId}/exercises/{exerciseId}` | Update a plan exercise |
| DELETE | `/api/workout-plans/{planId}/exercises/{exerciseId}` | Remove an exercise from a plan |

**Member Plan Assignment**

| Method | Endpoint | Description |
|---|---|---|
| POST | `/api/members/{memberId}/plan` | Assign a plan to a member |
| GET | `/api/members/{memberId}/plan` | Retrieve the member's assigned plan |
| PUT | `/api/members/{memberId}/plan` | Update the assignment |
| DELETE | `/api/members/{memberId}/plan` | Remove the assignment |

---

### 2. TrainingService

<badge color="success">Implemented</badge>

**Responsibility:** Manages actual training sessions and maintains members' training history.

TrainingService communicates with WorkoutService through an HTTP Client to retrieve assigned workout plans.

#### Domain Entities

| Entity | Description |
|---|---|
| `TrainingSession` | Represents a training session for a member. |
| `TrainingSessionExercise` | Represents an exercise included in a training session. |
| `SessionStatus` | Tracks the session state: `InProgress` or `Completed`. |

#### API Endpoints

| Method | Endpoint | Description |
|---|---|---|
| POST | `/api/members/{memberId}/sessions` | Start a new training session |
| GET | `/api/sessions/{sessionId}` | Retrieve session details |
| GET | `/api/members/{memberId}/sessions` | Retrieve member training history |
| POST | `/api/sessions/{sessionId}/complete` | Complete a training session |
| GET | `/health` | Health check |

#### Training Session Workflow

1. A member requests to start a new training session.
2. TrainingService requests the member's assigned workout plan from WorkoutService.
3. The service validates that the member has an assigned plan.
4. It checks that the assigned plan contains exercises.
5. A new training session is created with the `InProgress` status.
6. The assigned exercises are copied into the session.
7. The member can complete the session through the completion endpoint.

**Business Validation:**

- `NoAssignedPlan`: The member has no assigned workout plan.
- `WorkoutPlanNotFound`: The requested workout plan does not exist.
- `EmptyWorkoutPlan`: The assigned workout plan contains no exercises.

---

## 📨 Asynchronous Messaging & RabbitMQ

SudoWorkout uses **RabbitMQ** for asynchronous event publishing, allowing services to communicate through events without requiring direct synchronous communication for every operation.

### Training Session Completed Event

When a training session is completed, TrainingService publishes a `TrainingSessionCompleted` event.

The event contains:

| Property | Description |
|---|---|
| `EventId` | Unique event identifier |
| `TrainingSessionId` | Completed session identifier |
| `MemberId` | Member associated with the session |
| `WorkoutPlanId` | Associated workout plan |
| `CompletedAtUtc` | Session completion timestamp |

### RabbitMQ Configuration

- Exchange: `sudo-workout.events`
- Queue: `notification.training-session-completed`
- Client Library: `RabbitMQ.Client`

### Outbox Pattern

The project implements the Outbox Pattern to improve reliability when publishing events.

Instead of relying on a direct database-and-message-broker operation, the event is first persisted in the database as part of the business transaction.

A publisher subsequently retrieves pending events from the Outbox and publishes them to RabbitMQ.

This approach helps prevent inconsistencies between database updates and message publishing.

**Messaging scope:** Event publishing and Outbox infrastructure are implemented. A separate NotificationService consumer is a planned extension.

---

## 🛠️ Technology Stack

### Backend
- C#
- .NET 8
- ASP.NET Core Minimal APIs
- Entity Framework Core 8
- SQL Server
- HttpClient
- Dependency Injection

### Microservices & Messaging
- Microservices Architecture
- Database per Service
- RabbitMQ
- RabbitMQ.Client
- Outbox Pattern
- Asynchronous Event Publishing

### DevOps & Infrastructure
- Docker
- Docker Compose
- SQL Server 2022 Developer
- RabbitMQ Management

### Testing
- xUnit
- Unit Testing
- Integration Testing
- WebApplicationFactory
- SQLite for isolated integration testing

---

## 🐳 Running with Docker

Docker Compose is used to run the infrastructure dependencies and application services.

### Prerequisites

- Docker Desktop
- Docker Compose
- .NET 8 SDK (for local development)

### 1. Clone the Repository

```bash
git clone https://github.com/AboAlsho2/SudoWorkout.git
cd SudoWorkout
```

### 2. Configure Environment Variables

Create a `.env` file in the project root:

```env
SA_PASSWORD=YourStrongPassword123!
```

Use a strong password that satisfies SQL Server password requirements.

### 3. Start the Services

```bash
docker compose up --build
```

### 4. Service Ports

| Service | Port |
|---|---:|
| SQL Server | 1433 |
| WorkoutService | 8080 |
| TrainingService | 8081 |
| RabbitMQ AMQP | 5672 |
| RabbitMQ Management UI | 15672 |

### 5. Verify the Services

```bash
# WorkoutService
curl http://localhost:8080

# TrainingService
curl http://localhost:8081/health
```

Check the RabbitMQ Management UI:

```text
http://localhost:15672
```

---

## 💻 Running Locally

### Prerequisites

- .NET 8 SDK
- SQL Server
- RabbitMQ

### 1. Configure Connection Strings

Update the connection strings in each service's `appsettings.json` according to your local SQL Server configuration.

### 2. Start WorkoutService

```bash
cd src/Services/WorkoutService
dotnet run
```

### 3. Start TrainingService

Open another terminal:

```bash
cd src/Services/TrainingService
dotnet run
```

Make sure the `Services:WorkoutService:BaseUrl` configuration in TrainingService points to the running WorkoutService instance.

Configure the RabbitMQ connection settings as required by your local environment.

---

## 🔄 Example Application Workflow

The following sequence demonstrates a typical application flow:

```text
1. Create Exercises
   POST /api/exercises

2. Create a Workout Plan
   POST /api/workout-plans

3. Add Exercises to the Plan
   POST /api/workout-plans/{planId}/exercises

4. Assign the Plan to a Member
   POST /api/members/{memberId}/plan

5. Start a Training Session
   POST /api/members/{memberId}/sessions

6. Complete the Training Session
   POST /api/sessions/{sessionId}/complete

7. Publish TrainingSessionCompleted Event
   TrainingService → Outbox → RabbitMQ
```

---

## 🧪 Testing

The solution includes unit and integration tests for both services.

### Test Projects

```text
tests/
├── WorkoutService.Tests/
└── TrainingService.Tests/
```

### Running Tests

From the solution root:

```bash
dotnet test
```

Integration tests use `WebApplicationFactory` to test API behavior, with SQLite providing an isolated database environment for testing.

---

## 📁 Detailed Service Structure

### WorkoutService

```text
WorkoutService/
├── Contracts/
├── Domain/
│   └── Entities/
├── Endpoints/
├── Persistence/
├── Dockerfile
└── Program.cs
```

### TrainingService

```text
TrainingService/
├── Contracts/
│   └── WorkoutService/
├── Domain/
│   └── Entities/
├── Endpoints/
├── Integrations/
│   └── WorkoutService/
├── Persistence/
├── Services/
├── Dockerfile
└── Program.cs
```

---

## 📌 Design Decisions & Limitations

- Each microservice owns a separate database:
  - `SudoWorkoutDb` → WorkoutService
  - `SudoWorkoutTrainingDb` → TrainingService
- Service-to-service plan retrieval currently uses synchronous HTTP communication.
- RabbitMQ is used for asynchronous event publishing.
- The Outbox Pattern improves reliability between database transactions and event publishing.
- Members are identified using `Guid` values.
- There is currently no dedicated User or Identity Service.
- Authentication and authorization have not yet been implemented.
- NotificationService is planned but not yet implemented.

---

## 🚀 Future Improvements

- Implement a dedicated NotificationService to consume RabbitMQ events.
- Add authentication and authorization using JWT or Keycloak.
- Introduce a dedicated Member/User Service.
- Track actual completed sets and reps.
- Implement distributed caching.
- Introduce an API Gateway.
- Improve service resilience with retry policies and circuit breakers.
- Expand observability with centralized logging and distributed tracing.

---

## 👨‍💻 Developer

**AboAlsho2**

GitHub: [AboAlsho2](https://github.com/AboAlsho2)

