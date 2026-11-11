\# School Admission System



A Dockerized school admission system built with ASP.NET Core Web API, React, and PostgreSQL. It supports admission class management, temporary seat reservations, simulated payment confirmation, automatic hold expiration, and concurrent admission requests.



\## Technology Stack



\- \*\*Backend:\*\* ASP.NET Core Web API (.NET 10)

\- \*\*Frontend:\*\* React, Vite, JavaScript

\- \*\*Database:\*\* PostgreSQL

\- \*\*ORM:\*\* Entity Framework Core

\- \*\*Containerization:\*\* Docker and Docker Compose

\- \*\*Backend Testing:\*\* xUnit and Testcontainers

\- \*\*Frontend Testing:\*\* Vitest and React Testing Library



\## Project Structure



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

├── AI\_USAGE.md

└── .gitignore

```



\## Prerequisites



\- Docker Desktop

\- Git



Docker Compose starts the complete application. You do not need to install .NET, Node.js, or PostgreSQL separately to run it.



\## Run the Application



Clone the repository:



```bash

git clone <YOUR\_PUBLIC\_GITHUB\_REPOSITORY\_URL>

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



\## Application URLs



| Service | URL |

|---|---|

| Frontend | http://localhost:8080 |

| Backend API 1 | http://localhost:8081 |

| Backend API 2 | http://localhost:8082 |

| PostgreSQL | localhost:5433 |



Both backend instances share the same PostgreSQL database.



\## Main Features



\- Create admission classes with configurable seat capacity and shifts.

\- View admission classes and real-time seat availability.

\- Apply for admission using a 17-digit student identifier.

\- Temporarily reserve a seat when an application is submitted.

\- Confirm admission through simulated payment.

\- Automatically expire reservations after the configured hold duration.

\- Release seats from expired or cancelled reservations.

\- Prevent duplicate active applications for the same student and admission class.

\- Handle concurrent admission requests safely using PostgreSQL transactions and row-level locking.

\- Recover a student's existing applications after refreshing or reopening the application.

\- Periodically refresh admission status and seat availability.



\## Hold Duration



The default reservation duration is 600 seconds (10 minutes).



It can be changed through the `HOLD\_DURATION\_SECONDS` environment variable in Docker Compose.



\## Running Tests



\### Backend tests



From the repository root:



```bash

dotnet test .\\tests\\SchoolAdmission.Api.Tests\\SchoolAdmission.Api.Tests.csproj

```



Docker must be running because the backend integration tests use Testcontainers to run PostgreSQL.



\### Frontend tests



From the frontend directory:



```bash

cd frontend

npm install

npm test

```



\### Frontend lint and production build



```bash

npm run lint

npm run build

```



\## Stop the Application



Stop the containers:



```bash

docker compose down

```



The PostgreSQL data is stored in a named Docker volume and remains available after a normal shutdown and restart.



To remove the containers and permanently delete the database volume:



```bash

docker compose down -v

```



\*\*Warning:\*\* The `-v` option deletes the persisted database data.



\## Important Notes



\- Student identification is implemented using the `X-Student-Id` request header, as required by the assignment. It is not a production authentication system.

\- Payment is simulated; no real payment gateway is integrated.

\- The two backend instances share the same database and can be tested independently on ports 8081 and 8082.




