
# School Admission System

A Dockerized school admission system built with ASP.NET Core Web API, React, and PostgreSQL. It supports admission class management, temporary seat reservations, simulated payment confirmation, automatic hold expiration, and concurrent admission requests.

## Technology Stack

- **Backend:** ASP.NET Core Web API (.NET 10)
- **Frontend:** React, Vite, JavaScript
- **Database:** PostgreSQL
- **ORM:** Entity Framework Core
- **Containerization:** Docker and Docker Compose
- **Backend Testing:** xUnit and Testcontainers
- **Frontend Testing:** Vitest and React Testing Library

## Project Structure

```text
SchoolAdmissionSystem/
├── backend/
│   ├── SchoolAdmission.Api/
│   └── SchoolAdmissionSystem.slnx
├── frontend/
├── tests/
│   └── SchoolAdmission.Api.Tests/
├── docker-compose.yml
├── README.md
├── ASSUMPTIONS.md
├── AI_USAGE.md
└── .gitignore
```

## Prerequisites

- Docker Desktop
- Git

Docker Compose starts the complete application. You do not need to install .NET, Node.js, or PostgreSQL separately to run it.

## Run the Application

Clone the repository:

```bash
git clone https://github.com/rafiulshafin/SchoolAdmissionSystem.git
cd SchoolAdmissionSystem
```

Build and start all services:

```bash
docker compose up -d --build
```

Check the running containers:

```bash
docker compose ps
```

## Frontend Backend URL Configuration

The frontend backend URL is configurable through the `BACKEND_URL` environment variable.

By default, the application uses:

```env
BACKEND_URL=http://backend:8080
```

To use a different backend URL, create a `.env` file in the project root and set:

```env
BACKEND_URL=http://your-backend-host:8080
```

Replace `your-backend-host` with the hostname or IP address of the backend service.

After changing the environment variable, recreate the frontend container:

```bash
docker compose up -d --force-recreate frontend
```

The Nginx configuration template automatically applies the configured backend URL when the frontend container starts. The `.env` file is local configuration and should not be committed to the repository.

## Application URLs

| Service | URL |
|---|---|
| Frontend | http://localhost:8080 |
| Backend API 1 | http://localhost:8081 |
| Backend API 2 | http://localhost:8082 |
| PostgreSQL | localhost:5433 |

Both backend instances share the same PostgreSQL database.

## Main Features

- Create admission classes with configurable seat capacity and shifts.
- View admission classes and seat availability.
- Apply for admission using a 17-digit student identifier.
- Temporarily reserve a seat when an application is submitted.
- Confirm admission through simulated payment.
- Automatically expire reservations after the configured hold duration.
- Release seats from expired or cancelled reservations.
- Prevent duplicate active or confirmed applications for the same student and admission class.
- Handle concurrent admission requests safely using PostgreSQL transactions and row-level locking.
- Recover a student's existing applications after refreshing or reopening the application.
- Periodically refresh admission status and seat availability.

## Hold Duration

The default reservation duration is 600 seconds (10 minutes).

It can be changed through the `HOLD_DURATION_SECONDS` environment variable in Docker Compose.

## Running Automated Tests

### Backend Tests

Run from the repository root:

```powershell
dotnet test .\tests\SchoolAdmission.Api.Tests\SchoolAdmission.Api.Tests.csproj
```

Docker Desktop must be running because the backend integration tests use Testcontainers to run PostgreSQL.

### Frontend Tests

Run from the frontend directory:

```bash
cd frontend
npm install
npm test
```

### Frontend Lint and Production Build

```bash
npm run lint
npm run build
```

## Stopping the Application

Stop the containers:

```bash
docker compose down
```

The PostgreSQL data is stored in a named Docker volume and remains available after a normal shutdown and restart.

To remove the containers and permanently delete the database volume:

```bash
docker compose down -v
```

**Warning:** The `-v` option permanently deletes the persisted PostgreSQL data.

## Important Notes

- Student identification uses the `X-Student-Id` request header. It is an assignment-level identification mechanism, not production-grade authentication.
- Payment confirmation is simulated; no real payment gateway is integrated.
- Two backend instances run independently on ports 8081 and 8082 and share the same database.
- No load balancer is configured.
